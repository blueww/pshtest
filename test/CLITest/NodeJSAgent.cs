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

namespace Management.Storage.ScenarioTest
{
    /// <summary>
    /// This class is used to create an agent which could run Node.js xplat commands
    /// </summary>
    public class NodeJSAgent : Agent
    {
        private const string NotImplemented = "Not implemented in NodeJS Agent!";
        private const string ExportPathCommand = "export PATH=$PATH:/usr/local/bin/;";

        private static int DefaultMaxWaitingTime = 600000;  // in miliseconds

        private static Hashtable ExpectedErrorMsgTableNodeJS = new Hashtable() {
                {"GetBlobContentWithNotExistsBlob", "Can not find blob '{0}' in container '{1}'"},
                {"GetBlobContentWithNotExistsContainer", "Can not find blob '{0}' in container '{1}'"},
                {"RemoveBlobWithLease", "There is currently a lease on the blob and no lease ID was specified in the request"},
                {"SetBlobContentWithInvalidBlobType", "The blob type is invalid for this operation"},
                {"SetPageBlobWithInvalidFileSize", "The page blob size must be aligned to a 512-byte boundary"}, 
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

        internal static void SetProcessInfo(Process p, string argument)
        {
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            p.StartInfo.WorkingDirectory = WorkingDirectory;

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
            p.StartInfo.Arguments += string.Format(" azure storage {0} --json", argument);

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

        internal bool RunNodeJSProcess(string argument, bool force = false)
        {
            if (force)
            {
                argument += " --quiet";
            }

            if (!AgentConfig.UseEnvVar)
            {
                argument = AddAccountParameters(argument);
            }

            Process p = new Process();
            SetProcessInfo(p, argument);
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();

            p.WaitForExit(MaxWaitingTime);
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
                if (output.Trim().Length > 0)
                {
                    // modify output as a collection
                    if (output.TrimStart()[0] != '[')
                    {
                        output = '[' + output + ']';
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
            throw new NotImplementedException(NotImplemented);
        }

        public override bool GetAzureStorageQueue(string queueName)
        {
            return RunNodeJSProcess("queue list " + queueName);
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
            throw new NotImplementedException(NotImplemented);
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
            throw new NotImplementedException(NotImplemented);
        }

        public override bool GetAzureStorageTable(string tableName)
        {
            return RunNodeJSProcess("table list " + tableName);
        }

        public override bool GetAzureStorageTableByPrefix(string prefix)
        {
            throw new NotImplementedException(NotImplemented);
        }

        public override bool RemoveAzureStorageTable(string tableName, bool force = true)
        {
            return RunNodeJSProcess("table delete " + tableName, force);
        }

        public override bool RemoveAzureStorageTable(string[] tableNames, bool force = true)
        {
            throw new NotImplementedException(NotImplemented);
        }

        public override bool StartAzureStorageBlobCopy(string sourceUri, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            throw new NotImplementedException(NotImplemented);
        }

        public override bool StartAzureStorageBlobCopy(string srcContainerName, string srcBlobName, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            throw new NotImplementedException(NotImplemented);
        }

        public override bool StartAzureStorageBlobCopy(ICloudBlob srcBlob, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            throw new NotImplementedException(NotImplemented);
        }

        public override bool GetAzureStorageBlobCopyState(string containerName, string blobName, bool waitForComplete)
        {
            throw new NotImplementedException(NotImplemented);
        }

        public override bool GetAzureStorageBlobCopyState(ICloudBlob blob, object context, bool waitForComplete)
        {
            throw new NotImplementedException(NotImplemented);
        }

        public override bool StopAzureStorageBlobCopy(string containerName, string blobName, string copyId, bool force)
        {
            throw new NotImplementedException(NotImplemented);
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
                throw new NotImplementedException();
            }
        }

        public override object CreateStorageContextObject(string connectionString)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override void GetFileShareByPrefix(string prefix)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFileShareByName(string fileShareName, bool passThru = false, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override void NewDirectory(CloudFileShare fileShare, string directoryName)
        {
            throw new NotImplementedException();
        }

        public override void NewDirectory(CloudFileDirectory directory, string directoryName)
        {
            throw new NotImplementedException();
        }

        public override void NewDirectory(string fileShareName, string directoryName, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectory(CloudFileShare fileShare, string directoryName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectory(CloudFileDirectory directory, string path)
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectory(string fileShareName, string directoryName, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFile(CloudFileShare fileShare, string fileName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFile(CloudFileDirectory directory, string fileName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFile(CloudFile file)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFile(string fileShareName, string fileName, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override void ListFiles(string fileShareName, string path = null)
        {
            throw new NotImplementedException();
        }

        public override void ListFiles(CloudFileShare fileShare, string path = null)
        {
            throw new NotImplementedException();
        }

        public override void ListFiles(CloudFileDirectory directory, string path = null)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(CloudFile file, string destination, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(CloudFileDirectory directory, string path, string destination, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(CloudFileShare fileShare, string path, string destination, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(string fileShareName, string path, string destination, bool overwrite = false, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override void UploadFile(CloudFileShare fileShare, string source, string path, bool overwrite = false, bool passThru = false)
        {
            throw new NotImplementedException();
        }

        public override void UploadFile(CloudFileDirectory directory, string source, string path, bool overwrite = false, bool passThru = false)
        {
            throw new NotImplementedException();
        }

        public override void UploadFile(string fileShareName, string source, string path, bool overwrite = false, bool passThru = false, object contextObject = null)
        {
            throw new NotImplementedException();
        }

        public override void AssertNoError()
        {
            throw new NotImplementedException();
        }

        public override IExecutionResult Invoke(IEnumerable input = null, bool traceCommand = true)
        {
            throw new NotImplementedException();
        }

        public override void AssertErrors(Action<IExecutionError> assertErrorAction, int expectedErrorCount = 1)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            throw new NotImplementedException();
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

        public bool UseEnvVar { get; set; }
    }
}
