using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Management.Storage.ScenarioTest.Util;
using MS.Test.Common.MsTestLib;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;

namespace Management.Storage.ScenarioTest
{
    class CLUAgent : Agent
    {
        private string currentSessionID = null;
        public static OSType AgentOSType = OSType.Windows;
        public static OSConfig AgentConfig = new OSConfig();
        
        public static void GetOSConfig(TestConfig testConfig)
        {
            Utility.GetOSConfig(testConfig, ref AgentOSType, AgentConfig);
        }

        static void SetProcessInfo(Process p, string argument)
        {
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            p.StartInfo.WorkingDirectory = Test.Data.Get("NodeWorkingDirectory");

            if (AgentOSType == OSType.Linux)
            {
                p.StartInfo.FileName = AgentConfig.PLinkPath;
                p.StartInfo.Arguments = string.Format("-l {0} -i \"{1}\" {2} -P {3} -t ",
                    AgentConfig.UserName, AgentConfig.PrivateKeyPath, AgentConfig.HostName, AgentConfig.Port);

                p.StartInfo.Arguments += "\"" + argument + "\"";
            }
        }

        internal void HandleExecutionError(string output)
        {
            Dictionary<string, object> message = new Dictionary<string, object>();
            message["output"] = output;
            Output.Add(message);
        }

        internal bool RunSRPCLUProcess(string argument)
        {
            if (!string.IsNullOrEmpty(this.currentSessionID))
            {
                argument = "export CmdletSessionID=" + this.currentSessionID + ";" + argument;
            }

            return RunCLUProcess(argument);
        }
        
        internal bool RunCLUProcess(string argument, Action<string> parse = null, bool getOutputFromFile = true)
        {
            if (getOutputFromFile)
            {
                argument += " > output 2>&1;cat output;rm -f output";
            }

            Process p = new Process();
            SetProcessInfo(p, argument);
            p.Start();

            StringBuilder outputBuffer = new StringBuilder();
            p.OutputDataReceived += (sendingProcess, outLine) =>
            {
                if (!String.IsNullOrEmpty(outLine.Data))
                {
                    outputBuffer.Append(outLine.Data + "\n");
                }
            };
            StringBuilder errorBuffer = new StringBuilder();
            p.ErrorDataReceived += (sendingProcess, outLine) =>
            {
                if (!String.IsNullOrEmpty(outLine.Data))
                {
                    errorBuffer.Append(outLine.Data + "\n");
                }
            };

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit(Constants.DefaultMaxWaitingTime);

            string output = string.Empty;
            string error = string.Empty;

            if (!p.HasExited)
            {
                p.Kill();

                printInfo(outputBuffer, errorBuffer, ref output, ref error);

                throw new Exception(string.Format("CLU command timeout: cost time > 900s !"));
            }
            else
            {
                // To work around the issue that WaitForExit() with parameter will exit when not all the threads are completed. 
                p.WaitForExit();

                printInfo(outputBuffer, errorBuffer, ref output, ref error);
            }

            ErrorMessages.Clear();
            Output.Clear();

            bool bSuccess = string.IsNullOrEmpty(error);

            if (bSuccess)
            {
                if (null != parse)
                {
                    try
                    {
                        parse(output);
                    }
                    catch (Exception)
                    {
                        HandleExecutionError(output);
                        throw;
                    }
                }
                else
                {
                    output = parseOutput(output);
                    output = output.Replace("\n", ",");

                    Collection<Dictionary<string, object>> results = null;
                    try
                    {
                        results = JsonConvert.DeserializeObject<Collection<Dictionary<string, object>>>(output);
                    }
                    catch (JsonReaderException)
                    {
                        ErrorMessages.Add(output);
                        throw;
                    }

                    if (results != null)
                    {
                        Output = results;
                    }
                }
            }
            else
            {
                ErrorMessages.Add(error);
            }

            return bSuccess;
        }

        internal string parseOutput(string output)
        {
            // parse output data
            // the output may have warning message, it should keep the json object only.
            // WARNING: warning message
            // [{..........}]
            output = output.Trim();
            if (output.Length >= 2)
            {
                int startIndex = output.IndexOf('[');
                if (Regex.Match(output, "^{.*}$", RegexOptions.Singleline).Success)
                {
                    // modify output as a collection
                    output = '[' + output + ']';
                }
                else if (startIndex > 0 && output[startIndex - 1] == '\n')
                {
                    // Check '[' starts in a new line to escape the warning message in the output before json objects
                    int endIndex = output.LastIndexOf(']');
                    if (startIndex != -1 && endIndex != -1)
                    {
                        output = output.Substring(startIndex, endIndex - startIndex + 1);
                    }
                }
                else if (!Regex.Match(output, @"^\[.*\]$", RegexOptions.Singleline).Success)
                {
                    output = "[" + output + "]";
                }
            }

            return output;
        }

        internal void printInfo(StringBuilder outputBuffer, StringBuilder errorBuffer, ref string output, ref string error)
        {
            error = errorBuffer.ToString();
            if (!string.IsNullOrEmpty(error))
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Test.Verbose("Error:\n{0}", error);
                }
            }

            output = outputBuffer.ToString();
            Test.Verbose("CLU Output:\n{0}", output);
        }

        public override void AssertErrors(Action<IExecutionError> assertErrorAction, int expectedErrorCount = 1)
        {
            throw new NotImplementedException();
        }

        public override void AssertNoError()
        {
            throw new NotImplementedException();
        }

        public override bool ChangeCLIMode(Constants.Mode mode)
        {
            throw new NotImplementedException();
        }

        public override void ChangeLocation(string path)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            throw new NotImplementedException();
        }

        public override bool HadErrors
        {
            get { return false; }
        }

        public override IExecutionResult Invoke(System.Collections.IEnumerable input = null, bool traceCommand = true)
        {
            throw new NotImplementedException();
        }

        public override bool Login()
        {
            string argument = string.Format("azure account add --ServicePrincipal --TenantId {0} --ApplicationId {1} --Secret {2} --SubscriptionId {3}",                
                    Test.Data.Get("AADRealm"),
                    Test.Data.Get("AADClient"),
                    Test.Data.Get("AADPassword"),
                    Test.Data.Get("AzureSubscriptionID"));

            argument += " > /dev/null 2>&1;echo $PPID";

            return RunCLUProcess(argument, (output) => 
            {
                this.currentSessionID = output.TrimEnd(' ', '\n');
                this.currentSessionID = (int.Parse(this.currentSessionID) + 1).ToString();
            },
            false);
        }

        public override void Logout()
        {
            this.currentSessionID = null;
        }

        public override void SetActiveSubscription(string subscriptionId)
        {
            throw new NotImplementedException();
        }

        #region SRP Cmdlets

        public override bool CreateSRPAzureStorageAccount(string resourceGroupName, string accountName, string type, string location, System.Collections.Hashtable[] tags = null)
        {
            string argument = string.Format("azure storage account new -g {0} -n {1} -t {2} -l '{3}'",
                resourceGroupName,
                accountName,
                type,
                location.Replace(" ", ""));

            if (null != tags)
            {
                argument += string.Format(" --Tags {0}", JsonConvert.SerializeObject(tags));
            }

            return this.RunSRPCLUProcess(argument);
        }

        public override bool ShowSRPAzureStorageAccount(string resourceGroup, string accountName)
        {
            string argument = "azure storage account get";

            if (!string.IsNullOrEmpty(resourceGroup))
            {
                argument += " -g " + resourceGroup;
            }

            if (!string.IsNullOrEmpty(accountName))
            {
                argument += " -n " + accountName;
            }

            return RunSRPCLUProcess(argument);
        }

        public override bool ShowSRPAzureStorageAccountKeys(string resourceGroup, string accountName)
        {
            throw new NotImplementedException();
        }

        public override bool SetSRPAzureStorageAccount(string resourceGroupName, string accountName, string accountType)
        {
            throw new NotImplementedException();
        }

        public override bool SetRmCurrentStorageAccount(string storageAccountName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public override bool SetSRPAzureStorageAccountCustomDomain(string resourceGroupName, string accountName, string customDomain, bool? useSubdomain)
        {
            throw new NotImplementedException();
        }

        public override bool SetSRPAzureStorageAccountTags(string resourceGroupName, string accountName, System.Collections.Hashtable[] tags)
        {
            throw new NotImplementedException();
        }

        public override bool RenewSRPAzureStorageAccountKeys(string resourceGroupName, string accountName, Constants.AccountKeyType type)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteSRPAzureStorageAccount(string resourceGroup, string accountName)
        {
            throw new NotImplementedException();
        }

        public override bool CheckNameAvailability(string accountName)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageUsage()
        {
            throw new NotImplementedException();
        }

        #endregion //SRP Cmdlets

        #region RDFE account management

        public override void ImportAzureSubscription(string settingFile)
        {
            throw new NotImplementedException();
        }

        public override bool CreateAzureStorageAccount(string accountName, string subscription, string label, string description, string location, string affinityGroup, string type, bool? geoReplication = null)
        {
            throw new NotImplementedException();
        }

        public override bool ShowAzureStorageAccount(string accountName)
        {
            throw new NotImplementedException();
        }

        public override bool ShowAzureStorageAccountConnectionString(string accountName, string resourceGroupName = null)
        {
            throw new NotImplementedException();
        }

        public override bool ShowAzureStorageAccountKeys(string accountName)
        {
            throw new NotImplementedException();
        }

        public override bool RenewAzureStorageAccountKeys(string accountName, Constants.AccountKeyType type)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureStorageAccount(string accountName, string label, string description, string type, bool? geoReplication = null)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAzureStorageAccount(string accountName)
        {
            throw new NotImplementedException();
        }

        #endregion // RDFE account management

        #region Storage Context

        public override object CreateStorageContextObject(string connectionString)
        {
            throw new NotImplementedException();
        }

        public override object GetStorageContextWithSASToken(Microsoft.WindowsAzure.Storage.CloudStorageAccount account, string sasToken, string endpointSuffix = null, bool useHttps = false)
        {
            throw new NotImplementedException();
        }

        public override string SetContextWithSASToken(string accountName, Util.CloudBlobUtil blobUtil, StorageObjectType objectType, string endpoint, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null)
        {
            throw new NotImplementedException();
        }

        public override string SetContextWithSASToken(string accountName, Util.CloudBlobUtil blobUtil, StorageObjectType objectType, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null)
        {
            throw new NotImplementedException();
        }

        public override void SetStorageContextWithSASToken(string StorageAccountName, string sasToken, bool useHttps = true)
        {
            throw new NotImplementedException();
        }

        public override void SetStorageContextWithSASToken(string StorageAccountName, string sasToken, string endpoint, bool useHttps = true)
        {
            throw new NotImplementedException();
        }

        public override void SetStorageContextWithSASTokenInConnectionString(Microsoft.WindowsAzure.Storage.CloudStorageAccount StorageAccount, string sasToken)
        {
            throw new NotImplementedException();
        }

        #endregion // Storage Context

        #region Services

        public override bool GetAzureStorageCORSRules(Constants.ServiceType serviceType)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageServiceLogging(Constants.ServiceType serviceType)
        {
            return base.GetAzureStorageServiceLogging(serviceType);
        }

        public override bool GetAzureStorageServiceMetrics(Constants.ServiceType serviceType, Constants.MetricsType metricsType)
        {
            return base.GetAzureStorageServiceMetrics(serviceType, metricsType);
        }

        public override bool SetAzureStorageCORSRules(Constants.ServiceType serviceType, Microsoft.WindowsAzure.Commands.Storage.Model.ResourceModel.PSCorsRule[] corsRules)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureStorageServiceLogging(Constants.ServiceType serviceType, Microsoft.WindowsAzure.Storage.Shared.Protocol.LoggingOperations[] loggingOperations, string loggingRetentionDays = "", string loggingVersion = "", bool passThru = false)
        {
            return base.SetAzureStorageServiceLogging(serviceType, loggingOperations, loggingRetentionDays, loggingVersion, passThru);
        }

        public override bool SetAzureStorageServiceLogging(Constants.ServiceType serviceType, string loggingOperations = "", string loggingRetentionDays = "", string loggingVersion = "", bool passThru = false)
        {
            return base.SetAzureStorageServiceLogging(serviceType, loggingOperations, loggingRetentionDays, loggingVersion, passThru);
        }

        public override bool SetAzureStorageServiceMetrics(Constants.ServiceType serviceType, Constants.MetricsType metricsType, string metricsLevel = "", string metricsRetentionDays = "", string metricsVersion = "", bool passThru = false)
        {
            return base.SetAzureStorageServiceMetrics(serviceType, metricsType, metricsLevel, metricsRetentionDays, metricsVersion, passThru);
        }

        public override bool RemoveAzureStorageCORSRules(Constants.ServiceType serviceType)
        {
            throw new NotImplementedException();
        }

        #endregion //Services

        #region Blobs

        public override bool DownloadBlobFiles(string dirPath, string containerName, bool force = true, int concurrentCount = -1)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageContainer(string ContainerName)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageContainerByPrefix(string Prefix)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureStorageContainerACL(string ContainerName, Microsoft.WindowsAzure.Storage.Blob.BlobContainerPublicAccessType PublicAccess, bool PassThru = true)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureStorageContainerACL(string[] ContainerNames, Microsoft.WindowsAzure.Storage.Blob.BlobContainerPublicAccessType PublicAccess, bool PassThru = true)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageContainer(string ContainerName)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageContainer(string[] ContainerNames)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageContainer(string ContainerName, bool Force = true)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageContainer(string[] ContainerNames, bool Force = true)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageContainerStoredAccessPolicy(string containerName, string policyName, bool Force = true)
        {
            return base.RemoveAzureStorageContainerStoredAccessPolicy(containerName, policyName, Force);
        }

        public override bool GetAzureStorageContainerStoredAccessPolicy(string containerName, string policyName)
        {
            return base.GetAzureStorageContainerStoredAccessPolicy(containerName, policyName);
        }

        public override string GetContainerSasFromCmd(string containerName, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return base.GetContainerSasFromCmd(containerName, policy, permission, startTime, expiryTime, fulluri);
        }

        public override string GetBlobSasFromCmd(Microsoft.WindowsAzure.Storage.Blob.CloudBlob blob, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return base.GetBlobSasFromCmd(blob, policy, permission, startTime, expiryTime, fulluri);
        }

        public override string GetBlobSasFromCmd(string containerName, string blobName, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return base.GetBlobSasFromCmd(containerName, blobName, policy, permission, startTime, expiryTime, fulluri);
        }

        public override bool NewAzureStorageContainerStoredAccessPolicy(string containerName, string policyName, string permission, DateTime? startTime = null, DateTime? expiryTime = null)
        {
            return base.NewAzureStorageContainerStoredAccessPolicy(containerName, policyName, permission, startTime, expiryTime);
        }

        public override bool NewAzureStorageContainerSAS(string container, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return base.NewAzureStorageContainerSAS(container, policy, permission, startTime, expiryTime, fullUri);
        }

        public override bool NewAzureStorageBlobSAS(string container, string blob, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return base.NewAzureStorageBlobSAS(container, blob, policy, permission, startTime, expiryTime, fullUri);
        }

        public override bool SetAzureStorageContainerStoredAccessPolicy(string containerName, string policyName, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            return base.SetAzureStorageContainerStoredAccessPolicy(containerName, policyName, permission, startTime, expiryTime, NoStartTime, NoExpiryTime);
        }

        public override bool GetAzureStorageBlob(string BlobName, string ContainerName)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageBlobByPrefix(string Prefix, string ContainerName)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageBlobContent(string Blob, string FileName, string ContainerName, bool Force = true, int ConcurrentCount = -1)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureStorageBlobContent(string FileName, string ContainerName, Microsoft.WindowsAzure.Storage.Blob.BlobType Type, string BlobName = "", bool Force = true, int ConcurrentCount = -1, System.Collections.Hashtable properties = null, System.Collections.Hashtable metadata = null)
        {
            throw new NotImplementedException();
        }

        public override bool UploadLocalFiles(string dirPath, string containerName, Microsoft.WindowsAzure.Storage.Blob.BlobType blobType, bool force = true, int concurrentCount = -1)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageBlob(string BlobName, string ContainerName, bool onlySnapshot = false, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageBlobCopyState(string containerName, string blobName, bool waitForComplete)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageBlobCopyState(Microsoft.WindowsAzure.Storage.Blob.CloudBlob blob, object context, bool waitForComplete)
        {
            throw new NotImplementedException();
        }

        public override bool StartAzureStorageBlobCopy(Microsoft.WindowsAzure.Storage.Blob.CloudBlob srcBlob, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartAzureStorageBlobCopy(Microsoft.WindowsAzure.Storage.File.CloudFile srcFile, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartAzureStorageBlobCopy(Microsoft.WindowsAzure.Storage.File.CloudFileShare srcShare, string srcFilePath, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartAzureStorageBlobCopy(string sourceUri, string destContainerName, string destBlobName, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartAzureStorageBlobCopy(string srcContainerName, string srcBlobName, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StopAzureStorageBlobCopy(string containerName, string blobName, string copyId, bool force)
        {
            throw new NotImplementedException();
        }

        public override bool StartAzureStorageBlobCopyFromFile(string srcShareName, string srcFilePath, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            throw new NotImplementedException();
        }

        #endregion // Blobs

        #region Azure File

        public override void GetFileShareByName(string fileShareName)
        {
            throw new NotImplementedException();
        }

        public override void GetFileShareByPrefix(string prefix)
        {
            throw new NotImplementedException();
        }

        public override bool GetFileSharesByPrefix(string prefix)
        {
            throw new NotImplementedException();
        }

        public override void NewFileShare(string fileShareName, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override bool NewFileShares(string[] names)
        {
            throw new NotImplementedException();
        }

        public override void NewFileShareFromPipeline()
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureStorageShareQuota(Microsoft.WindowsAzure.Storage.File.CloudFileShare share, int quota)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureStorageShareQuota(string shareName, int quota)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFileShareByName(string fileShareName, bool passThru = false, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveFileShares(string[] names)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFileShareFromPipeline()
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageShareStoredAccessPolicy(string shareName, string policyName, string permissions, DateTime? startTime, DateTime? expiryTime)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageShareSAS(string shareName, string policyName = null, string permissions = null, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureStorageShareStoredAccessPolicy(string shareName, string policyName, string permissions, DateTime? startTime, DateTime? expiryTime, bool noStartTime = false, bool noExpiryTime = false)
        {
            throw new NotImplementedException();
        }

        public override bool ListDirectories(string fileShareName)
        {
            throw new NotImplementedException();
        }

        public override void NewDirectory(Microsoft.WindowsAzure.Storage.File.CloudFileDirectory directory, string directoryName)
        {
            throw new NotImplementedException();
        }

        public override void NewDirectory(Microsoft.WindowsAzure.Storage.File.CloudFileShare fileShare, string directoryName)
        {
            throw new NotImplementedException();
        }

        public override void NewDirectory(string fileShareName, string directoryName, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override bool NewDirectories(string fileShareName, string[] directoryNames)
        {
            throw new NotImplementedException();
        }

        public override void NewDirectoryFromPipeline(string fileShareName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectory(Microsoft.WindowsAzure.Storage.File.CloudFileDirectory directory, string path)
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectory(Microsoft.WindowsAzure.Storage.File.CloudFileShare fileShare, string directoryName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectory(string fileShareName, string directoryName, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveDirectories(string fileShareName, string[] directoryNames)
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectoriesFromPipeline(string fileShareName)
        {
            throw new NotImplementedException();
        }

        public override void GetFile(Microsoft.WindowsAzure.Storage.File.CloudFileDirectory directory, string path = null)
        {
            throw new NotImplementedException();
        }

        public override void GetFile(string fileShareName, string path = null)
        {
            throw new NotImplementedException();
        }

        public override void GetFile(Microsoft.WindowsAzure.Storage.File.CloudFileShare fileShare, string path = null)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageShareStoredAccessPolicy(string shareName, string policyName)
        {
            throw new NotImplementedException();
        }

        public override string GetAzureStorageShareSasFromCmd(string shareName, string policy, string permission = null, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            throw new NotImplementedException();
        }

        public override string GetAzureStorageFileSasFromCmd(string shareName, string filePath, string policy, string permission = null, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageFileSAS(Microsoft.WindowsAzure.Storage.File.CloudFile file, string policyName = null, string permissions = null, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageFileSAS(string shareName, string filePath, string policyName = null, string permissions = null, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(Microsoft.WindowsAzure.Storage.File.CloudFile file, string destination, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(Microsoft.WindowsAzure.Storage.File.CloudFileDirectory directory, string path, string destination, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(Microsoft.WindowsAzure.Storage.File.CloudFileShare fileShare, string path, string destination, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(string fileShareName, string path, string destination, bool overwrite = false, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFiles(string fileShareName, string path, string destination, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public override void UploadFile(Microsoft.WindowsAzure.Storage.File.CloudFileDirectory directory, string source, string path, bool overwrite = false, bool passThru = false)
        {
            throw new NotImplementedException();
        }

        public override void UploadFile(string fileShareName, string source, string path, bool overwrite = false, bool passThru = false, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override void UploadFile(Microsoft.WindowsAzure.Storage.File.CloudFileShare fileShare, string source, string path, bool overwrite = false, bool passThru = false)
        {
            throw new NotImplementedException();
        }

        public override void UploadFilesInFolderFromPipeline(string fileShareName, string folder)
        {
            throw new NotImplementedException();
        }

        public override void UploadFilesFromPipeline(string fileShareName, string localFileName)
        {
            throw new NotImplementedException();
        }

        public override bool GetFileCopyState(Microsoft.WindowsAzure.Storage.File.CloudFile file, object context, bool waitForComplete = false)
        {
            throw new NotImplementedException();
        }

        public override bool GetFileCopyState(string shareName, string filePath, object context, bool waitForComplete = false)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopy(Microsoft.WindowsAzure.Storage.Blob.CloudBlob blob, Microsoft.WindowsAzure.Storage.File.CloudFile destFile, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopy(Microsoft.WindowsAzure.Storage.Blob.CloudBlob blob, string shareName, string filePath, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopy(Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer container, string blobName, string shareName, string filePath, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopy(Microsoft.WindowsAzure.Storage.File.CloudFile srcFile, Microsoft.WindowsAzure.Storage.File.CloudFile destFile, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopy(Microsoft.WindowsAzure.Storage.File.CloudFile srcFile, string shareName, string filePath, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopy(Microsoft.WindowsAzure.Storage.File.CloudFileDirectory dir, string srcFilePath, string shareName, string filePath, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopy(Microsoft.WindowsAzure.Storage.File.CloudFileShare share, string srcFilePath, string shareName, string filePath, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopy(string uri, Microsoft.WindowsAzure.Storage.File.CloudFile destFile, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopy(string uri, string destShareName, string destFilePath, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopyFromBlob(string containerName, string blobName, string shareName, string filePath, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartFileCopyFromFile(string srcShareName, string srcFilePath, string shareName, string filePath, object destContext, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StopFileCopy(Microsoft.WindowsAzure.Storage.File.CloudFile file, string copyId, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override bool StopFileCopy(string shareName, string filePath, string copyId, bool force = true)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFile(Microsoft.WindowsAzure.Storage.File.CloudFile file)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFile(Microsoft.WindowsAzure.Storage.File.CloudFileDirectory directory, string fileName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFile(Microsoft.WindowsAzure.Storage.File.CloudFileShare fileShare, string fileName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFile(string fileShareName, string fileName, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFilesFromPipeline(string fileShareName)
        {
            throw new NotImplementedException();
        }

        #endregion // Azure File

        #region Queue

        public override bool GetAzureStorageQueue(string QueueName)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageQueueByPrefix(string Prefix)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageQueue(string QueueName)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageQueue(string[] QueueNames)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageQueue(string QueueName, bool Force = true)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageQueue(string[] QueueNames, bool Force = true)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageQueueStoredAccessPolicy(string queueName, string policyName, bool Force = true)
        {
            return base.RemoveAzureStorageQueueStoredAccessPolicy(queueName, policyName, Force);
        }

        public override bool RemoveAzureStorageShareStoredAccessPolicy(string shareName, string policyName)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageQueueStoredAccessPolicy(string queueName, string policyName)
        {
            return base.GetAzureStorageQueueStoredAccessPolicy(queueName, policyName);
        }

        public override string GetQueueSasFromCmd(string queueName, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return base.GetQueueSasFromCmd(queueName, policy, permission, startTime, expiryTime, fulluri);
        }

        public override bool NewAzureStorageQueueStoredAccessPolicy(string queueName, string policyName, string permission, DateTime? startTime = null, DateTime? expiryTime = null)
        {
            return base.NewAzureStorageQueueStoredAccessPolicy(queueName, policyName, permission, startTime, expiryTime);
        }

        public override bool NewAzureStorageQueueSAS(string name, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return base.NewAzureStorageQueueSAS(name, policy, permission, startTime, expiryTime, fullUri);
        }

        public override bool SetAzureStorageQueueStoredAccessPolicy(string queueName, string policyName, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            return base.SetAzureStorageQueueStoredAccessPolicy(queueName, policyName, permission, startTime, expiryTime, NoStartTime, NoExpiryTime);
        }

        #endregion // Queue

        #region Table

        public override bool GetAzureStorageTable(string TableName)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageTableByPrefix(string Prefix)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageTable(string TableName)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureStorageTable(string[] TableNames)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageTable(string TableName, bool Force = true)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageTable(string[] TableNames, bool Force = true)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureStorageTableStoredAccessPolicy(string tableName, string policyName, bool Force = true)
        {
            return base.RemoveAzureStorageTableStoredAccessPolicy(tableName, policyName, Force);
        }

        public override bool NewAzureStorageTableStoredAccessPolicy(string tableName, string policyName, string permission, DateTime? startTime = null, DateTime? expiryTime = null)
        {
            return base.NewAzureStorageTableStoredAccessPolicy(tableName, policyName, permission, startTime, expiryTime);
        }

        public override bool SetAzureStorageTableStoredAccessPolicy(string tableName, string policyName, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            return base.SetAzureStorageTableStoredAccessPolicy(tableName, policyName, permission, startTime, expiryTime, NoStartTime, NoExpiryTime);
        }

        public override bool NewAzureStorageTableSAS(string name, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, string startpk = "", string startrk = "", string endpk = "", string endrk = "", SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return base.NewAzureStorageTableSAS(name, policy, permission, startTime, expiryTime, fullUri, startpk, startrk, endpk, endrk);
        }

        public override bool GetAzureStorageTableStoredAccessPolicy(string tableName, string policyName)
        {
            return base.GetAzureStorageTableStoredAccessPolicy(tableName, policyName);
        }

        public override string GetTableSasFromCmd(string tableName, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, string startpk = "", string startrk = "", string endpk = "", string endrk = "", SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return base.GetTableSasFromCmd(tableName, policy, permission, startTime, expiryTime, fulluri, startpk, startrk, endpk, endrk);
        }

        #endregion // Table

        public override void SetVariable(string variableName, object value)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(IEnumerable<Microsoft.Azure.Management.Storage.Models.StorageAccount> accounts)
        {
            base.OutputValidation(accounts);
        }

        public override void OutputValidation(IEnumerable<Microsoft.WindowsAzure.Storage.Blob.BlobContainerPermissions> permissions)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(IEnumerable<Microsoft.WindowsAzure.Storage.Blob.CloudBlob> blobs)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(IEnumerable<Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer> containers)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(IEnumerable<Microsoft.WindowsAzure.Storage.File.CloudFileShare> shares)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(IEnumerable<Microsoft.WindowsAzure.Storage.File.IListFileItem> items)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(IEnumerable<Microsoft.WindowsAzure.Storage.Queue.CloudQueue> queues)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(IEnumerable<Microsoft.WindowsAzure.Storage.Table.CloudTable> tables)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(Microsoft.WindowsAzure.Storage.Shared.Protocol.ServiceProperties serviceProperties, string propertiesType)
        {
            base.OutputValidation(serviceProperties, propertiesType);
        }

        public override void OutputValidation(System.Collections.ObjectModel.Collection<Dictionary<string, object>> comp)
        {
            throw new NotImplementedException();
        }
    }
}
