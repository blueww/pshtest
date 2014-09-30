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

namespace Management.Storage.ScenarioTest
{
    /// <summary>
    /// This class is used to create an agent which could run Node.js xplat commands
    /// </summary>
    public class NodeJSAgent : Agent
    {
        private const string NotImplemented = "Not implemented in NodeJS Agent!";
        private const string ExportPathCommand = "export PATH=$PATH:/usr/local/bin/;";

        private static int DefaultMaxWaitingTime = 30000;  // in miliseconds

        private static Hashtable ExpectedErrorMsgTableNodeJS = new Hashtable() {
                {"GetBlobContentWithNotExistsBlob", "Can not find blob '{0}' in container '{1}'"},
                {"GetBlobContentWithNotExistsContainer", "Can not find blob '{0}' in container '{1}'"},
                {"RemoveBlobWithLease", "There is currently a lease on the blob and no lease ID was specified in the request"},
                {"SetBlobContentWithInvalidBlobType", "The blob type is invalid for this operation"},
                {"SetPageBlobWithInvalidFileSize", "Page blob length must be multiple of 512"}, 
                {"CreateExistingContainer", "Container '{0}' already exists"},
                {"CreateInvalidContainer", "Container name format is incorrect"},
                {"RemoveNonExistingContainer", "The specified container does not exist"},
                {"RemoveNonExistingBlob", "The specified blob does not exist."},
                {"SetBlobContentWithInvalidBlobName", "One of the request inputs is out of range"},
                {"SetContainerAclWithInvalidName", "Container name format is incorrect"},
                {"ShowNonExistingBlob", "Blob {0} in Container {1} doesn't exist"},
                {"ShowNonExistingContainer", "Container {0} doesn't exist"},
                {"UseInvalidAccount", "getaddrinfo"}, //bug#892297
                {"MissingAccountName", "Please set the storage account parameters or one of the following two environment variables to use"},
                {"MissingAccountKey", "Please set the storage account parameters or one of the following two environment variables to use"},
                {"OveruseAccountParams", "Please only define one of them: 1. --connection-string. 2 --account-name and --account-key"},
                {"CreateExistingTable", "The table specified already exists"},
                {"CreateInvalidTable", "Table name format is incorrect"},
                {"GetNonExistingTable", "Table {0} doesn't exist"},
                {"RemoveNonExistingTable", "Can not find table '{0}'"},
                {"CreateExistingQueue", "The queue specified already exists"},
                {"CreateInvalidQueue", "Queue name format is incorrect"},
                {"GetNonExistingQueue", "Queue {0} doesn't exist"},
                {"RemoveNonExistingQueue", "The specified queue does not exist"},
        };

        public static string BinaryFileName { get; set; }
        public static int MaxWaitingTime { get; set; }
        public static string WorkingDirectory { get; set; }

        public static OSType AgentOSType = OSType.Windows;
        public static OSConfig AgentConfig = new OSConfig();

        public NodeJSAgent()
        {
            MaxWaitingTime = DefaultMaxWaitingTime;
            WorkingDirectory = ".";

            //assign the error message table for error validation
            ExpectedErrorMsgTable = ExpectedErrorMsgTableNodeJS;
        }

        internal static void SetProcessInfo(Process p, string category, string argument)
        {
            p.StartInfo.UseShellExecute = false;
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
                    p.StartInfo.Arguments += ExportPathCommand;
                }

                // replace all " with ' in argument for linux
                argument = argument.Replace('"', '\'');
            }
            p.StartInfo.Arguments += string.Format(" azure {0} {1} --json", category, argument);

            Test.Info("NodeJS command: {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);
        }

        internal string AddAccountParameters(string argument)
        {
            string ret = argument;
            if (!string.IsNullOrEmpty(AgentConfig.ConnectionString))
            {
                ret += string.Format(" -c \"{0}\"", AgentConfig.ConnectionString);
            }

            if (!string.IsNullOrEmpty(AgentConfig.AccountName))
            {
                ret += string.Format(" -a \"{0}\" -k \"{1}\"", AgentConfig.AccountName, AgentConfig.AccountKey);
            }

            // if no account param set, then we would use the env var
            return ret;
        }

        internal void ImportAzureSubscription()
        {
            string settingFile = Test.Data.Get("AzureSubscriptionPath");
            RunNodeJSProcess(string.Format("import \"{0}\"", settingFile), needAccountParam: false, category: "account");
        }               

        internal void SetActiveSubscription(string nameOrID)
        {
            RunNodeJSProcess(string.Format("set \"{0}\"", nameOrID), needAccountParam: false, category: "account");
        }

        internal bool RunNodeJSProcess(string argument, bool force = false, bool needAccountParam = true, string category = "storage")
        {
            if (force)
            {
                argument += " --quiet";
            }

            if (!AgentConfig.UseEnvVar && needAccountParam)
            {
                argument = AddAccountParameters(argument);
            }

            Process p = new Process();
            SetProcessInfo(p, category, argument);
            StringBuilder outputBuilder = new StringBuilder();
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

            string output = outputBuffer.ToString();
            string error = errorBuffer.ToString();

            if (!string.IsNullOrEmpty(error))
            {
                Test.Verbose("Error:\n{0}", error);
            }

            if (!p.HasExited)
            {
                p.Kill();
                throw new Exception(string.Format("NodeJS command timeout: cost time > {0}s !", MaxWaitingTime / 1000));
            }

            ErrorMessages.Clear();
            Output.Clear();

            bool bSuccess = string.IsNullOrEmpty(error);

            if (bSuccess)
            {
                // parse output data
                // the output may have warning message, it should keep the json object only.
                // WARNING: warning message
                // [{..........}]
                output = output.Trim();
                if (output.Length >= 2)
                {
                    int startIndex = output.IndexOf('[');
                    if (startIndex == -1 || (output[0] == '{' && output[output.Length - 1] == '}'))
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
                }

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
                Test.Info(error);
            }

            return bSuccess;
        }

        public override bool ShowAzureStorageAccountConnectionString(string argument)
        {
            return RunNodeJSProcess(string.Format("account connectionstring show {0}", argument), needAccountParam: false);
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
        public bool ShowAzureStorageContainer(string containerName)
        {
            return RunNodeJSProcess(string.Format("container show \"{0}\"", containerName));
        }

        public override bool GetAzureStorageContainerByPrefix(string prefix)
        {
            return RunNodeJSProcess("container list " + prefix);
        }

        public override bool SetAzureStorageContainerACL(string containerName, BlobContainerPublicAccessType publicAccess, bool passThru = true)
        {
            return RunNodeJSProcess(string.Format("container set \"{0}\" --permission {1}", containerName, publicAccess));
        }

        public override bool SetAzureStorageContainerACL(string[] containerNames, BlobContainerPublicAccessType publicAccess, bool passThru = true)
        {
            return BatchOperation(MethodBase.GetCurrentMethod().Name, containerNames, publicAccess, passThru);
        }

        public override bool RemoveAzureStorageContainer(string containerName, bool force = true)
        {
            return RunNodeJSProcess(string.Format("container delete \"{0}\"", containerName), force);
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
            bool force = true, int concurrentCount = -1, Hashtable properties = null, Hashtable metadata = null)
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
            bool force = true, int concurrentCount = -1)
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

        public override bool GetAzureStorageBlob(string blobName, string containerName)
        {
            return RunNodeJSProcess(string.Format("blob list \"{0}\" \"{1}\"", containerName, blobName));
        }

        // this command is nodejs specific
        public bool ShowAzureStorageBlob(string blobName, string containerName)
        {
            return RunNodeJSProcess(string.Format("blob show \"{0}\" \"{1}\"", containerName, blobName));
        }

        public override bool GetAzureStorageBlobByPrefix(string prefix, string containerName)
        {
            return RunNodeJSProcess(string.Format("blob list \"{0}\" {1}", containerName, prefix));
        }

        public override bool RemoveAzureStorageBlob(string blobName, string containerName, bool onlySnapshot = false, bool force = true)
        {
            // need to add onlySnapshot to the parameter list later (in V1, we won't support this functionality)
            return RunNodeJSProcess(string.Format("blob delete \"{0}\" \"{1}\"", containerName, blobName), force);
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
            string argument = string.Empty;
            argument = string.Format("blob copy start \"{0}\" \"{1}\"", sourceUri, destContainerName);

            if (!string.IsNullOrWhiteSpace(destBlobName))
            {
                argument = string.Format(argument + " --dest-blob \"{0}\"", destBlobName);
            }

            return RunNodeJSProcess(argument, force);
        }

        public override bool StartAzureStorageBlobCopy(string srcContainerName, string srcBlobName, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            string argument = string.Format("blob copy start --source-container \"{0}\" --source-blob \"{1}\" --dest-container \"{2}\"", srcContainerName, srcBlobName, destContainerName);

            if (!string.IsNullOrWhiteSpace(destBlobName))
            {
                argument = string.Format(argument + " --dest-blob \"{0}\"", destBlobName);
            }

            CloudStorageAccount account = destContext as CloudStorageAccount;
            if (account != null)
            {
                argument += (" --dest-account-name " + account.Credentials.AccountName + " --dest-account-key " + account.Credentials.ExportBase64EncodedKey());
            }

            return RunNodeJSProcess(argument, force);
        }

        public override bool StartAzureStorageBlobCopy(ICloudBlob srcBlob, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            string argument = string.Format(string.Format("blob copy start \"{0}\" \"{1}\"", srcBlob.SnapshotQualifiedUri.AbsoluteUri, destContainerName));
            if (!string.IsNullOrWhiteSpace(destBlobName))
            {
                argument = string.Format(argument + " --dest-blob \"{0}\"", destBlobName);
            }

            CloudStorageAccount account = destContext as CloudStorageAccount;
            if (account != null)
            {
                argument = string.Format(argument + " --dest-account-name {0} --dest-account-key \"{1}\"", account.Credentials.AccountName, account.Credentials.ExportBase64EncodedKey());
            }

            return RunNodeJSProcess(argument, force);
        }

        public override bool GetAzureStorageBlobCopyState(string containerName, string blobName, bool waitForComplete)
        {
            return RunNodeJSProcess(string.Format("blob copy show \"{0}\" \"{1}\"", containerName, blobName));
        }

        public override bool GetAzureStorageBlobCopyState(ICloudBlob blob, object context, bool waitForComplete)
        {
            string argument = string.Format("blob copy show \"{0}\" \"{1}\"", blob.Container.Name, blob.Name);

            bool needAccountParam = true;
            CloudStorageAccount account = context as CloudStorageAccount;
            if (account != null)
            {
                needAccountParam = false;
                argument = string.Format(argument + " --account-name {0} --account-key \"{1}\"", account.Credentials.AccountName, account.Credentials.ExportBase64EncodedKey());
            }

            return RunNodeJSProcess(argument, needAccountParam: needAccountParam);
        }

        public override bool StopAzureStorageBlobCopy(string containerName, string blobName, string copyId, bool force)
        {
            return RunNodeJSProcess(string.Format("blob copy stop \"{0}\" \"{1}\" \"{2}\"", containerName, blobName, copyId));
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

                            case "ICloudBlob":
                                CompareEntity(dic, (ICloudBlob)comp[count]["ICloudBlob"]);
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
                                CompareEntity(dic, (ICloudBlob)comp[count]["ShowBlob"]);
                                {
                                    // construct a json format dictionary object
                                    var jsonDic = new Dictionary<string, object> { { "properties", JsonConvert.SerializeObject(dic) } };
                                    // compare fields in container properties
                                    CompareEntity(jsonDic, (ICloudBlob)comp[count]["ShowBlob"]);
                                }
                                break;
                            case "ApproximateMessageCount":
                                {
                                    var key = ((JObject)(dic["metadata"]))["approximatemessagecount"].ToString();
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

        public override void OutputValidation(IEnumerable<ICloudBlob> blobs)
        {
            Test.Info("Validate ICloudBlob objects");
            Test.Assert(blobs.Count() == Output.Count, "Comparison size: {0} = {1} Output size", blobs.Count(), Output.Count);
            if (blobs.Count() != Output.Count)
            {
                return;
            }

            int count = 0;
            foreach (ICloudBlob blob in blobs)
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
                        ret = RemoveAzureStorageContainer(name, (bool)args[0]);
                        break;

                    case "GetAzureStorageContainer":
                        ret = GetAzureStorageContainer(name);
                        break;

                    case "SetAzureStorageContainerACL":
                        ret = SetAzureStorageContainerACL(name, (BlobContainerPublicAccessType)args[0], (bool)args[1]);
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

            if (obj is ICloudBlob)
            {
                ICloudBlob blob = (ICloudBlob)obj;
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
            if (obj is ICloudBlob)
            {
                return ((ICloudBlob)obj).Properties;
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
            return connectionString;
        }

        public override void SetVariable(string variableName, object value)
        {
            throw new NotImplementedException();
        }

        public override void ChangeLocation(string path)
        {
            throw new NotImplementedException();
        }

        public override void NewFileShare(string fileShareName, object contextObject = null)
        {
            bool needAccountParam = true;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("share create \"{0}\"", fileShareName);
            if (contextObject != null)
            {
                sb.AppendFormat(" -c \"{0}\"", contextObject);
                needAccountParam = false;
            }

            this.RunNodeJSProcess(sb.ToString(), needAccountParam: needAccountParam);
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

        public override void GetFileShareByName(string fileShareName)
        {
            this.RunNodeJSProcess(string.Format("share show \"{0}\"", fileShareName));
        }

        public override void GetFileShareByPrefix(string prefix)
        {
            this.RunNodeJSProcess(string.Format("share list \"{0}\"", prefix));
        }

        public override void RemoveFileShareByName(string fileShareName, bool passThru = false, object contextObject = null)
        {
            bool needAccountParam = true;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("share delete \"{0}\"", fileShareName);
            if (contextObject != null)
            {
                sb.AppendFormat(" -c \"{0}\"", contextObject);
                needAccountParam = false;
            }

            this.RunNodeJSProcess(sb.ToString(), true, needAccountParam);
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
            bool needAccountParam = true;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("directory create \"{0}\" \"{1}\"", fileShareName, directoryName);
            if (contextObject != null)
            {
                sb.AppendFormat(" -c \"{0}\"", contextObject);
                needAccountParam = false;
            }

            this.RunNodeJSProcess(sb.ToString(), needAccountParam: needAccountParam);
        }

        public override void RemoveDirectory(CloudFileShare fileShare, string directoryName)
        {
            this.RemoveDirectory(fileShare.Name, directoryName);
        }

        public override void RemoveDirectory(CloudFileDirectory directory, string path)
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectory(string fileShareName, string directoryName, object contextObject = null)
        {
            bool needAccountParam = true;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("directory delete \"{0}\" \"{1}\"", fileShareName, directoryName);
            if (contextObject != null)
            {
                sb.AppendFormat(" -c \"{0}\"", contextObject);
                needAccountParam = false;
            }

            this.RunNodeJSProcess(sb.ToString(), true, needAccountParam);
        }

        public override void RemoveFile(CloudFileShare fileShare, string fileName)
        {
            this.RemoveFile(fileShare.Name, fileName);
        }

        public override void RemoveFile(CloudFileDirectory directory, string fileName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFile(CloudFile file)
        {
            this.RemoveFile(file.Share.Name, CloudFileUtil.GetFullPath(file));
        }

        public override void RemoveFile(string fileShareName, string fileName, object contextObject = null)
        {
            bool needAccountParam = true;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("file delete \"{0}\" \"{1}\"", fileShareName, fileName);
            if (contextObject != null)
            {
                sb.AppendFormat(" -c \"{0}\"", contextObject);
                needAccountParam = false;
            }

            this.RunNodeJSProcess(sb.ToString(), true, needAccountParam);
        }

        public override void ListFiles(string fileShareName, string path = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("file list \"{0}\"", fileShareName);
            if (path != null)
            {
                sb.AppendFormat(" \"{0}\"", path);
            }
            this.RunNodeJSProcess(sb.ToString());
        }

        public override void ListFiles(CloudFileShare fileShare, string path = null)
        {
            this.ListFiles(fileShare.Name, path);
        }

        public override void ListFiles(CloudFileDirectory directory, string path = null)
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

        public override void DownloadFile(string fileShareName, string path, string destination, bool overwrite = false, object contextObject = null)
        {
            bool needAccountParam = true;
            StringBuilder sb = new StringBuilder();
            string dest = destination.TrimEnd(CloudFileUtil.PathSeparators);
            if (AgentOSType != OSType.Windows)
            {
                dest = FileUtil.GetLinuxPath(dest);
            }
            sb.AppendFormat("file download \"{0}\" \"{1}\" \"{2}\"", fileShareName, path, dest);
            if (contextObject != null)
            {
                sb.AppendFormat(" -c \"{0}\"", contextObject);
                needAccountParam = false;
            }

            this.RunNodeJSProcess(sb.ToString(), overwrite, needAccountParam);
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
            bool needAccountParam = true;
            StringBuilder sb = new StringBuilder();
            if (AgentOSType != OSType.Windows)
            {
                source = FileUtil.GetLinuxPath(source);
            }
            sb.AppendFormat("file upload \"{0}\" \"{1}\" \"{2}\"", source, fileShareName, path);
            if (contextObject != null)
            {
                sb.AppendFormat(" -c \"{0}\"", contextObject);
                needAccountParam = false;
            }

            this.RunNodeJSProcess(sb.ToString(), overwrite, needAccountParam: needAccountParam);
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
        /// SAS token APIs
        ///-------------------------------------
        public override bool NewAzureStorageContainerSAS(string container, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false)
        {
            string command = string.Format("container sas create {0}", container);

            command = GetGeneralSASCmd(command, permission, startTime, expiryTime, policy);

            return RunNodeJSProcess(command);
        }

        public override bool NewAzureStorageBlobSAS(string container, string blob, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false)
        {
            string command = string.Format("blob sas create {0} \"{1}\"", container, blob);

            command = GetGeneralSASCmd(command, permission, startTime, expiryTime, policy);

            return RunNodeJSProcess(command);
        }

        public override bool NewAzureStorageTableSAS(string name, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, string startpk = "", string startrk = "", string endpk = "", string endrk = "")
        {
            string command = string.Format("table sas create {0}", name);

            command = GetGeneralSASCmd(command, permission, startTime, expiryTime, policy);

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
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false)
        {
            string command = string.Format("queue sas create {0}", name);

            command = GetGeneralSASCmd(command, permission, startTime, expiryTime, policy);

            return RunNodeJSProcess(command);
        }

        public override string GetBlobSasFromCmd(string containerName, string blobName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false)
        {
            Test.Assert(NewAzureStorageBlobSAS(containerName, blobName, policy, permission, startTime, expiryTime, fulluri),
                    "Generate blob sas token should succeed");
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

        public override string GetBlobSasFromCmd(ICloudBlob blob, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false)
        {
            return GetBlobSasFromCmd(blob.Container.Name, blob.Name, policy, permission, startTime, expiryTime, fulluri);
        }

        public override string GetContainerSasFromCmd(string containerName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false)
        {
            Test.Assert(NewAzureStorageContainerSAS(containerName, policy, permission, startTime, expiryTime, fulluri),
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

        public override string GetQueueSasFromCmd(string queueName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false)
        {
            Test.Assert(NewAzureStorageQueueSAS(queueName, policy, permission, startTime, expiryTime, fulluri),
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
            string startpk = "", string startrk = "", string endpk = "", string endrk = "")
        {
            Test.Assert(NewAzureStorageTableSAS(tableName, policy, permission, startTime, expiryTime, fulluri,
                startpk, startrk, endpk, endrk),
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

        internal string GetGeneralSASCmd(string command, string permission, DateTime? startTime, DateTime? expiryTime, string policy)
        {
            if (string.IsNullOrEmpty(policy))
            {
                if (string.IsNullOrEmpty(permission))
                {
                    permission = "r";
                }

                command += " --permissions " + permission +
                    " --expiry " + (expiryTime.HasValue ? expiryTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") : DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }
            else
            {
                command += " --policy " + policy;
            }

            if (startTime.HasValue)
            {
                command += " --start " + startTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return command;
        }
    }

    public enum OSType
    {
        Windows, Linux, Mac
    }

    public class OSConfig
    {
        public string PLinkPath;
        public string UserName;
        public string HostName;
        public string Port = "22";
        public string PrivateKeyPath;

        public string ConnectionStr;
        public string Name;
        public string Key;
        public string Sas;

        public OSConfig()
        {
            UseEnvVar = false;
        }

        public string ConnectionString
        {
            get { return ConnectionStr; }
            set
            {
                ConnectionStr = value;
                Name = string.Empty;
                Key = string.Empty;
            }
        }

        public string AccountName
        {
            get { return Name; }
            set
            {
                Name = value;
                ConnectionStr = string.Empty;
            }
        }

        public string AccountKey
        {
            get { return Key; }
            set
            {
                Key = value;
                ConnectionStr = string.Empty;
            }
        }

        public string SAS
        {
            get { return Sas; }
            set
            {
                Sas = value;
                ConnectionStr = string.Empty;
            }
        }

        public bool UseEnvVar { get; set; }
    }
}
