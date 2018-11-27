// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Management.Storage.ScenarioTest.Util;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using MS.Test.Common.MsTestLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Commands.Storage.Model.ResourceModel;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Azure.Commands.Management.Storage.Models;

namespace Management.Storage.ScenarioTest
{
    /// <summary>
    /// This class is used to create an agent which could run Node.js xplat commands
    /// </summary>
    public class NodeJSAgent : Agent
    {
        private const string NotImplemented = "Not implemented in NodeJS Agent!";
        private const string ExportPathCommand = " export PATH=$PATH:/usr/local/bin/;";
        private const string DOUBLE_SPACE = "CLITEST_DOUBLESPACE_INDICATOR";

        private static string UnlockKeyChainCommand = string.Format(" security -v unlock-keychain \"-p\" \"{0}\";", Test.Data.Get("UserName"));
        private static string UnlockKeyChainOutput = string.Format("unlock-keychain \"-p\" \"{0}\"\n", Test.Data.Get("UserName"));
        private static string ErrorPrefixOutput = "error: ";

        private static Hashtable ExpectedErrorMsgTableNodeJS = new Hashtable() {
                {"GetBlobContentWithNotExistsBlob", "Can not find blob '{0}' in container '{1}'"},
                {"GetBlobContentWithNotExistsContainer", "Can not find blob '{0}' in container '{1}'"},
                {"RemoveBlobWithLease", "There is currently a lease on the blob and no lease ID was specified in the request"},
                {"SetBlobContentWithInvalidBlobType", "The blob type is invalid for this operation"},
                {"SetPageBlobWithInvalidFileSize", "Page blob length must be multiple of 512"},
                {"CreateExistingContainer", "Container '{0}' already exists"},
                {"CreateInvalidContainer", "Container name format is incorrect"},
                {"RemoveNonExistingContainer", "Can not find container '{0}'"},
                {"RemoveNonExistingBlob", "The specified blob does not exist."},
                {"SetBlobContentWithInvalidBlobName", "One of the request inputs is out of range"},
                {"SetContainerAclWithInvalidName", "Container name format is incorrect"},
                {"ShowNonExistingBlob", "Blob {0} in Container {1} doesn't exist"},
                {"ShowNonExistingContainer", "Container {0} doesn't exist"},
                {"UseInvalidAccount", "getaddrinfo"}, //bug#892297
                {"MissingAccountName", "Please set the storage account parameters or one of the following two environment variables to use"},
                {"MissingAccountKey", "Please set the storage account parameters or one of the following two environment variables to use"},
                {"OveruseAccountParams", "Please only define one of them:\n 1. --connection-string\n 2. --account-name and --account-key\n 3. --account-name and --sas"},
                {"CreateExistingTable", "The table specified already exists"},
                {"CreateInvalidTable", "Table name format is incorrect"},
                {"GetNonExistingTable", "Table {0} doesn't exist"},
                {"RemoveNonExistingTable", "Can not find table '{0}'"},
                {"CreateExistingQueue", "The queue specified already exists"},
                {"CreateInvalidQueue", "Queue name format is incorrect"},
                {"GetNonExistingQueue", "Queue {0} doesn't exist"},
                {"RemoveNonExistingQueue", "Can not find queue '{0}'"},
                {"RemoveNonExistingBlobSnapshot", "Can not find snapshot '{0}' of blob '{1}' in container '{2}'"},
                {"RemoveBlobSnapshotWithInvalidOption", "The deleteSnapshots option cannot be included when deleting a specific snapshot using the snapshotId option"},
                {"SnapshotNonExistingBlob", "Blob {0} in Container {1} doesn't exist"},
                {"SnapshotLeaseBlobWithWrongLeaseID", "The lease ID specified did not match the lease ID for the blob"},
                {"LeaseOnNonExistingContainer", "The specified container does not exist"},
                {"LeaseOnNonExistingBlob", "The specified blob does not exist"},
                {"LeaseOnLeasedContainer", "There is already a lease present"},
                {"LeaseOnLeasedBlob", "There is already a lease present"},
                {"LeaseContainerWithInvalidDuration", "The value for one of the HTTP headers is not in the correct format"},
                {"LeaseBlobWithInvalidDuration", "The value for one of the HTTP headers is not in the correct format"},
                {"LeaseContainerWithInvalidID", "Given string \"{0}\" is not valid UUID"},
                {"LeaseBlobWithInvalidID", "Given string \"{0}\" is not valid UUID"},
                {"LeaseWithoutEnoughPermission", "This request is not authorized to perform this operation"},
                {"RenewLeaseOnNonExistingContainer", "The specified container does not exist"},
                {"RenewLeaseOnNonExistingBlob", "The specified blob does not exist"},
                {"RenewNotLeasedContainer", "The lease ID specified did not match the lease ID for the container"},
                {"RenewNotLeasedBlob", "The lease ID specified did not match the lease ID for the blob"},
                {"RenewWithInvalidLeaseID", "Given string \"{0}\" is not valid UUID"},
                {"RenewWithUnmatchLeaseID", "The lease ID specified did not match the lease ID for the "}, 
                {"RenewWithoutEnoughPermission", "This request is not authorized to perform this operation using this permission"},
                {"ChangeLeaseOnNonExistingContainer", "The specified container does not exist"},
                {"ChangeLeaseOnNonExistingBlob", "The specified blob does not exist"},
                {"ChangeWithInvalidLeaseID", "Given string \"{0}\" is not valid UUID"},
                {"ChangeWithUnmatchLeaseID", "The lease ID specified did not match the lease ID for the "},
                {"ChangeWithInvalidProposedLeaseID", "Given string \"{0}\" is not valid UUID"},
                {"ChangeWithoutEnoughPermission", "This request is not authorized to perform this operation using this permission"},  
                {"ReleaseContainerLease", "A lease ID was specified, but the lease for the container has expired"},
                {"ReleaseBlobLease", "A lease ID was specified, but the lease for the blob has expired"},
                {"LeaseAfterReleaseLease", "A lease ID was specified, but the lease for the container has expired"},
                {"ReleaseLeaseOnNonExistingContainer", "The specified container does not exist"},
                {"ReleaseLeaseOnNonExistingBlob", "The specified blob does not exist"},
                {"ReleaseNotLeasedContainer", "The lease ID specified did not match the lease ID for the container"},
                {"ReleaseNotLeasedBlob", "The lease ID specified did not match the lease ID for the blob"},
                {"ReleaseWithInvalidLeaseID", "Given string \"{0}\" is not valid UUID"},
                {"ReleaseWithUnmatchLeaseID", "The lease ID specified did not match the lease ID for the "},
                {"ReleaseWithoutEnoughPermission", "This request is not authorized to perform this operation using this permission"},
                {"BreakContainerLease", "PreconditionFailed"},
                {"BreakBlobLease", "PreconditionFailed"},
                {"BreakContainerLeaseAndAcquireAgain", "There is already a lease present"},
                {"BreakBlobLeaseAndAcquireAgain", "There is already a lease present"},
                {"BreakLeaseOnNonExistingContainer", "The specified container does not exist"},
                {"BreakLeaseOnNonExistingBlob", "The specified blob does not exist"},
                {"BreakNotLeasedContainer", "There is currently no lease on the container"},
                {"BreakNotLeasedBlob", "There is currently no lease on the blob"},
                {"BreakContainerLeaseWithInvalidDuration", "The value for one of the HTTP headers is not in the correct format"},
                {"BreakBlobLeaseWithInvalidDuration", "The value for one of the HTTP headers is not in the correct format"},
                {"BreakLeaseWithoutEnoughPermission", "This request is not authorized to perform this operation"},
        };
        private static readonly Regex ColorIndicatorRegex = new Regex("\x1b\\[[0-9]+m");

        public static string BinaryFileName { get; set; }
        public static int MaxWaitingTime { get; set; }
        public static string WorkingDirectory { get; set; }

        public static OSType AgentOSType = OSType.Windows;
        public static OSConfig AgentConfig = new OSConfig();

        public NodeJSAgent()
        {
            MaxWaitingTime = Constants.DefaultMaxWaitingTime;
            WorkingDirectory = ".";

            //assign the error message table for error validation
            ExpectedErrorMsgTable = ExpectedErrorMsgTableNodeJS;
        }

        internal static void SetProcessInfo(Process p, string category, string argument)
        {
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            p.StartInfo.WorkingDirectory = Test.Data.Get("NodeWorkingDirectory");

            if (AgentOSType == OSType.Windows)
            {
                //usually the file path would be "C:\Windows\System32\cmd.exe"
                p.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
                p.StartInfo.Arguments = "/c";
            }
            else if (AgentOSType == OSType.Linux || AgentOSType == OSType.Mac)
            {
                p.StartInfo.FileName = AgentConfig.PLinkPath;
                p.StartInfo.Arguments = string.Format("-l {0} -i \"{1}\" {2} -P {3}",
                    AgentConfig.UserName, AgentConfig.PrivateKeyPath, AgentConfig.HostName, AgentConfig.Port);

                if (AgentConfig.UseEnvVar)
                {
                    if (!string.IsNullOrEmpty(AgentConfig.ConnectionString))
                    {
                        p.StartInfo.Arguments += string.Format(" export AZURE_STORAGE_CONNECTION_STRING='{0}';", AgentConfig.ConnectionString);
                    }
                    else if (!string.IsNullOrEmpty(AgentConfig.AccountName))
                    {
                        p.StartInfo.Arguments += string.Format(" export AZURE_STORAGE_ACCOUNT={0};export AZURE_STORAGE_ACCESS_KEY={1};",
                            AgentConfig.AccountName, AgentConfig.AccountKey);
                    }
                }

                if (AgentOSType == OSType.Mac)
                {
                    p.StartInfo.Arguments += UnlockKeyChainCommand;
                    p.StartInfo.Arguments += ExportPathCommand;
                }

                // Replace all "..." with '"..."' in argument for sending the commands to linux/Mac by plink
                //   plink uses '...' to identify a string.
                //   Linux uses "..." to identify a string and ' is used in the command when the option is a json string. 
                argument += " ";
                argument = argument.Replace(" \"", " '\"");
                argument = argument.Replace("\" ", "\"' ");
            }

            // replace all double-space parameter according to plink usage
            // On Windows: DOUBLE_SPACE --> "  "
            // With Plink for Linux and Mac: DOUBLE_SPACE --> "'  '"
            if (AgentOSType == OSType.Windows)
            {
                argument = argument.Replace(DOUBLE_SPACE, "\"  \"");
            }
            else
            {
                argument = argument.Replace(DOUBLE_SPACE, "'\"  \"'");
            }

            p.StartInfo.Arguments += string.Format(" azure {0} {1} --json", category, argument);

            string argumentsLog = p.StartInfo.Arguments;
            if (category == "login")
            {
                argumentsLog = Regex.Replace(argumentsLog, " -[p|P] .*", " -p ******");
            }

            Test.Info("NodeJS command: \"{0}\" {1}", p.StartInfo.FileName, argumentsLog);
        }

        public override void ImportAzureSubscription(string settingFile)
        {
            RunNodeJSProcess(string.Format("import \"{0}\"", settingFile), needAccountParam: false, category: "account");
        }

        public override bool SetRmCurrentStorageAccount(string storageAccountName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public override void SetActiveSubscription(string subscriptionId)
        {
            RunNodeJSProcess(string.Format("set \"{0}\"", subscriptionId), needAccountParam: false, category: "account");
        }

        internal bool RunNodeJSProcess(string argument, bool force = false, bool needAccountParam = true, string category = "storage")
        {
            if (force)
            {
                argument += " --quiet";
            }

            if (!AgentConfig.UseEnvVar && needAccountParam)
            {
                argument = UtilBase.AddAccountParameters(argument, AgentConfig);
            }

            Process p = new Process();
            SetProcessInfo(p, category, argument);
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
            p.WaitForExit(MaxWaitingTime);

            string output = string.Empty;
            string error = string.Empty;

            if (!p.HasExited)
            {
                p.Kill();

                printInfo(outputBuffer, errorBuffer, ref output, ref error);

                throw new Exception(string.Format("NodeJS command timeout: cost time > {0}s !", MaxWaitingTime / 1000));
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
                output = parseOutput(output);

                Collection<Dictionary<string, object>> result = null;
                try
                {
                    result = JsonConvert.DeserializeObject<Collection<Dictionary<string, object>>>(output);
                }
                catch (JsonReaderException ex)
                {
                    // write the output to a file for investigation
                    File.WriteAllText(Path.Combine(WorkingDirectory, "output_for_debug.txt"), output);
                    Dictionary<string, object> message = new Dictionary<string, object>();
                    message["output"] = output;
                    Output.Add(message);

                    throw ex;
                }

                if (result != null)
                {
                    Output = result;
                }
            }
            else
            {
                ErrorMessages.Add(error);

                string errFile = "azure.err";
                Test.Info(string.Format("Error details in {0}:", errFile));
                try
                {
                    Test.Info(FileUtil.ReadFileToText(errFile));
                }
                catch (Exception e)
                {
                    Test.Info(string.Format("Load details in azure.err error: %s"), e.Message);
                }

            }

            return bSuccess;
        }

        internal void printInfo(StringBuilder outputBuffer, StringBuilder errorBuffer, ref string output, ref string error)
        {
            error = errorBuffer.ToString();
            if (!string.IsNullOrEmpty(error))
            {
                error = ColorIndicatorRegex.Replace(error, string.Empty);

                if (error.StartsWith(ErrorPrefixOutput, StringComparison.OrdinalIgnoreCase))
                {
                    error = error.Remove(0, ErrorPrefixOutput.Length);
                }

                if (error.StartsWith(UnlockKeyChainOutput))
                {
                    error = error.Remove(0, UnlockKeyChainOutput.Length);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    Test.Verbose("Error:\n{0}", error);
                }
            }

            output = outputBuffer.ToString();
            if (!string.IsNullOrEmpty(output))
            {
                output = ColorIndicatorRegex.Replace(output, string.Empty);
            }
            Test.Verbose("Node Output:\n{0}", output);
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
                    int lineIndex = 0;
                    string[] lines = output.Split('\n');
                    output = "[{";
                    foreach (string line in lines)
                    {
                        int index = line.IndexOf(':');
                        if (index != -1 && line[index + 1] != '/')
                        {
                            output += string.Format("{0}_{1}:\'{2}\',\n", lineIndex++, line.Substring(0, index).Trim(), line.Substring(index + 1).Trim());
                        }
                        else
                        {
                            output += string.Format("{0}_info:\'{1}\',\n", lineIndex++, line.Trim());
                        }
                    }
                    output += "}]";
                }
            }

            return output;
        }

        internal string appendStringOption(string command, string optionName, string optionValue, bool quoted = false, bool onlyNonEmpty = true)
        {
            if (!onlyNonEmpty || !string.IsNullOrEmpty(optionValue))
            {
                if (optionValue.Contains("\""))
                {
                    if (AgentOSType == OSType.Windows)
                    {
                        optionValue = optionValue.Replace("\"", "\"\"");
                    }
                    else
                    {
                        optionValue = optionValue.Replace("$", "\\$");  // escape variable mark
                        optionValue = optionValue.Replace("`", "\\`");  // escape execution mark
                        optionValue = optionValue.Replace("\"", "\\\"");  // double quotation
                    }
                }

                if (quoted)
                {
                    command += string.Format(" {0} \"{1}\" ", optionName, optionValue);
                }
                else
                {
                    command += string.Format(" {0} {1} ", optionName, optionValue);
                }
            }

            return command;
        }

        internal string appendBoolOption(string command, string optionName)
        {
            return command + string.Format(" {0} ", optionName);
        }

        internal string appendBoolOption(string command, string optionName, bool? value)
        {
            if (value.HasValue && value.Value)
            {
                return command + string.Format(" {0} ", optionName);
            }

            return command;
        }

        internal string appendDateTimeOption(string command, string optionName, DateTime? date)
        {
            if (date.HasValue)
            {
                command = appendStringOption(command, optionName, date.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }

            return command;
        }

        internal string appendAccountOption(string command, object context, bool suffix, bool isSource)
        {
            CloudStorageAccount account = context as CloudStorageAccount;
            if (account != null)
            {
                string connectionString = string.Empty;
                string accountCSOption = suffix ? (isSource ? "--connection-string" : "--dest-connection-string") : "--connection-string";
                if (account.Credentials.IsSharedKey)
                {
                    connectionString = string.Format(
                        "AccountName={0};AccountKey={1};",
                        account.Credentials.AccountName,
                        account.Credentials.ExportBase64EncodedKey());
                }
                else if (account.Credentials.IsSAS)
                {
                    connectionString = string.Format("SharedAccessSignature={0};", account.Credentials.SASToken);
                }

                if (account.BlobEndpoint != null)
                {
                    connectionString += string.Format("BlobEndpoint={0};", account.BlobEndpoint.AbsoluteUri);
                }

                if (account.TableEndpoint != null)
                {
                    connectionString += string.Format("TableEndpoint={0};", account.TableEndpoint.AbsoluteUri);
                }

                if (account.QueueEndpoint != null)
                {
                    connectionString += string.Format("QueueEndpoint={0};", account.QueueEndpoint.AbsoluteUri);
                }

                if (account.FileEndpoint != null)
                {
                    connectionString += string.Format("FileEndpoint={0};", account.FileEndpoint.AbsoluteUri);
                }

                command = appendStringOption(command, accountCSOption, connectionString, quoted: true);
            }

            return command;
        }

        internal string appendHashOption(string command, string optionName, Hashtable[] tags)
        {
            if (tags != null)
            {
                if (tags.Length > 0 && tags[0] != null && tags[0].Keys.Count > 0)
                {
                    return command + string.Format(" {0} {1} ", optionName, Utility.ConvertTables(tags));
                }
                else
                {
                    // Double space to indicate the empty string which is a constrain on CLI
                    return command + string.Format(" {0} {1} ", optionName, DOUBLE_SPACE);
                }
            }

            return command;
        }

        internal void AssertMandatoryParameter(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new Exception(string.Format("The required parameter {0} is missing.", name));
            }
        }

        public override bool ChangeCLIMode(Constants.Mode mode)
        {
            return RunNodeJSProcess(string.Format("mode {0}", mode), needAccountParam: false, category: "config");
        }

        public override bool Login()
        {
            return RunNodeJSProcess(
                string.Format("-u {0} -p {1} --tenant {2} --service-principal",
                    Test.Data.Get("AADClient"),
                    Test.Data.Get("AADPassword"),
                    Test.Data.Get("AADRealm")), 
                needAccountParam: false, 
                category: "login");
        }

        public override bool Logout()
        {
            try
            {
                RunNodeJSProcess(string.Format("{0}", Test.Data.Get("AADClient")), needAccountParam: false, category: "logout");
                return true;
            }
            catch (Exception ex)
            {
                Test.Info("Logout exception: {0}\n Info: {1}", ex, Output);
                return false;
            }
        }

        public override bool ShowAzureStorageAccountConnectionString(string argument, string resourceGroupName = null)
        {
            if (string.IsNullOrEmpty(resourceGroupName))
            {
                return RunNodeJSProcess(string.Format("account connectionstring show {0}", argument), needAccountParam: false);
            }
            else
            {
                return RunNodeJSProcess(string.Format("account connectionstring show {0} --resource-group {1}", argument, resourceGroupName), needAccountParam: false);
            }
        }

        public override bool ShowAzureStorageAccountKeys(string accountName)
        {
            return RunNodeJSProcess(string.Format("account keys list {0}", accountName), needAccountParam: false);
        }

        public override bool ShowSRPAzureStorageAccountKeys(string resourceGroupName, string accountName)
        {
            return RunNodeJSProcess(string.Format("account keys list {0} --resource-group {1}", accountName, resourceGroupName), needAccountParam: false);
        }

        public override bool RenewAzureStorageAccountKeys(string accountName, Constants.AccountKeyType type)
        {
            return this.RenewSRPAzureStorageAccountKeys(null, accountName, type);
        }

        public override bool RenewSRPAzureStorageAccountKeys(string resourceGroupName, string accountName, Constants.AccountKeyType type)
        {
            string command = string.Format("account keys renew {0}", accountName);
            if (type == Constants.AccountKeyType.Primary)
            {
                command = appendStringOption(command, "--primary", string.Empty, onlyNonEmpty: false);
            }
            else if (type == Constants.AccountKeyType.Secondary)
            {
                command = appendStringOption(command, "--secondary", string.Empty, onlyNonEmpty: false);
            }
            else
            {
                command = appendStringOption(command, "--INVALIDTYPE", string.Empty, onlyNonEmpty: false);
            }

            command = appendStringOption(command, "--resource-group", resourceGroupName);
            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool CreateAzureStorageAccount(string accountName, string subscription, string label, string description, string location, string affinityGroup, string type, bool? geoReplication = null)
        {
            string command = string.Format("account create {0}", accountName);
            command = appendStringOption(command, "--subscription", subscription);
            command = appendStringOption(command, "--label", label);
            command = appendStringOption(command, "--description", description, true);
            command = appendStringOption(command, "--location", location, true);
            command = appendStringOption(command, "--affinity-group", affinityGroup, true);
            command = appendStringOption(command, "--type", type);
            if (geoReplication.HasValue)
            {
                if (geoReplication.Value)
                {
                    command = appendBoolOption(command, "--geoReplication");
                }
                else
                {
                    command = appendBoolOption(command, "--disable-geoReplication");
                }
            }

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool SetAzureStorageAccount(string accountName, string label, string description, string type, bool? geoReplication = null)
        {
            string command = string.Format("account set {0}", accountName);
            command = appendStringOption(command, "--label", label);
            command = appendStringOption(command, "--description", description);
            command = appendStringOption(command, "--type", type);
            if (geoReplication.HasValue)
            {
                if (geoReplication.Value)
                {
                    command = appendBoolOption(command, "--geoReplication");
                }
                else
                {
                    command = appendBoolOption(command, "--disable-geoReplication");
                }
            }

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool DeleteAzureStorageAccount(string accountName)
        {
            string command = string.Format("account delete {0}", accountName);

            return RunNodeJSProcess(command, force: true, needAccountParam: false);
        }

        public override bool ShowAzureStorageAccount(string accountName)
        {
            string command = string.Empty;
            if (string.IsNullOrEmpty(accountName))
            {
                command = string.Format("account list {0}", accountName);
            }
            else
            {
                command = string.Format("account show {0}", accountName);
            }

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool CreateSRPAzureStorageAccount(
            string resourceGroupName, 
            string accountName, 
            string skuName, 
            string location, 
            Hashtable[] tags = null, 
            Kind? kind = null, 
            Constants.EncryptionSupportServiceEnum? enableEncryptionService = null, 
            AccessTier? accessTier = null, 
            string customDomain = null, 
            bool? useSubdomain = null,
            bool? enableHttpsTrafficOnly = null,
            bool AssignIdentity = false,
            PSNetworkRuleSet networkAcl = null)
        {
            string command = string.Format("account create {0}", accountName);
            command = appendStringOption(command, "--resource-group", resourceGroupName);
            command = appendStringOption(command, "--location", location, true);
            command = appendStringOption(command, "--sku-name", skuName);
            if (kind == null)
            {
                command = appendStringOption(command, "--kind", Kind.Storage.ToString());
            }
            else
            {
                command = appendStringOption(command, "--kind", kind.Value.ToString());
            }
            command = appendHashOption(command, "--tags", tags);
            if (accessTier != null)
            {
                command = appendStringOption(command, "--access-tier", accessTier.ToString());
            }
            if (enableEncryptionService != null)
            {
                command = appendStringOption(command, "--enable-encryption-service", enableEncryptionService.ToString().Replace(" ", string.Empty));
            }
            if (!string.IsNullOrEmpty(customDomain))
            {
                command = appendStringOption(command, "--custom-domain", customDomain);
            }
            if (useSubdomain.HasValue && useSubdomain.Value)
            {
                command = appendBoolOption(command, "--subdomain");
            }

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool SetSRPAzureStorageAccount(
            string resourceGroupName, 
            string accountName, 
            string skuName = null, 
            Hashtable[] tags = null, 
            Constants.EncryptionSupportServiceEnum? enableEncryptionService = null, 
            Constants.EncryptionSupportServiceEnum? disableEncryptionService = null, 
            AccessTier? accessTier = null, 
            string customDomain = null, 
            bool? useSubdomain = null,
            bool? enableHttpsTrafficOnly = null,
            bool AssignIdentity = false,
            bool StorageEncryption = false,
            PSNetworkRuleSet networkAcl = null, 
            Kind? kind = null)
        {
            string command = string.Format("account set {0}", accountName);
            command = appendStringOption(command, "--resource-group", resourceGroupName);
            command = appendStringOption(command, "--sku-name", skuName);
            command = appendHashOption(command, "--tags", tags);
            if (enableEncryptionService != null)
            {
                command = appendStringOption(command, "--enable-encryption-service", enableEncryptionService.ToString());
            }
            if (disableEncryptionService != null)
            {
                command = appendStringOption(command, "--disable-encryption-service", disableEncryptionService.ToString());
            }
            if (accessTier != null)
            {
                command = appendStringOption(command, "--access-tier", accessTier.ToString());
            }
            if (customDomain != null)
            {
                if (!string.IsNullOrEmpty(customDomain))
                {
                    command = appendStringOption(command, "--custom-domain", customDomain, quoted: true);
                }
                else
                {
                    command = appendStringOption(command, "--custom-domain", "  ", quoted: true);
                }
            }
            if (useSubdomain.HasValue && useSubdomain.Value)
            {
                command = appendBoolOption(command, "--subdomain");
            }

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool SetSRPAzureStorageAccountTags(string resourceGroupName, string accountName, Hashtable[] tags)
        {
            string command = string.Format("account set {0}", accountName);
            command = appendStringOption(command, "--resource-group", resourceGroupName);
            command = appendHashOption(command, "--tags", tags);

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool SetSRPAzureStorageAccountCustomDomain(string resourceGroupName, string accountName, string customDomain, bool? useSubdomain)
        {
            if (string.IsNullOrEmpty(customDomain))
            {
                customDomain = "  ";
            }

            string command = string.Format("account set {0}", accountName);
            command = appendStringOption(command, "--resource-group", resourceGroupName);
            command = appendStringOption(command, "--custom-domain", customDomain, quoted: true);
            command = appendBoolOption(command, "--subdomain", useSubdomain);

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool DeleteSRPAzureStorageAccount(string resourceGroupName, string accountName)
        {
            string command = string.Format("account delete {0}", accountName);
            command = appendStringOption(command, "--resource-group", resourceGroupName);

            return RunNodeJSProcess(command, force: true, needAccountParam: false);
        }

        public override bool ShowSRPAzureStorageAccount(string resourceGroupName, string accountName, bool IncludeGeoReplicationStats = false)
        {
            string command = string.Empty;
            if (string.IsNullOrEmpty(accountName))
            {
                command = string.Format("account list {0}", accountName);
            }
            else
            {
                command = string.Format("account show {0}", accountName);
            }
            command = appendStringOption(command, "--resource-group", resourceGroupName);

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool CheckNameAvailability(string accountName)
        {
            string command = string.Format("account check \"{0}\"", accountName);

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool GetAzureStorageUsage(string Location = null)
        {
            string command = string.Format("account usage show");

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool NewAzureStorageContainer(string containerName)
        {
            return RunNodeJSProcess(string.Format("container create \"{0}\"", containerName));
        }

        public override bool NewAzureStorageContainer(string[] containerNames)
        {
            return BatchOperation(MethodBase.GetCurrentMethod().Name, containerNames);
        }

        public override bool GetAzureStorageContainer(string containerName)
        {
            return RunNodeJSProcess(string.Format("container list \"{0}\"", containerName));
        }

        // this command is only for nodejs
        public bool ShowAzureStorageContainer(string containerName, string leaseId = null)
        {
            string command = "container show";
            command = appendStringOption(command, "--container", containerName, quoted: true);

            if (!string.IsNullOrEmpty(leaseId))
            {
                command = appendStringOption(command, "--lease ", leaseId);
            }

            return RunNodeJSProcess(command);
        }

        public override bool GetAzureStorageContainerByPrefix(string prefix)
        {
            return RunNodeJSProcess("container list " + prefix);
        }

        public override bool SetAzureStorageContainerACL(string containerName, BlobContainerPublicAccessType publicAccess, string leaseId = null, bool passThru = true)
        {
            string command = "container set";
            command = appendStringOption(command, "--container", containerName, quoted: true);
            command = appendStringOption(command, "--permission", publicAccess.ToString());

            if (!string.IsNullOrEmpty(leaseId))
            {
                command = appendStringOption(command, "--lease ", leaseId);
            }

            return RunNodeJSProcess(command);
        }

        public override bool SetAzureStorageContainerACL(string[] containerNames, BlobContainerPublicAccessType publicAccess, bool passThru = true)
        {
            return BatchOperation(MethodBase.GetCurrentMethod().Name, containerNames, publicAccess, passThru);
        }

        public override bool RemoveAzureStorageContainer(string containerName, string leaseId = null, bool force = true)
        {
            string command = "container delete";
            command = appendStringOption(command, "--container", containerName, quoted: true);

            if (!string.IsNullOrEmpty(leaseId))
            {
                command = appendStringOption(command, "--lease ", leaseId);
            }

            return RunNodeJSProcess(command, force);
        }

        public override bool RemoveAzureStorageContainer(string[] containerNames, bool force = true)
        {
            return BatchOperation(MethodBase.GetCurrentMethod().Name, containerNames, force);
        }

        public override bool NewAzureStorageQueue(string queueName)
        {
            return RunNodeJSProcess("queue create " + queueName);
        }

        public override bool NewAzureStorageQueue(string[] queueNames)
        {
            bool result = true;
            foreach (string queue in queueNames)
            {
                result = NewAzureStorageQueue(queue) && result;
            }

            return result;
        }

        public override bool GetAzureStorageQueue(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                return RunNodeJSProcess("queue list");
            }
            else
            {
                return RunNodeJSProcess("queue show " + queueName);
            }
        }

        public override bool GetAzureStorageQueueByPrefix(string prefix)
        {
            return RunNodeJSProcess("queue list " + prefix);
        }

        public override bool RemoveAzureStorageQueue(string queueName, bool force = true)
        {
            return RunNodeJSProcess("queue delete " + queueName, force);
        }

        public override bool RemoveAzureStorageQueue(string[] queueNames, bool force = true)
        {
            bool result = true;
            foreach (string queue in queueNames)
            {
                result = RemoveAzureStorageQueue(queue, force) && result;
            }

            return result;
        }

        public override bool SetAzureStorageBlobContent(string fileName, string containerName, BlobType type, string blobName = "",
            bool force = true, int concurrentCount = -1, Hashtable properties = null, Hashtable metadata = null, PremiumPageBlobTier? premiumPageBlobTier = null)
        {
            if (AgentOSType != OSType.Windows)
            {
                fileName = FileUtil.GetLinuxPath(fileName);
            }

            string parameter = string.Format("blob upload \"{0}\" \"{1}\"", fileName, containerName);
            if (!string.IsNullOrEmpty(blobName))
            {
                parameter += " \"" + blobName + "\"";
            }

            if (type == BlobType.PageBlob)
            {
                parameter += " --blobtype page ";
            }
            else if (type == BlobType.AppendBlob)
            {
                parameter += " --blobtype append ";
            }
            else
            {
                // Randomly set block blob explictly or use default type (block blob)
                Random random = new Random();
                if (random.Next(0, 2) > 0)
                {
                    parameter += " --blobtype block ";
                }
            }

            if (properties != null)
            {
                parameter += " --properties " + Utility.ConvertTable(properties);
            }

            if (metadata != null)
            {
                parameter += " --metadata " + Utility.ConvertTable(metadata);
            }
            return RunNodeJSProcess(parameter, force);
        }

        public override bool GetAzureStorageBlobContent(string blobName, string fileName, string containerName,
            bool force = true, int concurrentCount = -1, bool CheckMd5 = false)
        {
            //Trim the \ for the destination path
            fileName = fileName.TrimEnd('\\');

            if (AgentOSType != OSType.Windows)
            {
                fileName = FileUtil.GetLinuxPath(fileName);
            }

            return RunNodeJSProcess(string.Format("blob download \"{0}\" \"{1}\" \"{2}\"", containerName, blobName, fileName), force);
        }

        public override bool UploadLocalFiles(string dirPath, string containerName, BlobType blobType, bool force = true, int concurrentCount = -1)
        {
            if (AgentOSType != OSType.Windows)
            {
                dirPath = FileUtil.GetLinuxPath(dirPath);
            }

            return BatchOperation(MethodBase.GetCurrentMethod().Name, Directory.EnumerateFiles(dirPath).ToArray(), containerName, blobType, "", force, concurrentCount);
        }

        /// <summary>
        /// Get all the blob names in one container
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        internal string[] GetBlobNames(string containerName)
        {
            List<string> blobNames = new List<string>();
            if (!GetAzureStorageBlob("*", containerName))
            {
                return blobNames.ToArray();
            }

            foreach (var blob in Output)
            {
                blobNames.Add(blob["name"].ToString());
            }

            return blobNames.ToArray();
        }

        public override bool DownloadBlobFiles(string dirPath, string containerName, bool force = true, int concurrentCount = -1)
        {
            string[] blobNames = GetBlobNames(containerName);
            if (blobNames.Length > 0)
            {
                return BatchOperation(MethodBase.GetCurrentMethod().Name, blobNames, dirPath, containerName, force, concurrentCount);
            }
            else
            {
                return false;
            }
        }

        public override bool GetAzureStorageBlob(string blobName, string containerName, bool IncludeDeleted = false)
        {
            if (string.IsNullOrEmpty(blobName) || (blobName.Contains("*") || blobName.Contains("?")))
            {
                return RunNodeJSProcess(string.Format("blob list \"{0}\" \"{1}\"", containerName, blobName));
            }
            else
            {
                return RunNodeJSProcess(string.Format("blob show \"{0}\" \"{1}\"", containerName, blobName));
            }
        }

        // this command is nodejs specific
        public bool ShowAzureStorageBlob(string blobName, string containerName, string leaseId = null)
        {
            string command = "blob show";
            command = appendStringOption(command, "--container", containerName, quoted: true);
            command = appendStringOption(command, "--blob", blobName, quoted: true);

            if (!string.IsNullOrEmpty(leaseId))
            {
                command = appendStringOption(command, "--lease ", leaseId);
            }

            return RunNodeJSProcess(command);
        }

        public override bool GetAzureStorageBlobByPrefix(string prefix, string containerName)
        {
            return RunNodeJSProcess(string.Format("blob list \"{0}\" {1}", containerName, prefix));
        }

        public override bool RemoveAzureStorageBlob(string blobName, string containerName, string snapshotId= "", string leaseId = null, bool? onlySnapshot = null, bool force = true)
        {
            string command = "blob delete";
            command = appendStringOption(command, "", containerName, quoted: true);
            command = appendStringOption(command, "", blobName, quoted: true);
            if (!string.IsNullOrEmpty(snapshotId))
            {
                command = appendStringOption(command, "--snapshot", snapshotId, quoted: true);
            }
            if (onlySnapshot.HasValue)
            {
                if (onlySnapshot.Value)
                {
                    command = appendStringOption(command, "--delete-snapshots", "only");
                }
                else
                {
                    command = appendStringOption(command, "--delete-snapshots", "include");
                }
            }

            if (!string.IsNullOrEmpty(leaseId))
            {
                command = appendStringOption(command, "--lease ", leaseId);
            }


            return RunNodeJSProcess(command, force);
        }

        public override bool NewAzureStorageTable(string tableName)
        {
            return RunNodeJSProcess("table create " + tableName);
        }

        public override bool NewAzureStorageTable(string[] tableNames)
        {
            bool result = true;
            foreach (string table in tableNames)
            {
                result = NewAzureStorageTable(table) && result;
            }

            return result;
        }

        public override bool GetAzureStorageTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return RunNodeJSProcess("table list");
            }
            else
            {
                return RunNodeJSProcess("table show " + tableName);
            }
        }

        public override bool GetAzureStorageTableByPrefix(string prefix)
        {
            return RunNodeJSProcess("table list " + prefix);
        }

        public override bool RemoveAzureStorageTable(string tableName, bool force = true)
        {
            return RunNodeJSProcess("table delete " + tableName, force);
        }

        public override bool RemoveAzureStorageTable(string[] tableNames, bool force = true)
        {
            bool result = true;
            foreach (string table in tableNames)
            {
                result = RemoveAzureStorageTable(table, force) && result;
            }

            return result;
        }

        public override bool StartAzureStorageBlobCopy(string sourceUri, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            string command = "blob copy start";
            command = appendStringOption(command, "", sourceUri, quoted: true);
            command = appendStringOption(command, "", destContainerName, quoted: true);
            command = appendStringOption(command, "--dest-blob", destBlobName, quoted: true);

            return RunNodeJSProcess(command, force);
        }

        public override bool StartAzureStorageBlobCopy(string srcContainerName, string srcBlobName, string destContainerName, string destBlobName, string sourceLease = null, string destLease = null, object destContext = null, bool force = true, PremiumPageBlobTier? premiumPageBlobTier = null)
        {
            string command = "blob copy start";
            command = appendStringOption(command, "--source-container", srcContainerName, quoted: true);
            command = appendStringOption(command, "--source-blob", srcBlobName, quoted: true);
            command = appendStringOption(command, "--dest-container", destContainerName, quoted: true);
            command = appendStringOption(command, "--dest-blob", destBlobName, quoted: true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            if (!string.IsNullOrEmpty(sourceLease))
            {
                command = appendStringOption(command, "--source-lease ", sourceLease);
            }

            if (!string.IsNullOrEmpty(destLease))
            {
                command = appendStringOption(command, "--dest-lease ", destLease);
            }

            return RunNodeJSProcess(command, force);
        }

        public override bool StartAzureStorageBlobCopy(CloudBlob srcBlob, string destContainerName, string destBlobName, object destContext = null, bool force = true, PremiumPageBlobTier? premiumPageBlobTier = null)
        {
            string command = "blob copy start";
            command = appendStringOption(command, "", srcBlob.SnapshotQualifiedUri.AbsoluteUri, quoted: true);
            command = appendStringOption(command, "", destContainerName, quoted: true);
            command = appendStringOption(command, "--dest-blob", destBlobName, quoted: true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force);
        }

        public override bool StartAzureStorageBlobCopyFromFile(string srcShareName, string srcFilePath, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            string url = string.Empty;

            string command = "blob copy start";
            command = appendStringOption(command, "--source-share", srcShareName);
            command = appendStringOption(command, "--source-path", srcFilePath);
            command = appendStringOption(command, "--dest-container", destContainerName);
            command = appendStringOption(command, "--dest-blob", destBlobName);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force);
        }

        public override bool StartAzureStorageBlobCopy(CloudFileShare srcShare, string srcFilePath, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            string url = string.Empty;

            string command = "blob copy start";
            command = appendStringOption(command, "--source-share", srcShare.Name);
            command = appendStringOption(command, "--source-path", srcFilePath);
            command = appendStringOption(command, "--dest-container", destContainerName);
            command = appendStringOption(command, "--dest-blob", destBlobName);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force);
        }

        public override bool StartAzureStorageBlobCopy(CloudFile srcFile, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            string fileUri = GetAzureStorageFileSasFromCmd(srcFile.Share.Name, CloudFileUtil.GetFullPath(srcFile), null, "r", null, DateTime.UtcNow.AddHours(1), true);

            string command = "blob copy start";
            command = appendStringOption(command, "--source-uri", fileUri, quoted: true, onlyNonEmpty: true);
            command = appendStringOption(command, "--dest-container", destContainerName);
            command = appendStringOption(command, "--dest-blob", destBlobName);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool GetAzureStorageBlobCopyState(string containerName, string blobName, bool waitForComplete)
        {
            return RunNodeJSProcess(string.Format("blob copy show \"{0}\" \"{1}\"", containerName, blobName));
        }

        public override bool GetAzureStorageBlobCopyState(CloudBlob blob, object context, bool waitForComplete)
        {
            string command = "blob copy show";
            command = appendStringOption(command, "", blob.Container.Name, quoted: true);
            command = appendStringOption(command, "", blob.Name, quoted: true);
            command = appendAccountOption(command, context, suffix: false, isSource: false);

            return RunNodeJSProcess(command, needAccountParam: context == null);
        }

        public override bool StopAzureStorageBlobCopy(string containerName, string blobName, string copyId, bool force)
        {
            AssertMandatoryParameter("--copy-id", copyId);
            return RunNodeJSProcess(string.Format("blob copy stop \"{0}\" \"{1}\" \"{2}\"", containerName, blobName, copyId));
        }

        public override bool SnapshotAzureStorageBlob(string containerName, string blobName, string leaseId = null)
        {
            string command = "blob snapshot";
            command = appendStringOption(command, "--container", containerName, quoted: true);
            command = appendStringOption(command, "--blob ", blobName, quoted: true);
            if (!string.IsNullOrEmpty(leaseId))
            {
                command = appendStringOption(command, "--lease ", leaseId);
            }

            return RunNodeJSProcess(command);
        }

        public override bool AcquireLease(string containerName, string blobName, string proposedLeaseId = null, int duration = -1)
        {
            string command;
            if (string.IsNullOrEmpty(blobName))
            {
                command = "container lease acquire";
            }
            else
            {
                command = "blob lease acquire";
                command = appendStringOption(command, "--blob ", blobName, quoted: true);
            }

            command = appendStringOption(command, "--container", containerName, quoted: true);

            if (!string.IsNullOrEmpty(proposedLeaseId))
            {
                command = appendStringOption(command, "--proposed-id", proposedLeaseId);
            }

            if (duration != -1)
            {
                command = appendStringOption(command, "--duration", duration.ToString());
            }

            return RunNodeJSProcess(command);
        }

        public override bool RenewLease(string containerName, string blobName, string leaseId)
        {
            string command;
            if (string.IsNullOrEmpty(blobName))
            {
                command = "container lease renew";
            }
            else
            {
                command = "blob lease renew";
                command = appendStringOption(command, "--blob ", blobName, quoted: true);
            }

            command = appendStringOption(command, "--container", containerName, quoted: true);

            if (!string.IsNullOrEmpty(leaseId))
            {
                command = appendStringOption(command, "--lease", leaseId);
            }

            return RunNodeJSProcess(command);
        }

        public override bool ChangeLease(string containerName, string blobName, string leaseId, string proposedLeaseId)
        {
            string command;
            if (string.IsNullOrEmpty(blobName))
            {
                command = "container lease change";
            }
            else
            {
                command = "blob lease change";
                command = appendStringOption(command, "--blob ", blobName, quoted: true);
            }

            command = appendStringOption(command, "--container", containerName, quoted: true);

            if (!string.IsNullOrEmpty(leaseId))
            {
                command = appendStringOption(command, "--lease", leaseId);
            }

            if (!string.IsNullOrEmpty(proposedLeaseId))
            {
                command = appendStringOption(command, "--proposed-id", proposedLeaseId);
            }

            return RunNodeJSProcess(command);
        }

        public override bool ReleaseLease(string containerName, string blobName, string leaseId)
        {
            string command;
            if (string.IsNullOrEmpty(blobName))
            {
                command = "container lease release";
            }
            else
            {
                command = "blob lease release";
                command = appendStringOption(command, "--blob ", blobName, quoted: true);
            }

            command = appendStringOption(command, "--container", containerName, quoted: true);

            if (!string.IsNullOrEmpty(leaseId))
            {
                command = appendStringOption(command, "--lease", leaseId);
            }

            return RunNodeJSProcess(command);
        }

        public override bool BreakLease(string containerName, string blobName, int duration = 0)
        {
            string command;
            if (string.IsNullOrEmpty(blobName))
            {
                command = "container lease break";
            }
            else
            {
                command = "blob lease break";
                command = appendStringOption(command, "--blob ", blobName, quoted: true);
            }

            command = appendStringOption(command, "--container", containerName, quoted: true);

            if (duration > 0)
            {
                command = appendStringOption(command, "--duration", duration.ToString());
            }

            return RunNodeJSProcess(command);
        }

        public override bool StartFileCopyFromBlob(string containerName, string blobName, string shareName, string filePath, object destContext, bool force = true)
        {
            string command = "file copy start";
            command = appendStringOption(command, "--source-container", containerName, quoted: true);
            command = appendStringOption(command, "--source-blob", blobName, quoted: true);
            command = appendStringOption(command, "--dest-share", shareName);
            command = appendStringOption(command, "--dest-path", filePath, true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force);
        }

        public bool StartFileCopyFromBlob(CloudBlob blob, string shareName, string filePath, object destContext, bool force = true)
        {
            string command = "file copy start";
            command = appendStringOption(command, "--source-uri", blob.StorageUri.PrimaryUri.AbsoluteUri, quoted: true);
            command = appendStringOption(command, "--dest-share", shareName);
            command = appendStringOption(command, "--dest-path", filePath, true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StartFileCopyFromFile(string srcShareName, string srcFilePath, string shareName, string filePath, object destContext, bool force = true)
        {
            string command = "file copy start";
            command = appendStringOption(command, "--source-share", srcShareName);
            command = appendStringOption(command, "--source-path", srcFilePath);
            command = appendStringOption(command, "--dest-share", shareName);
            command = appendStringOption(command, "--dest-path", filePath, true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force);
        }

        public override bool GetFileCopyState(string shareName, string filePath, object context, bool waitForComplete = false)
        {
            string command = "file copy show";
            command = appendStringOption(command, "", shareName, quoted: true);
            command = appendStringOption(command, "", filePath, quoted: true);
            command = appendAccountOption(command, context, suffix: false, isSource: false);

            return RunNodeJSProcess(command, needAccountParam: false);
        }

        public override bool GetFileCopyState(CloudFile file, object context, bool waitForComplete = false)
        {
            return GetFileCopyState(file.Share.Name, CloudFileUtil.GetFullPath(file), context, waitForComplete);
        }

        public override bool StartFileCopy(CloudBlobContainer container, string blobName, string shareName, string filePath, object destContext, bool force = true)
        {
            CloudStorageAccount srcContext = new CloudStorageAccount(container.ServiceClient.Credentials, container.ServiceClient.BaseUri, null, null, null);

            string command = "file copy start";
            command = appendStringOption(command, "--source-container", container.Name, quoted: true);
            command = appendStringOption(command, "--source-blob", blobName, quoted: true);
            command = appendStringOption(command, "--dest-share", shareName);
            command = appendStringOption(command, "--dest-path", filePath, true);
            command = appendAccountOption(command, srcContext, suffix: false, isSource: true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StartFileCopy(CloudBlob blob, string shareName, string filePath, object destContext, bool force = true)
        {
            string command = "file copy start";
            command = appendStringOption(command, "--source-uri", blob.StorageUri.PrimaryUri.AbsoluteUri, quoted: true);
            command = appendStringOption(command, "--dest-share", shareName);
            command = appendStringOption(command, "--dest-path", filePath, true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StartFileCopy(CloudBlob blob, CloudFile destFile, object destContext, bool force = true)
        {
            CloudStorageAccount srcContext = new CloudStorageAccount(blob.ServiceClient.Credentials, blob.ServiceClient.BaseUri, null, null, null);

            string command = "file copy start";
            command = appendStringOption(command, "--source-container", blob.Container.Name, quoted: true);
            command = appendStringOption(command, "--source-blob", blob.Name, quoted: true);
            command = appendStringOption(command, "--dest-share", destFile.Share.Name);
            command = appendStringOption(command, "--dest-path", CloudFileUtil.GetFullPath(destFile), true);
            command = appendAccountOption(command, srcContext, suffix: false, isSource: true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StartFileCopy(CloudFileShare share, string srcFilePath, string shareName, string filePath, object destContext, bool force = true)
        {
            CloudStorageAccount srcContext = new CloudStorageAccount(share.ServiceClient.Credentials, null, null, null, share.ServiceClient.BaseUri);

            string command = "file copy start";
            command = appendStringOption(command, "--source-share", share.Name);
            command = appendStringOption(command, "--source-path", srcFilePath);
            command = appendStringOption(command, "--dest-share", shareName);
            command = appendStringOption(command, "--dest-path", filePath, true);
            command = appendAccountOption(command, srcContext, suffix: false, isSource: true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StartFileCopy(CloudFileDirectory dir, string srcFilePath, string shareName, string filePath, object destContext, bool force = true)
        {
            CloudStorageAccount srcContext = new CloudStorageAccount(dir.ServiceClient.Credentials, null, null, null, dir.ServiceClient.BaseUri);

            string command = "file copy start";
            command = appendStringOption(command, "--source-share", dir.Share.Name);
            command = appendStringOption(command, "--source-path", CloudFileUtil.GetFullPath(dir) + '/' + srcFilePath);
            command = appendStringOption(command, "--dest-share", shareName);
            command = appendStringOption(command, "--dest-path", filePath);
            command = appendAccountOption(command, srcContext, suffix: false, isSource: true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StartFileCopy(CloudFile srcFile, string shareName, string filePath, object destContext, bool force = true)
        {
            CloudStorageAccount srcContext = new CloudStorageAccount(srcFile.ServiceClient.Credentials, null, null, null, srcFile.ServiceClient.BaseUri);

            string command = "file copy start";
            command = appendStringOption(command, "--source-share", srcFile.Share.Name);
            command = appendStringOption(command, "--source-path", CloudFileUtil.GetFullPath(srcFile));
            command = appendStringOption(command, "--dest-share", shareName);
            command = appendStringOption(command, "--dest-path", filePath, true);
            command = appendAccountOption(command, srcContext, suffix: false, isSource: true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StartFileCopy(CloudFile srcFile, CloudFile destFile, bool force = true)
        {
            CloudStorageAccount srcContext = new CloudStorageAccount(srcFile.ServiceClient.Credentials, null, null, null, srcFile.ServiceClient.BaseUri);
            CloudStorageAccount destContext = new CloudStorageAccount(destFile.ServiceClient.Credentials, null, null, null, destFile.ServiceClient.BaseUri);

            string command = "file copy start";
            command = appendStringOption(command, "--source-share", srcFile.Share.Name);
            command = appendStringOption(command, "--source-path", CloudFileUtil.GetFullPath(srcFile));
            command = appendStringOption(command, "--dest-share", destFile.Share.Name);
            command = appendStringOption(command, "--dest-path", CloudFileUtil.GetFullPath(destFile));
            command = appendAccountOption(command, srcContext, suffix: false, isSource: true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StartFileCopy(string uri, string destShareName, string destFilePath, object destContext, bool force = true)
        {
            string command = "file copy start";
            command = appendStringOption(command, "--source-uri", uri, quoted: true);
            command = appendStringOption(command, "--dest-share", destShareName);
            command = appendStringOption(command, "--dest-path", destFilePath, true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StartFileCopy(string uri, CloudFile destFile, bool force = true)
        {
            CloudStorageAccount destContext = new CloudStorageAccount(destFile.ServiceClient.Credentials, null, null, null, destFile.ServiceClient.BaseUri);

            string command = "file copy start";
            command = appendStringOption(command, "--source-uri", uri, quoted: true);
            command = appendStringOption(command, "--dest-share", destFile.Share.Name);
            command = appendStringOption(command, "--dest-path", CloudFileUtil.GetFullPath(destFile), true);
            command = appendAccountOption(command, destContext, suffix: true, isSource: false);

            return RunNodeJSProcess(command, force, needAccountParam: false);
        }

        public override bool StopFileCopy(string shareName, string filePath, string copyId, bool force = true)
        {
            AssertMandatoryParameter("--copy-id", copyId);

            string command = "file copy stop";
            command = appendStringOption(command, "--share", shareName);
            command = appendStringOption(command, "--path", filePath, true);
            command = appendStringOption(command, "--copyid", copyId);

            return RunNodeJSProcess(command, false);
        }

        public override bool StopFileCopy(CloudFile file, string copyId, bool force = true)
        {
            AssertMandatoryParameter("--copy-id", copyId);

            string command = "file copy stop";
            command = appendStringOption(command, "--share", file.Share.Name);
            command = appendStringOption(command, "--path", CloudFileUtil.GetFullPath(file), true);
            command = appendStringOption(command, "--copyid", copyId);

            return RunNodeJSProcess(command, false);
        }

        public override void OutputValidation(Collection<Dictionary<string, object>> comp)
        {
            Test.Info("Validate Dictionary objects");
            Test.Assert(comp.Count == Output.Count, "Comparison size: {0} = {1} Output size", comp.Count, Output.Count);
            if (comp.Count != Output.Count)
            {
                return;
            }

            int count = 0;
            foreach (var dic in Output)
            {
                for (int i = 0; i < comp.Count; ++i)
                {
                    foreach (string str in comp[i].Keys)
                    {
                        switch (str)
                        {
                            case "CloudBlobContainer":
                                CompareEntity(dic, (CloudBlobContainer)comp[count]["CloudBlobContainer"]);
                                break;

                            case "CloudBlob":
                                CompareEntity(dic, (CloudBlob)comp[count]["CloudBlob"]);
                                break;

                            case "ShowContainer":
                                CompareEntity(dic, (CloudBlobContainer)comp[count]["ShowContainer"]);
                                {
                                    // construct a json format dictionary object
                                    var jsonDic = new Dictionary<string, object> { { "properties", JsonConvert.SerializeObject(dic) } };
                                    // compare fields in container properties
                                    CompareEntity(jsonDic, (CloudBlobContainer)comp[count]["ShowContainer"]);
                                }
                                break;

                            case "ShowBlob":
                                CompareEntity(dic, (CloudBlob)comp[count]["ShowBlob"]);
                                {
                                    // construct a json format dictionary object
                                    var jsonDic = new Dictionary<string, object> { { "properties", JsonConvert.SerializeObject(dic) } };
                                    // compare fields in container properties
                                    CompareEntity(jsonDic, (CloudBlob)comp[count]["ShowBlob"]);
                                }
                                break;
                            case "ApproximateMessageCount":
                                {
                                    var key = ((JObject)(dic["metadata"]))["approximateMessageCount"].ToString();
                                    int? message = comp[0]["ApproximateMessageCount"] as int?;
                                    int value;
                                    if (message != null && Int32.TryParse(key, out value))
                                    {
                                        Test.Assert(value == message, "Expect approximate message to be {0} and actually it's {1}.", message, value);
                                    }
                                }
                                break;
                        }
                    }
                }
                count++;
            }
        }

        public override void OutputValidation(IEnumerable<CloudBlobContainer> containers)
        {
            Test.Info("Validate CloudBlobContainer objects");
            Test.Assert(containers.Count() == Output.Count, "Comparison size: {0} = {1} Output size", containers.Count(), Output.Count);
            if (containers.Count() != Output.Count)
            {
                return;
            }

            int count = 0;
            foreach (CloudBlobContainer container in containers)
            {
                container.FetchAttributes();
                CompareEntity(Output[count], container);
                ++count;
            }
        }

        public override void OutputValidation(IEnumerable<BlobContainerPermissions> permissions)
        {
            Test.Info("Validate BlobContainerPermissions");
            Test.Assert(permissions.Count() == Output.Count, "Comparison size: {0} = {1} Output size", permissions.Count(), Output.Count);
            if (permissions.Count() != Output.Count)
                return;

            int count = 0;
            foreach (BlobContainerPermissions permission in permissions)
            {
                Test.Assert(permission.PublicAccess.ToString() == Output[count]["publicAccessLevel"].ToString(),
                    "container permision equality checking {0} = {1}", permission.PublicAccess.ToString(), Output[count]["publicAccessLevel"]);
                ++count;
            }
        }

        public override void OutputValidation(IEnumerable<CloudBlob> blobs)
        {
            Test.Info("Validate CloudBlob objects");
            Test.Assert(blobs.Count() == Output.Count, "Comparison size: {0} = {1} Output size", blobs.Count(), Output.Count);
            if (blobs.Count() != Output.Count)
            {
                return;
            }

            int count = 0;
            foreach (CloudBlob blob in blobs)
            {
                blob.FetchAttributes();
                CompareEntity(Output[count], blob);
                ++count;
            }
        }

        public override void OutputValidation(IEnumerable<CloudTable> tables)
        {
            Test.Info("Validate CloudTable objects");
            Test.Assert(tables.Count() == Output.Count, "Comparison size: {0} = {1} Output size", tables.Count(), Output.Count);
            if (tables.Count() != Output.Count)
                return;

            int count = 0;
            foreach (CloudTable table in tables)
            {
                //Test.Assert(Utility.CompareEntity(table, (CloudTable)Output[count]["CloudTable"]), "table equality checking: {0}", table.Name);
                ++count;
            }
        }

        public override void OutputValidation(IEnumerable<CloudQueue> queues)
        {
            Test.Info("Validate CloudQueue objects");
            Test.Assert(queues.Count() == Output.Count, "Comparison size: {0} = {1} Output size", queues.Count(), Output.Count);
            if (queues.Count() != Output.Count)
                return;

            int count = 0;
            foreach (CloudQueue queue in queues)
            {
                queue.FetchAttributes();
                //Test.Assert(Utility.CompareEntity(queue, (CloudQueue)Output[count]["CloudQueue"]), "queue equality checking: {0}", queue.Name);
                ++count;
            }
        }

        /// <summary>
        /// common batch operations
        /// </summary> 
        internal bool BatchOperation(string operation, string[] names, params object[] args)
        {
            bool success = true;

            // the following two objects would store output & error info for each operation
            Collection<Dictionary<string, object>> tmpOutput = new Collection<Dictionary<string, object>>();
            Collection<string> tmpErrMsgs = new Collection<string>();

            foreach (var name in names)
            {
                bool ret = false;
                switch (operation)
                {
                    case "NewAzureStorageContainer":
                        ret = NewAzureStorageContainer(name);
                        break;

                    case "RemoveAzureStorageContainer":
                        ret = RemoveAzureStorageContainer(name, force: (bool)args[0]);
                        break;

                    case "GetAzureStorageContainer":
                        ret = GetAzureStorageContainer(name);
                        break;

                    case "SetAzureStorageContainerACL":
                        ret = SetAzureStorageContainerACL(name, (BlobContainerPublicAccessType)args[0], passThru: (bool)args[1]);
                        break;

                    case "UploadLocalFiles":
                        ret = SetAzureStorageBlobContent(name, args[0].ToString(), (BlobType)args[1], args[2].ToString(), (bool)args[3], (int)args[4]);
                        break;

                    case "DownloadBlobFiles":
                        ret = GetAzureStorageBlobContent(name, args[0].ToString(), args[1].ToString(), (bool)args[2], (int)args[3]);
                        break;

                    case "RemoveAzureStorageBlob":
                        ret = RemoveAzureStorageBlob(name, args[0].ToString());
                        break;

                    default:
                        throw new Exception("operation not found in BatchOperation!");
                }

                if (!ret)
                {
                    success = false;
                }

                // store output & error info for each operation
                if (Output.Count > 0)
                {
                    tmpOutput.Add(Output[0]);
                }

                if (ErrorMessages.Count > 0)
                {
                    tmpErrMsgs.Add(ErrorMessages[0]);
                }
            }

            Output = tmpOutput;
            ErrorMessages = tmpErrMsgs;
            return success;
        }

        /// <summary>
        /// Compare two entities, usually one from NodeJS, one from XSCL
        /// Since XSCL object has more properties than NodeJS, we would only validate whether property in NodeJS would equal to the property in XSCL
        /// <param name="dic">from NodeJS</param>
        /// <param name="obj">from XSCL</param>
        /// </summary> 
        internal static void CompareEntity(Dictionary<string, object> dic, object obj)
        {
            //convert XSCL entity
            var xsclDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(obj, new StringEnumConverter()));
            xsclDic = ConvertXSCLEntities(xsclDic);

            if (obj is CloudBlob)
            {
                CloudBlob blob = (CloudBlob)obj;
                if (blob.SnapshotTime != null)
                {
                    string snapshotUri = BlobHttpWebRequestFactory.Get(blob.Uri, 0, blob.SnapshotTime.Value.UtcDateTime, AccessCondition.GenerateEmptyCondition(), new OperationContext()).Address.AbsoluteUri;
                    xsclDic["uri"] = snapshotUri;
                }
            }

            dic = ConvertNodeJSEntities(dic);

            foreach (var pair in dic)
            {
                if (pair.Key == "properties")
                {
                    //begin to compare properties
                    var propDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(pair.Value.ToString(), new StringEnumConverter());
                    var xsclPropDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(GetProperties(obj), new StringEnumConverter()));

                    propDic = ConvertNodeJSEntities(propDic);
                    xsclPropDic = ConvertXSCLEntities(xsclPropDic);

                    foreach (var propPair in propDic)
                    {
                        if (propPair.Key == "lastmodified")
                        {
                            //convert the string to datetime and then compare
                            DateTime t1 = DateTime.Parse(propPair.Value.ToString());
                            DateTime t2 = DateTime.Parse(xsclPropDic[propPair.Key].ToString());
                            Test.Assert(t1.Equals(t2), "{0} equality checking: {1} = {2}", propPair.Key, propPair.Value, xsclPropDic[propPair.Key]);
                        }
                        else
                        {
                            if (xsclPropDic.ContainsKey(propPair.Key))
                            {
                                string v = string.Empty;
                                if (xsclPropDic[propPair.Key] != null)
                                {
                                    v = xsclPropDic[propPair.Key].ToString();
                                }

                                Test.Assert(String.Compare(propPair.Value.ToString(), v, true) == 0,
                                    "{0} equality checking: {1} = {2}", propPair.Key, propPair.Value, xsclPropDic[propPair.Key]);
                            }
                            else
                            {
                                //Test.Assert(false, "key {0} not found in XSCL entity properties", propPair.Key);
                            }
                        }
                    }
                }
                else  // compare non-properties fields
                {
                    if (xsclDic.ContainsKey(pair.Key))
                    {
                        if (pair.Key == "container")
                        {
                            // skip container check as in nodejs it's container name but in xscl, it's container object, we already check it somewhere else
                        }
                        else
                        {
                            //compare other columns
                            Test.Assert(String.Compare(pair.Value.ToString(), xsclDic[pair.Key].ToString(), true) == 0,
                                "{0} equality checking: {1} = {2}", pair.Key, pair.Value, xsclDic[pair.Key]);
                        }
                    }
                    else
                    {
                        //Test.Assert(false, "key {0} not found in XSCL entity", pair.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Get blob or container object from XSCL object
        /// </summary>
        /// <param name="obj">XSCL object</param>
        /// <returns></returns>
        internal static object GetProperties(object obj)
        {
            if (obj is CloudBlob)
            {
                return ((CloudBlob)obj).Properties;
            }
            else if (obj.GetType() == typeof(CloudBlobContainer))
            {
                return ((CloudBlobContainer)obj).Properties;
            }
            else
            {
                throw new Exception("could not get properties from object : " + obj);
            }
        }

        /// <summary>
        /// convert a XSCL dictionary 
        /// </summary>
        /// <param name="dic">XSCL dictionary</param>
        /// <returns></returns>
        internal static Dictionary<string, object> ConvertXSCLEntities(Dictionary<string, object> dic)
        {
            Dictionary<string, object> lowerDic = new Dictionary<string, object>();
            foreach (var pair in dic)
            {
                lowerDic.Add(pair.Key.ToLower(), pair.Value);
            }
            return lowerDic;
        }

        internal static Dictionary<string, object> ConvertNodeJSEntities(Dictionary<string, object> dic)
        {
            Dictionary<string, string> map = new Dictionary<string, string>() { { "url", "uri" }, { "content-length", "length" } };

            Dictionary<string, object> retDic = new Dictionary<string, object>();
            foreach (var pair in dic)
            {
                //change key name according to map
                string key = pair.Key;
                foreach (var p in map)
                {
                    if (key.Equals(p.Key))
                    {
                        key = p.Value;
                        break;
                    }
                }

                //remove '-' in the key
                int index = key.IndexOf('-');
                if (index != -1)
                {
                    key = key.Remove(index, 1);
                }

                object value = pair.Value;
                if (pair.Key == "etag")
                {
                    // add " for comparison of etag value
                    if (!value.ToString().StartsWith("\""))
                    {
                        value = "\"" + value.ToString() + "\"";
                    }
                }

                retDic.Add(key.ToLower(), value);
            }
            return retDic;
        }

        /// <summary>
        /// Get OS config from testdata.xml
        /// </summary>
        public static void GetOSConfig(TestConfig data)
        {
            Utility.GetOSConfig(data, ref AgentOSType, AgentConfig);
        }

        public override bool HadErrors
        {
            get
            {
                return this.ErrorMessages.Count != 0;
            }
        }

        public override object CreateStorageContextObject(string connectionString)
        {
            CloudStorageAccount context = CloudStorageAccount.Parse(connectionString);
            return context;
        }

        public override void SetVariable(string variableName, object value)
        {
            throw new NotImplementedException();
        }

        public override string GetCurrentLocation()
        {
            throw new NotImplementedException();
        }

        public override void ChangeLocation(string path)
        {
            throw new NotImplementedException();
        }

        public override void NewFileShare(string fileShareName, object contextObject = null)
        {
            string command = "share create";
            command = appendStringOption(command, "", fileShareName);
            command = appendAccountOption(command, contextObject, false, isSource: true);

            this.RunNodeJSProcess(command, needAccountParam: contextObject == null);
        }

        public override void NewFileShareFromPipeline()
        {
            throw new NotImplementedException();
        }

        public override void NewDirectoryFromPipeline(string fileShareName)
        {
            throw new NotImplementedException();
        }

        public override void UploadFilesFromPipeline(string fileShareName, string localFileName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFileShareFromPipeline()
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectoriesFromPipeline(string fileShareName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFilesFromPipeline(string fileShareName)
        {
            throw new NotImplementedException();
        }

        public override void GetFileShareByName(string fileShareName, DateTimeOffset? snapshotTime = null)
        {
            this.RunNodeJSProcess(string.Format("share show \"{0}\"", fileShareName));
        }

        public override void GetFileShareByPrefix(string prefix)
        {
            this.RunNodeJSProcess(string.Format("share list \"{0}\"", prefix));
        }

        public override void RemoveFileShareByName(string fileShareName, bool passThru = false, object contextObject = null, bool confirm = false, bool includeAllSnapshot = false)
        {
            string command = "share delete";
            command = appendStringOption(command, "", fileShareName);
            command = appendAccountOption(command, contextObject, false, isSource: true);

            this.RunNodeJSProcess(command, !confirm, needAccountParam: contextObject == null);
        }

        public override void NewDirectory(CloudFileShare fileShare, string directoryName)
        {
            this.NewDirectory(fileShare.Name, directoryName);
        }

        public override void NewDirectory(CloudFileDirectory directory, string directoryName)
        {
            throw new NotImplementedException();
        }

        public override void NewDirectory(string fileShareName, string directoryName, object contextObject = null)
        {
            string command = "directory create";
            command = appendStringOption(command, "", fileShareName, quoted: true);
            command = appendStringOption(command, "", directoryName, quoted: true);
            command = appendAccountOption(command, contextObject, false, isSource: true);

            this.RunNodeJSProcess(command, needAccountParam: contextObject == null);
        }

        public override void RemoveDirectory(CloudFileShare fileShare, string directoryName, bool confirm = false)
        {
            this.RemoveDirectory(fileShare.Name, directoryName, confirm: confirm);
        }

        public override void RemoveDirectory(CloudFileDirectory directory, string path, bool confirm = false)
        {
            this.RemoveDirectory(directory.Share.Name, CloudFileUtil.GetFullPath(directory) + "/" + path, confirm: confirm);
        }

        public override void RemoveDirectory(string fileShareName, string directoryName, object contextObject = null, bool confirm = false)
        {
            string command = "directory delete";
            command = appendStringOption(command, "", fileShareName, quoted: true);
            command = appendStringOption(command, "", directoryName, quoted: true);
            command = appendAccountOption(command, contextObject, false, isSource: true);

            this.RunNodeJSProcess(command, !confirm, needAccountParam: contextObject == null);
        }

        public override void RemoveFile(CloudFileShare fileShare, string fileName, bool confirm = false)
        {
            this.RemoveFile(fileShare.Name, fileName, confirm: confirm);
        }

        public override void RemoveFile(CloudFileDirectory directory, string fileName, bool confirm = false)
        {
            this.RemoveFile(directory.Share.Name, CloudFileUtil.GetFullPath(directory) + "/" + fileName, confirm: confirm);
        }

        public override void RemoveFile(CloudFile file, bool confirm = false)
        {
            this.RemoveFile(file.Share.Name, CloudFileUtil.GetFullPath(file), confirm: confirm);
        }

        public override void RemoveFile(string fileShareName, string fileName, object contextObject = null, bool confirm = false)
        {
            string command = "file delete";
            command = appendStringOption(command, "", fileShareName, quoted: true);
            command = appendStringOption(command, "", fileName, quoted: true);
            command = appendAccountOption(command, contextObject, false, isSource: true);

            this.RunNodeJSProcess(command, !confirm, needAccountParam: contextObject == null);
        }

        public override void GetFile(string fileShareName, string path = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("file list \"{0}\"", fileShareName);
            if (path != null)
            {
                sb.AppendFormat(" \"{0}\"", path);
            }
            this.RunNodeJSProcess(sb.ToString());
        }

        public override void GetFile(CloudFileShare fileShare, string path = null)
        {
            this.GetFile(fileShare.Name, path);
        }

        public override void GetFile(CloudFileDirectory directory, string path = null)
        {
            this.GetFile(directory.Share.Name, directory.Name + '/' + path);
        }
        public override void GetFile()
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(CloudFile file, string destination, bool overwrite = false)
        {
            this.DownloadFile(file.Share.Name, CloudFileUtil.GetFullPath(file), destination, overwrite);
        }

        public override void DownloadFile(CloudFileDirectory directory, string path, string destination, bool overwrite = false)
        {
            this.DownloadFile(directory.GetFileReference(path), destination, overwrite);
        }

        public override void DownloadFile(CloudFileShare fileShare, string path, string destination, bool overwrite = false)
        {
            this.DownloadFile(fileShare.Name, path, destination, overwrite);
        }

        public override void DownloadFile(string fileShareName, string path, string destination, bool overwrite = false, object contextObject = null, bool CheckMd5 = false)
        {
            string dest = destination.TrimEnd(CloudFileUtil.PathSeparators);
            if (AgentOSType != OSType.Windows)
            {
                dest = FileUtil.GetLinuxPath(dest);
            }

            string command = "file download";
            command = appendStringOption(command, "", fileShareName, quoted: true);
            command = appendStringOption(command, "", path, quoted: true);
            command = appendStringOption(command, "", dest, quoted: true);
            command = appendAccountOption(command, contextObject, false, isSource: true);

            this.RunNodeJSProcess(command, overwrite, needAccountParam: contextObject == null);
        }

        public override void UploadFile(CloudFileShare fileShare, string source, string path, bool overwrite = false, bool passThru = false)
        {
            this.UploadFile(fileShare.Name, source, path, overwrite, passThru);
        }

        public override void UploadFile(CloudFileDirectory directory, string source, string path, bool overwrite = false, bool passThru = false)
        {
            throw new NotImplementedException();
        }

        public override void UploadFile(string fileShareName, string source, string path, bool overwrite = false, bool passThru = false, object contextObject = null)
        {
            if (AgentOSType != OSType.Windows)
            {
                source = FileUtil.GetLinuxPath(source);
            }

            string command = "file upload";
            command = appendStringOption(command, "", source, quoted: true);
            command = appendStringOption(command, "", fileShareName, quoted: true);
            command = appendStringOption(command, "", path, quoted: true);
            command = appendAccountOption(command, contextObject, false, isSource: true);

            this.RunNodeJSProcess(command, overwrite, needAccountParam: contextObject == null);
        }

        public override bool SetAzureStorageShareQuota(string shareName, int quota)
        {
            string command = string.Format("share set {0} --quota {1}", shareName, quota);

            return RunNodeJSProcess(command);
        }

        public override bool SetAzureStorageShareQuota(CloudFileShare share, int quota)
        {
            return SetAzureStorageShareQuota(share.Name, quota);
        }

        public override void AssertNoError()
        {
            Test.Assert(!this.HadErrors, "Should execute command without error.");
        }

        public override IExecutionResult Invoke(IEnumerable input = null, bool traceCommand = true)
        {
            if (traceCommand)
            {
                foreach (var output in this.Output)
                {
                    foreach (var item in output)
                    {
                        Test.Verbose("{0}: {1}", item.Key, item.Value);
                    }
                }
            }

            return new NodeJSExecutionResult(this.Output);
        }

        public override void AssertErrors(Action<IExecutionError> assertErrorAction, int expectedErrorCount = 1)
        {
            Test.Assert(this.ErrorMessages.Count == expectedErrorCount, "Expected {0} error records while there's {1}.", expectedErrorCount, this.ErrorMessages.Count);
            foreach (var errorRecord in this.ErrorMessages)
            {
                assertErrorAction(new NodeJSExecutionError(errorRecord));
            }
        }

        public override void Clear()
        {
            this.ErrorMessages.Clear();
            this.Output.Clear();
        }

        public override void UploadFilesInFolderFromPipeline(string fileShareName, string folder)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFiles(string fileShareName, string path, string destination, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public override bool NewFileShares(string[] names)
        {
            throw new NotImplementedException();
        }

        public override bool GetFileSharesByPrefix(string prefix)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveFileShares(string[] names)
        {
            throw new NotImplementedException();
        }

        public override bool NewDirectories(string fileShareName, string[] directoryNames)
        {
            throw new NotImplementedException();
        }

        public override bool ListDirectories(string fileShareName)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveDirectories(string fileShareName, string[] directoryNames)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(IEnumerable<CloudFileShare> shares)
        {
            throw new NotImplementedException();
        }

        public override void OutputValidation(IEnumerable<IListFileItem> items)
        {
            throw new NotImplementedException();
        }

        ///-------------------------------------
        /// Logging & Metrics & CORS APIs
        ///-------------------------------------
        public override bool GetAzureStorageServiceLogging(Constants.ServiceType serviceType)
        {
            string command = string.Format("logging show --{0}", serviceType.ToString().ToLower());

            return RunNodeJSProcess(command);
        }

        public override bool GetAzureStorageServiceMetrics(Constants.ServiceType serviceType, Constants.MetricsType metricsType)
        {
            string command = string.Format("metrics show --{0}", serviceType.ToString().ToLower());

            return RunNodeJSProcess(command);
        }

        public override bool SetAzureStorageServiceLogging(Constants.ServiceType serviceType, string loggingOperations = "", string loggingRetentionDays = "",
            string loggingVersion = "", bool passThru = false)
        {
            string command = "logging set";
            command = appendBoolOption(command, string.Format("--{0} ", serviceType.ToString().ToLower()));
            ;
            command += GetLoggingOptions(loggingOperations);

            if (!string.IsNullOrEmpty(loggingRetentionDays))
            {
                command = appendStringOption(command, "--retention", loggingRetentionDays);
            }

            if (!string.IsNullOrEmpty(loggingVersion))
            {
                command = appendStringOption(command, "--version", loggingVersion);
            }

            return RunNodeJSProcess(command);
        }

        public override bool SetAzureStorageServiceLogging(Constants.ServiceType serviceType, LoggingOperations[] loggingOperations, string loggingRetentionDays = "",
            string loggingVersion = "", bool passThru = false)
        {
            return this.SetAzureStorageServiceLogging(serviceType, GetLoggingOptions(loggingOperations), loggingRetentionDays, loggingVersion, passThru);
        }

        public override bool SetAzureStorageServiceMetrics(Constants.ServiceType serviceType, Constants.MetricsType metricsType, string metricsLevel = "", string metricsRetentionDays = "",
            string metricsVersion = "", bool passThru = false)
        {
            string command = "metrics set";
            command = appendBoolOption(command, string.Format("--{0}", serviceType.ToString().ToLower()));

            if (string.Compare(metricsLevel, "None", true) == 0)
            {
                if (metricsType == Constants.MetricsType.Hour)
                {
                    command = appendBoolOption(command, "--hour-off");
                }
                else if (metricsType == Constants.MetricsType.Minute)
                {
                    command = appendBoolOption(command, "--minute-off");
                }
            }
            else
            {
                if (metricsType == Constants.MetricsType.Hour)
                {
                    command = appendBoolOption(command, "--hour");
                    command += " --hour ";
                }
                else if (metricsType == Constants.MetricsType.Minute)
                {
                    command = appendBoolOption(command, "--minute");
                }

                if (string.Compare(metricsLevel, "ServiceAndApi", true) == 0)
                {
                    command = appendBoolOption(command, "--api");
                }
                else
                {
                    command = appendBoolOption(command, "--api-off");
                }
            }

            if (!string.IsNullOrEmpty(metricsRetentionDays))
            {
                int retention = 0;
                if (int.TryParse(metricsRetentionDays, out retention))
                {
                    if (retention > 0)
                    {
                        command = appendStringOption(command, "--retention", metricsRetentionDays);
                    }
                    else
                    {
                        command = appendStringOption(command, "--retention", "0");
                    }
                }
            }

            if (!string.IsNullOrEmpty(metricsVersion))
            {
                command = appendStringOption(command, "--version", metricsVersion);
            }

            return RunNodeJSProcess(command);
        }

        public override bool SetAzureStorageCORSRules(Constants.ServiceType serviceType, PSCorsRule[] corsRules)
        {
            string cors = StringifyCORS(corsRules);

            string command = "cors set";
            command = appendBoolOption(command, string.Format("--{0}", serviceType.ToString().ToLower()));
            command = appendStringOption(command, "--cors", cors, quoted: true);

            return RunNodeJSProcess(command);
        }

        public override bool GetAzureStorageCORSRules(Constants.ServiceType serviceType)
        {
            string command = "cors show";
            command = appendBoolOption(command, string.Format("--{0}", serviceType.ToString().ToLower()));

            return RunNodeJSProcess(command);
        }

        public override bool RemoveAzureStorageCORSRules(Constants.ServiceType serviceType)
        {
            string command = "cors delete";
            command = appendBoolOption(command, string.Format("--{0}", serviceType.ToString().ToLower()));

            return RunNodeJSProcess(command, true);
        }

        internal string StringifyCORS(PSCorsRule[] corsRules)
        {
            return JsonConvert.SerializeObject(corsRules);
        }

        internal string GetLoggingOptions(string loggingOperations)
        {
            string[] separator = { "," };
            string[] operations = loggingOperations.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            string options = string.Empty;

            List<LoggingOperations> ops = new List<LoggingOperations>();
            foreach (string operation in operations)
            {
                LoggingOperations op = LoggingOperations.None;
                if (Enum.TryParse<LoggingOperations>(loggingOperations, true, out op))
                {
                    ops.Add(op);
                }
                else
                {
                    options += string.Format(" {0} ", operation);
                }
            }

            options += GetLoggingOptions(ops.ToArray());
            return options;
        }

        internal string GetLoggingOptions(LoggingOperations[] loggingOperations)
        {
            bool read = false;
            bool write = false;
            bool delete = false;

            string loggingOption = string.Empty;
            foreach (LoggingOperations operation in loggingOperations)
            {
                if ((operation & LoggingOperations.Read) != 0)
                {
                    read = true;
                }

                if ((operation & LoggingOperations.Write) != 0)
                {
                    write = true;
                }

                if ((operation & LoggingOperations.Delete) != 0)
                {
                    delete = true;
                }
            }

            loggingOption += read ? " --read " : " --read-off ";
            loggingOption += write ? " --write " : " --write-off ";
            loggingOption += delete ? " --delete " : " --delete-off ";

            return loggingOption;
        }

        public override void OutputValidation(ServiceProperties serviceProperties, string propertiesType)
        {
            Test.Info("Validate ServiceProperties");
            Test.Assert(1 == Output.Count, "Output count should be {0} = 1", Output.Count);

            if (propertiesType.ToLower() == "logging")
            {
                LoggingOperations operations = serviceProperties.Logging.LoggingOperations;

                bool? read = Output[0]["Read"] as bool?;
                if ((operations & LoggingOperations.Read) != 0)
                {
                    Test.Assert((read.HasValue && read.Value),
                    string.Format("expected LoggingOperations '{0}' for reading, actually read is '{1}'", serviceProperties.Logging.LoggingOperations.ToString(),
                    read));
                }
                else
                {
                    Test.Assert((read.HasValue && !read.Value),
                    string.Format("expected LoggingOperations '{0}' for reading, actually read is '{1}'", serviceProperties.Logging.LoggingOperations.ToString(),
                    read));
                }

                bool? write = Output[0]["Write"] as bool?;
                if ((operations & LoggingOperations.Write) != 0)
                {
                    Test.Assert((write.HasValue && write.Value),
                    string.Format("expected LoggingOperations '{0}' for writing, actually write is '{1}'", serviceProperties.Logging.LoggingOperations.ToString(),
                    write));
                }
                else
                {
                    Test.Assert((write.HasValue && !write.Value),
                    string.Format("expected LoggingOperations '{0}' for writing, actually write is '{1}'", serviceProperties.Logging.LoggingOperations.ToString(),
                    write));
                }

                bool? delete = Output[0]["Delete"] as bool?;
                if ((operations & LoggingOperations.Delete) != 0)
                {
                    Test.Assert((delete.HasValue && delete.Value),
                    string.Format("expected LoggingOperations '{0}' for deleting, actually delete '{1}'", serviceProperties.Logging.LoggingOperations.ToString(),
                    delete));
                }
                else
                {
                    Test.Assert((delete.HasValue && !delete.Value),
                    string.Format("expected LoggingOperations '{0}' for deleting, actually delete is '{1}'", serviceProperties.Logging.LoggingOperations.ToString(),
                    delete));
                }

                dynamic retention = Output[0]["RetentionPolicy"];
                int? days = retention.Days;
                Test.Assert(serviceProperties.Logging.RetentionDays.Equals(days),
                    string.Format("expected RetentionDays {0}, actually it's {1}", serviceProperties.Logging.RetentionDays, days));

                Test.Assert(serviceProperties.Logging.Version.Equals(Output[0]["Version"]),
                    string.Format("expected Version {0}, actually it's {1}", serviceProperties.Logging.Version, Output[0]["Version"]));
            }
            else if (propertiesType.ToLower() == "hourmetrics")
            {
                dynamic metrics = Output[0]["HourMetrics"];
                int retention = metrics[0].RetentionPolicy.Days ?? 0;
                int expected = serviceProperties.HourMetrics.RetentionDays ?? 0;
                Test.Assert(expected == retention, string.Format("expected RetentionDays to be {0}, actually it's {1}", expected, retention));

                string version = metrics[0].Version.ToString();
                Test.Assert(serviceProperties.HourMetrics.Version.Equals(version),
                    string.Format("expected Version to be {0}, actually it's {1}", serviceProperties.HourMetrics.Version, version));
            }
            else if (propertiesType.ToLower() == "minutemetrics")
            {
                dynamic metrics = Output[0]["MinuteMetrics"];
                int retention = metrics[0].RetentionPolicy.Days ?? 0;
                int expected = serviceProperties.MinuteMetrics.RetentionDays ?? 0;

                Test.Assert(expected == retention, string.Format("expected RetentionDays to be {0}, actually it's {1}", expected, retention));

                string version = metrics[0].Version.ToString();
                Test.Assert(serviceProperties.MinuteMetrics.Version.Equals(version),
                    string.Format("expected Version to be {0}, actually it's {1}", serviceProperties.MinuteMetrics.Version, version));
            }
            else
            {
                throw new Exception("unknown properties type : " + propertiesType);
            }
        }

        ///-------------------------------------
        /// SAS token APIs
        ///-------------------------------------
        internal static object GetStorageContext(string ConnectionString)
        {
            return CloudStorageAccount.Parse(ConnectionString);
        }

        internal static void SetStorageContext(string ConnectionString)
        {
            Agent.Context = CloudStorageAccount.Parse(ConnectionString);
        }

        public override object GetStorageContextWithSASToken(CloudStorageAccount account, string sasToken, string endpointSuffix = null, bool useHttps = false)
        {
            string connectionString = string.Format("SharedAccessSignature={0};", sasToken);
            if (account.BlobEndpoint != null)
            {
                connectionString += string.Format("BlobEndpoint={0};", account.BlobEndpoint.AbsoluteUri);
            }

            if (account.TableEndpoint != null)
            {
                connectionString += string.Format("TableEndpoint={0};", account.TableEndpoint.AbsoluteUri);
            }

            if (account.QueueEndpoint != null)
            {
                connectionString += string.Format("QueueEndpoint={0};", account.QueueEndpoint.AbsoluteUri);
            }

            if (account.FileEndpoint != null)
            {
                connectionString += string.Format("FileEndpoint={0};", account.FileEndpoint.AbsoluteUri);
            }

            return CloudStorageAccount.Parse(connectionString);
        }

        public override void SetStorageContextWithSASToken(string StorageAccountName, string sasToken, bool useHttps = true)
        {
            this.SetStorageContextWithSASToken(StorageAccountName, sasToken, null, useHttps);
        }

        public override void SetStorageContextWithSASToken(string StorageAccountName, string sasToken, string endpoint, bool useHttps = true)
        {
            AgentConfig.AccountName = StorageAccountName;
            AgentConfig.SAS = sasToken;
        }

        public override void SetStorageContextWithSASTokenInConnectionString(CloudStorageAccount StorageAccount, string sasToken)
        {
            if (sasToken.StartsWith("?"))
            {
                sasToken = sasToken.Substring(1);
            }

            AgentConfig.ConnectionStr = string.Format("BlobEndpoint={0};FileEndpoint={1};TableEndpoint={2};QueueEndpoint={3};SharedAccessSignature={4}",
                StorageAccount.BlobEndpoint,
                StorageAccount.FileEndpoint,
                StorageAccount.TableEndpoint,
                StorageAccount.QueueEndpoint,
                sasToken);
        }

        public override string SetContextWithSASToken(string accountName, CloudBlobUtil blobUtil, StorageObjectType objectType,
            string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null)
        {
            return this.SetContextWithSASToken(accountName, blobUtil, objectType, null, policy, permission, startTime, expiryTime);
        }

        public override string SetContextWithSASToken(string accountName, CloudBlobUtil blobUtil, StorageObjectType objectType,
            string endpoint, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null)
        {
            string sastoken = string.Empty;
            switch (objectType)
            {
                case StorageObjectType.Blob:
                    sastoken = GetBlobSasFromCmd(blobUtil.Blob, policy, permission);
                    break;
                case StorageObjectType.Container:
                    sastoken = GetContainerSasFromCmd(blobUtil.ContainerName, policy, permission);
                    break;
                default:
                    throw new Exception("unsupported object type : " + objectType.ToString());
            }
            AgentConfig.AccountName = accountName;
            AgentConfig.SAS = sastoken;

            return sastoken;
        }

        public override bool NewAzureStorageContainerSAS(string container, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            string command = string.Format("container sas create {0}", container);

            command = GetGeneralSASCmd(command, permission, startTime, expiryTime, policy, protocol, iPAddressOrRange);

            return RunNodeJSProcess(command);
        }

        public override bool NewAzureStorageBlobSAS(string container, string blob, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            string command = string.Format("blob sas create {0} \"{1}\"", container, blob);

            command = GetGeneralSASCmd(command, permission, startTime, expiryTime, policy, protocol, iPAddressOrRange);

            return RunNodeJSProcess(command);
        }

        public override bool NewAzureStorageShareSAS(string shareName, string policyName, string permissions = null,
           DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            string command = string.Format("share sas create {0}", shareName);

            command = GetGeneralSASCmd(command, permissions, startTime, expiryTime, policyName, protocol, iPAddressOrRange);

            return RunNodeJSProcess(command);
        }

        public override bool NewAzureStorageFileSAS(string shareName, string filePath, string policyName = null, string permissions = null,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            string command = string.Format("file sas create {0} {1}", shareName, filePath);

            command = GetGeneralSASCmd(command, permissions, startTime, expiryTime, policyName, protocol, iPAddressOrRange);

            return RunNodeJSProcess(command);
        }

        public override bool NewAzureStorageFileSAS(CloudFile file, string policyName = null, string permissions = null,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return NewAzureStorageFileSAS(file.Share.Name, CloudFileUtil.GetFullPath(file), policyName, permissions, startTime, expiryTime, fulluri);
        }

        public override bool NewAzureStorageTableSAS(string name, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, string startpk = "", string startrk = "", string endpk = "", string endrk = "", SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            string command = string.Format("table sas create {0}", name);

            command = GetGeneralSASCmd(command, permission, startTime, expiryTime, policy, protocol, iPAddressOrRange);

            if (!string.IsNullOrEmpty(startpk))
            {
                if (!string.IsNullOrEmpty(startrk))
                {
                    command += " --start-rk " + startrk;
                }
                command += " --start-pk " + startpk;
            }

            if (!string.IsNullOrEmpty(endpk))
            {
                if (!string.IsNullOrEmpty(endrk))
                {
                    command += " --end-rk " + endrk;
                }
                command += " --end-pk " + endpk;
            }

            return RunNodeJSProcess(command);
        }

        public override bool NewAzureStorageQueueSAS(string name, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            string command = string.Format("queue sas create {0}", name);

            command = GetGeneralSASCmd(command, permission, startTime, expiryTime, policy, protocol, iPAddressOrRange);

            return RunNodeJSProcess(command);
        }

        public override bool NewAzureStorageAccountSAS(SharedAccessAccountServices service, SharedAccessAccountResourceTypes resourceType, string permission, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            string command = string.Format("account sas create");

            command = appendStringOption(command, "--services", SharedAccessAccountPolicy.ServicesToString(service));
            command = appendStringOption(command, "--resource-types ", SharedAccessAccountPolicy.ResourceTypesToString(resourceType));
            command = appendStringOption(command, "--permissions", permission);

            command = appendDateTimeOption(command, "--start", startTime);
            if (expiryTime.HasValue)
            {
                command = appendDateTimeOption(command, "--expiry", expiryTime);
            }
            else
            {
                // Default value to 1 hour later from now.
                // Normally we should not add this kind of logic in the agent like "default value" to make sure we can cover the negative case.
                // Why we do this is because PowerShellAgent has this logic and the existing positive test cases won't pass the expiry parameter explicitly and rely on the default value logic. 
                // Besides that, the negative case can only be covered manually because if we don't pass the expiry parameter, the command will hang there - waiting for input as the xPlat doesn't support pipeline now.
                command = appendDateTimeOption(command, "--expiry", DateTime.UtcNow.AddHours(1));
            }

            if (protocol.HasValue)
            {
                command = appendStringOption(command, "--protocol", protocol.Value.ToString());
            }
            if (!string.IsNullOrEmpty(iPAddressOrRange))
            {
                command = appendStringOption(command, "--ip-range", iPAddressOrRange);
            }

            return RunNodeJSProcess(command);
        }

        public override string GetBlobSasFromCmd(string containerName, string blobName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageBlobSAS(containerName, blobName, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange),
                    "Generate blob sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKeyNode].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                if (fulluri)
                {
                    return Output[0][Constants.SASTokenURLNode].ToString();
                }
                else
                {
                    return sasToken;
                }
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        public override string GetBlobSasFromCmd(CloudBlob blob, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return GetBlobSasFromCmd(blob.Container.Name, blob.Name, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange);
        }

        public override string GetContainerSasFromCmd(string containerName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageContainerSAS(containerName, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange),
                    "Generate container sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKeyNode].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        public override string GetAzureStorageShareSasFromCmd(string shareName, string policy, string permission = null,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            NewAzureStorageShareSAS(shareName, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange);
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKeyNode].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                Test.Info("Fail to generate sas token.");
                return string.Empty;
            }
        }

        public override string GetAzureStorageFileSasFromCmd(string shareName, string filePath, string policy, string permission = null,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            NewAzureStorageFileSAS(shareName, filePath, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange);
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKeyNode].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                if (fulluri)
                {
                    return Output[0][Constants.SASTokenURLNode].ToString();
                }
                else
                {
                    return sasToken;
                }
            }
            else
            {
                Test.Info("Fail to generate sas token.");
                return string.Empty;
            }
        }

        public override string GetQueueSasFromCmd(string queueName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageQueueSAS(queueName, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange),
                    "Generate queue sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKeyNode].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        public override string GetTableSasFromCmd(string tableName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false,
            string startpk = "", string startrk = "", string endpk = "", string endrk = "", SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageTableSAS(tableName, policy, permission, startTime, expiryTime, fulluri,
                startpk, startrk, endpk, endrk, protocol, iPAddressOrRange),
                    "Generate table sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKeyNode].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        public override string GetAccountSasFromCmd(SharedAccessAccountServices service, SharedAccessAccountResourceTypes resourceType, string permission, SharedAccessProtocol? protocol, string iPAddressOrRange,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            Test.Assert(NewAzureStorageAccountSAS(service, resourceType, permission, protocol, iPAddressOrRange, startTime, expiryTime),
                    "Generate account sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKey].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        internal string GetGeneralSASCmd(string command, string permission, DateTime? startTime, DateTime? expiryTime, string policy, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            if (string.IsNullOrEmpty(policy))
            {
                if (string.IsNullOrEmpty(permission))
                {
                    permission = "r";
                }

                command = appendStringOption(command, "--permissions", permission);

                if (expiryTime.HasValue)
                {
                    command = appendDateTimeOption(command, "--expiry", expiryTime);
                }
                else
                {
                    command = appendDateTimeOption(command, "--expiry", DateTime.UtcNow.AddHours(1));
                }
            }
            else
            {
                command = appendStringOption(command, "--policy", policy, quoted: true);

                command = appendStringOption(command, "--permissions", permission);
                command = appendDateTimeOption(command, "--expiry", expiryTime);
            }

            command = appendDateTimeOption(command, "--start", startTime);
            if (protocol.HasValue)
            {
                command = appendStringOption(command, "--protocol", protocol.Value.ToString());
            }
            if (!string.IsNullOrEmpty(iPAddressOrRange))
            {
                command = appendStringOption(command, "--ip-range", iPAddressOrRange);
            }

            return command;
        }

        ///-------------------------------------
        /// Stored Access Policy APIs
        ///-------------------------------------
        public override bool GetAzureStorageTableStoredAccessPolicy(string tableName, string policyName)
        {
            return GetAzureStorageStoredAccessPolicy("table", tableName, policyName);
        }

        public override bool NewAzureStorageTableStoredAccessPolicy(string tableName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            return NewAzureStorageStoredAccessPolicy("table", tableName, policyName, permission, startTime, expiryTime);
        }

        public override bool RemoveAzureStorageTableStoredAccessPolicy(string tableName, string policyName, bool Force = true)
        {
            return RemoveAzureStorageStoredAccessPolicy("table", tableName, policyName, Force);
        }

        public override bool SetAzureStorageTableStoredAccessPolicy(string tableName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            return SetAzureStorageStoredAccessPolicy("table", tableName, policyName, permission, startTime, expiryTime, NoStartTime, NoExpiryTime);
        }

        public override bool GetAzureStorageQueueStoredAccessPolicy(string queueName, string policyName)
        {
            return GetAzureStorageStoredAccessPolicy("queue", queueName, policyName);
        }

        public override bool NewAzureStorageQueueStoredAccessPolicy(string queueName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            return NewAzureStorageStoredAccessPolicy("queue", queueName, policyName, permission, startTime, expiryTime);
        }

        public override bool RemoveAzureStorageQueueStoredAccessPolicy(string queueName, string policyName, bool Force = true)
        {
            return RemoveAzureStorageStoredAccessPolicy("queue", queueName, policyName, Force);
        }

        public override bool SetAzureStorageQueueStoredAccessPolicy(string queueName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            return SetAzureStorageStoredAccessPolicy("queue", queueName, policyName, permission, startTime, expiryTime, NoStartTime, NoExpiryTime);
        }

        public override bool GetAzureStorageContainerStoredAccessPolicy(string containerName, string policyName)
        {
            return GetAzureStorageStoredAccessPolicy("container", containerName, policyName);
        }

        public override bool NewAzureStorageContainerStoredAccessPolicy(string containerName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            return NewAzureStorageStoredAccessPolicy("container", containerName, policyName, permission, startTime, expiryTime);
        }

        public override bool RemoveAzureStorageContainerStoredAccessPolicy(string containerName, string policyName, bool Force = true)
        {
            return RemoveAzureStorageStoredAccessPolicy("container", containerName, policyName, Force);
        }

        public override bool SetAzureStorageContainerStoredAccessPolicy(string containerName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            return SetAzureStorageStoredAccessPolicy("container", containerName, policyName, permission, startTime, expiryTime, NoStartTime, NoExpiryTime);
        }

        public override bool NewAzureStorageShareStoredAccessPolicy(string shareName, string policyName, string permissions, DateTime? startTime, DateTime? expiryTime)
        {
            return NewAzureStorageStoredAccessPolicy("share", shareName, policyName, permissions, startTime, expiryTime);
        }

        public override bool GetAzureStorageShareStoredAccessPolicy(string shareName, string policyName)
        {
            return GetAzureStorageStoredAccessPolicy("share", shareName, policyName);
        }

        public override bool RemoveAzureStorageShareStoredAccessPolicy(string shareName, string policyName, bool confirm = false)
        {
            return RemoveAzureStorageStoredAccessPolicy("share", shareName, policyName, !confirm);
        }

        public override bool SetAzureStorageShareStoredAccessPolicy(string shareName, string policyName, string permissions,
            DateTime? startTime, DateTime? expiryTime, bool noStartTime = false, bool noExpiryTime = false)
        {
            return SetAzureStorageStoredAccessPolicy("share", shareName, policyName, permissions, startTime, expiryTime, noStartTime, noExpiryTime);
        }

        internal bool GetAzureStorageStoredAccessPolicy(string resourceType, string resourceName, string policyName)
        {
            string command = string.Format("{0} policy show \"{1}\" \"{2}\"", resourceType, resourceName, policyName);

            return RunNodeJSProcess(command);
        }

        internal bool NewAzureStorageStoredAccessPolicy(string resourceType, string resourceName, string policyName, string permission,
             DateTime? startTime = null, DateTime? expiryTime = null)
        {
            string command = string.Format("{0} policy create \"{1}\" \"{2}\"", resourceType, resourceName, policyName);

            if (!string.IsNullOrEmpty(permission))
            {
                command += " --permissions " + permission;
            }

            if (startTime.HasValue)
            {
                command += " --start " + startTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            if (expiryTime.HasValue)
            {
                command += " --expiry " + expiryTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return RunNodeJSProcess(command);
        }

        internal bool RemoveAzureStorageStoredAccessPolicy(string resourceType, string resourceName, string policyName, bool Force = true)
        {
            string command = string.Format("{0} policy delete \"{1}\" \"{2}\"", resourceType, resourceName, policyName);

            return RunNodeJSProcess(command);
        }

        internal bool SetAzureStorageStoredAccessPolicy(string resourceType, string resourceName, string policyName, string permission,
             DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            string command = string.Format("{0} policy set \"{1}\" \"{2}\"", resourceType, resourceName, policyName);

            if (!string.IsNullOrEmpty(permission))
            {
                command += " --permissions " + permission;
            }
            else if (permission == string.Empty)
            {
                command += " --permissions " + DOUBLE_SPACE;
            }

            if (NoStartTime)
            {
                command += " --start " + DOUBLE_SPACE;
            }
            else if (startTime.HasValue)
            {
                command += " --start " + startTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            if (NoExpiryTime)
            {
                command += " --expiry " + DOUBLE_SPACE;
            }
            else if (expiryTime.HasValue)
            {
                command += " --expiry " + expiryTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return RunNodeJSProcess(command);
        }


        public override bool StartAzureStorageBlobIncrementalCopy(string sourceUri, string destContainerName, string destBlobName, object destContext = null)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateSRPAzureStorageAccountNetworkAcl(string resourceGroupName, string accountName, PSNetWorkRuleBypassEnum? bypass = default(PSNetWorkRuleBypassEnum?), PSNetWorkRuleDefaultActionEnum? defaultAction = default(PSNetWorkRuleDefaultActionEnum?), PSIpRule[] ipRules = null, PSVirtualNetworkRule[] networkRules = null)
        {
            throw new NotImplementedException();
        }
        public override bool StartAzureStorageBlobIncrementalCopy(string srcContainerName, string srcBlobName, DateTimeOffset? SnapshotTime, string destContainerName, string destBlobName, object destContext = null)
        {
            throw new NotImplementedException();
        }

        public override bool GetSRPAzureStorageAccountNetworkAcl(string resourceGroupName, string accountName)
        {
            throw new NotImplementedException();
        }
        public override bool StartAzureStorageBlobIncrementalCopy(CloudBlobContainer srcContainer, string srcBlobName, DateTimeOffset? SnapshotTime, string destContainerName, string destBlobName, object destContext = null)
        {
            throw new NotImplementedException();
        }

        public override bool AddSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, string[] ruleId, bool isIPRule = true)
        {
            throw new NotImplementedException();
        }

        public override bool StartAzureStorageBlobIncrementalCopy(CloudPageBlob srcBlob, string destContainerName, string destBlobName, object destContext = null)
        {
            throw new NotImplementedException();
        }

        public override bool AddSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, PSIpRule[] iprule)
        {
            throw new NotImplementedException();
        }

        public override bool StartAzureStorageBlobIncrementalCopy(CloudPageBlob srcBlob, CloudPageBlob destBlob, object destContext = null)
        {
            throw new NotImplementedException();
        }

        public override bool AddSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, PSVirtualNetworkRule[] networkRule)
        {
            throw new NotImplementedException();
        }

        public override bool SetSRPAzureStorageAccountKeyVault(string resourceGroupName, string accountName, string skuName = null, Hashtable[] tags = null, Constants.EncryptionSupportServiceEnum? enableEncryptionService = default(Constants.EncryptionSupportServiceEnum?), Constants.EncryptionSupportServiceEnum? disableEncryptionService = default(Constants.EncryptionSupportServiceEnum?), AccessTier? accessTier = default(AccessTier?), string customDomain = null, bool? useSubdomain = default(bool?), bool? enableHttpsTrafficOnly = default(bool?), bool AssignIdentity = false, bool keyvaultEncryption = false, string keyName = null, string keyVersion = null, string keyVaultUri = null, PSNetworkRuleSet networkAcl = null)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, string[] ruleId, bool isIPRule = true)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, PSIpRule[] iprule)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, PSVirtualNetworkRule[] networkRule)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureStorageServiceProperties(Constants.ServiceType serviceType)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateAzureStorageServiceProperties(Constants.ServiceType serviceType, string DefaultServiceVersion)
        {
            throw new NotImplementedException();
        }

        public override bool DisableAzureStorageDeleteRetentionPolicy(bool PassThru = false)
        {
            throw new NotImplementedException();
        }

        public override bool EnableAzureStorageDeleteRetentionPolicy(int RetentionDays, bool PassThru = false)
        {
            throw new NotImplementedException();
        }

        public override bool NewAzureRmStorageContainer(string resourceGroupName, string accountName, string Name, Hashtable Metadata = null, PSPublicAccess? PublicAccess = null)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateAzureRmStorageContainer(string resourceGroupName, string accountName, string Name, Hashtable Metadata = null, PSPublicAccess? PublicAccess = null)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureRmStorageContainer(string resourceGroupName, string accountName, string Name = null)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureRmStorageContainer(string resourceGroupName, string accountName, string Name)
        {
            throw new NotImplementedException();
        }

        public override bool AddAzureRmStorageContainerLegalHold(string resourceGroupName, string accountName, string Name, string[] tag)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureRmStorageContainerLegalHold(string resourceGroupName, string accountName, string Name, string[] tag)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureRmStorageContainerImmutabilityPolicy(string resourceGroupName, string accountName, string containerName)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureRmStorageContainerImmutabilityPolicy(string resourceGroupName, string accountName, string containerName, int immutabilityPeriod, bool extendPolicy = false, string Etag = null)
        {
            throw new NotImplementedException();
        }

        public override bool LockAzureRmStorageContainerImmutabilityPolicy(string resourceGroupName, string accountName, string containerName, string Etag)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureRmStorageContainerImmutabilityPolicy(string resourceGroupName, string accountName, string containerName, string Etag)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureRmStorageAccountManagementPolicy(string resourceGroupName, string accountName, string policy)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureRmStorageAccountManagementPolicy(PSStorageAccount accountObject, string policy)
        {
            throw new NotImplementedException();
        }

        public override bool SetAzureRmStorageAccountManagementPolicy(string resourceGroupName, string accountName, PSManagementPolicy policyObject)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureRmStorageAccountManagementPolicy(string resourceGroupName, string accountName)
        {
            throw new NotImplementedException();
        }

        public override bool GetAzureRmStorageAccountManagementPolicy(PSStorageAccount accountObject)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureRmStorageAccountManagementPolicy(string resourceGroupName, string accountName)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureRmStorageAccountManagementPolicy(PSStorageAccount accountObject)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveAzureRmStorageAccountManagementPolicy(PSManagementPolicy policyObject)
        {
            throw new NotImplementedException();
        }

        public override bool DisableAzureStorageStaticWebsite(bool PassThru = false)
        {
            throw new NotImplementedException();
        }

        public override bool EnableAzureStorageStaticWebsite(string indexDocument, string errorDocument404Path, bool PassThru = false)
        {
            throw new NotImplementedException();
        }

        public override bool InvokeAzureRmStorageAccountFailover(string resourceGroup, string accountName)
        {
            throw new NotImplementedException();
        }
    }
}
