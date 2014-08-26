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
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table;
using MS.Test.Common.MsTestLib;

namespace Management.Storage.ScenarioTest
{
    public abstract class Agent : IDisposable
    {
        private const string NotImplemented = "Not implemented in Agent!";
        /// <summary>
        /// output data returned after agent operation
        /// </summary>   
        public Collection<Dictionary<string, object>> Output { get { return _Output; } set { _Output = value; } }

        /// <summary>
        /// error messages returned after agent operation
        /// </summary>   
        public Collection<string> ErrorMessages { get { return _ErrorMessages; } protected set { _ErrorMessages = value; } }

        /// <summary>
        /// this table would store all the expected error messages, the format would be:
        /// case name : expected error message
        /// </summary>   
        protected static Hashtable ExpectedErrorMsgTable;

        /// <summary>
        /// get expected error message from table
        /// </summary>   
        protected string GetExpectedErrorMsg(string caseName, params string[] args)
        {
            if (!ExpectedErrorMsgTable.ContainsKey(caseName))
            {
                throw new Exception("case name " + caseName + " not found in error message table!");
            }
            string msg = ExpectedErrorMsgTable[caseName].ToString();

            return string.Format(msg, args);
        }

        /// <summary>
        /// validate the output error message with expected error message
        /// </summary>   
        public void ValidateErrorMessage(string caseName, params string[] args)
        {
            string expectedErrorMessage = GetExpectedErrorMsg(caseName, args);
            if (ErrorMessages.Count == 0)
            {
                throw new Exception("returned error message is empty!");
            }
            Test.Assert(ErrorMessages[0].StartsWith(expectedErrorMessage), String.Format("Expected error message should start with {0}, and actually it's {1}",
                expectedErrorMessage, ErrorMessages[0]));
        }

        public abstract bool ShowAzureStorageAccountConnectionString(string accountName);

        /// <summary>
        /// Return true if succeed otherwise return false
        /// </summary>   
        public abstract bool NewAzureStorageContainer(string ContainerName);

        /// <summary>
        /// Parameters:
        ///     ContainerName:
        ///         1. Could be empty if no Container parameter specified
        ///         2. Could contain wildcards
        /// </summary>
        public abstract bool GetAzureStorageContainer(string ContainerName);
        public abstract bool GetAzureStorageContainerByPrefix(string Prefix);
        public abstract bool SetAzureStorageContainerACL(string ContainerName, BlobContainerPublicAccessType PublicAccess, bool PassThru = true);
        public abstract bool SetAzureStorageContainerACL(string[] ContainerNames, BlobContainerPublicAccessType PublicAccess, bool PassThru = true);
        public abstract bool RemoveAzureStorageContainer(string ContainerName, bool Force = true);
        /// <summary>
        /// For pipeline, new/remove a list of container names
        /// </summary>
        public abstract bool NewAzureStorageContainer(string[] ContainerNames);
        public abstract bool RemoveAzureStorageContainer(string[] ContainerNames, bool Force = true);

        public abstract bool NewAzureStorageQueue(string QueueName);
        /// <summary>
        /// Parameters:
        ///     ContainerName:
        ///         1. Could be empty if no Queue parameter specified
        ///         2. Could contain wildcards
        /// </summary>
        public abstract bool GetAzureStorageQueue(string QueueName);
        public abstract bool GetAzureStorageQueueByPrefix(string Prefix);
        public abstract bool RemoveAzureStorageQueue(string QueueName, bool Force = true);

        /// <summary>
        /// For pipeline, new/remove a list of queue names
        /// </summary>
        public abstract bool NewAzureStorageQueue(string[] QueueNames);
        public abstract bool RemoveAzureStorageQueue(string[] QueueNames, bool Force = true);

        /// <summary>
        /// Parameters:
        ///     Block:
        ///         true for BlockBlob, false for PageBlob
        ///     ConcurrentCount:
        ///         -1 means use the default value
        /// </summary>
        public abstract bool SetAzureStorageBlobContent(string FileName, string ContainerName, BlobType Type, string BlobName = "",
            bool Force = true, int ConcurrentCount = -1, Hashtable properties = null, Hashtable metadata = null);
        public abstract bool GetAzureStorageBlobContent(string Blob, string FileName, string ContainerName,
            bool Force = true, int ConcurrentCount = -1);
        public abstract bool GetAzureStorageBlob(string BlobName, string ContainerName);
        public abstract bool GetAzureStorageBlobByPrefix(string Prefix, string ContainerName);

        /// <summary>
        /// upload all files in one directory to a specific container (no recursive)
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="containerName"></param>
        /// <param name="blobType"></param>
        /// <param name="force"></param>
        /// <param name="concurrentCount"></param>
        /// <returns></returns>
        public abstract bool UploadLocalFiles(string dirPath, string containerName, BlobType blobType, bool force = true, int concurrentCount = -1);

        /// <summary>
        /// Download all blobs in one container to a specific directory
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="containerName"></param>
        /// <param name="force"></param>
        /// <param name="concurrentCount"></param>
        /// <returns></returns>
        public abstract bool DownloadBlobFiles(string dirPath, string containerName, bool force = true, int concurrentCount = -1);

        public abstract bool RemoveAzureStorageBlob(string BlobName, string ContainerName, bool onlySnapshot = false, bool force = true);

        public abstract bool NewAzureStorageTable(string TableName);
        public abstract bool NewAzureStorageTable(string[] TableNames);
        public abstract bool GetAzureStorageTable(string TableName);
        public abstract bool GetAzureStorageTableByPrefix(string Prefix);
        public abstract bool RemoveAzureStorageTable(string TableName, bool Force = true);
        public abstract bool RemoveAzureStorageTable(string[] TableNames, bool Force = true);

        public abstract bool StartAzureStorageBlobCopy(string sourceUri, string destContainerName, string destBlobName, object destContext, bool force = true);
        public abstract bool StartAzureStorageBlobCopy(string srcContainerName, string srcBlobName, string destContainerName, string destBlobName, object destContext = null, bool force = true);
        public abstract bool StartAzureStorageBlobCopy(ICloudBlob srcBlob, string destContainerName, string destBlobName, object destContext = null, bool force = true);

        public abstract bool GetAzureStorageBlobCopyState(string containerName, string blobName, bool waitForComplete);
        public abstract bool GetAzureStorageBlobCopyState(ICloudBlob blob, object context, bool waitForComplete);
        public abstract bool StopAzureStorageBlobCopy(string containerName, string blobName, string copyId, bool force);


        ///-------------------------------------
        /// Logging & Metrics APIs
        ///-------------------------------------
        public virtual bool GetAzureStorageServiceLogging(Constants.ServiceType serviceType) { return false; }
        public virtual bool GetAzureStorageServiceMetrics(Constants.ServiceType serviceType, Constants.MetricsType metricsType) { return false; }
        public virtual bool SetAzureStorageServiceLogging(Constants.ServiceType serviceType, string loggingOperations = "", string loggingRetentionDays = "",
            string loggingVersion = "", bool passThru = false) { return false; }
        public virtual bool SetAzureStorageServiceLogging(Constants.ServiceType serviceType, LoggingOperations[] loggingOperations, string loggingRetentionDays = "",
            string loggingVersion = "", bool passThru = false) { return false; }
        public virtual bool SetAzureStorageServiceMetrics(Constants.ServiceType serviceType, Constants.MetricsType metricsType, string metricsLevel = "", string metricsRetentionDays = "",
            string metricsVersion = "", bool passThru = false) { return false; }

        ///-------------------------------------
        /// SAS token APIs
        ///-------------------------------------
        public virtual bool NewAzureStorageContainerSAS(string container, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false) { return false; }

        public virtual bool NewAzureStorageBlobSAS(string container, string blob, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false) { return false; }

        public virtual bool NewAzureStorageTableSAS(string name, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, string startpk = "", string startrk = "", string endpk = "", string endrk = "") { return false; }

        public virtual bool NewAzureStorageQueueSAS(string name, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false) { return false; }

        public abstract void OutputValidation(Collection<Dictionary<string, object>> comp);
        public abstract void OutputValidation(IEnumerable<CloudBlobContainer> containers);
        public abstract void OutputValidation(IEnumerable<CloudFileShare> shares);
        public abstract void OutputValidation(IEnumerable<IListFileItem> items);
        public abstract void OutputValidation(IEnumerable<BlobContainerPermissions> permissions);
        public abstract void OutputValidation(IEnumerable<ICloudBlob> blobs);
        public abstract void OutputValidation(IEnumerable<CloudTable> tables);
        public abstract void OutputValidation(IEnumerable<CloudQueue> queues);
        public virtual void OutputValidation(ServiceProperties serviceProperties, string propertiesType) { throw new NotImplementedException(NotImplemented); }

        public virtual string GetBlobSasFromCmd(string containerName, string blobName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false) { return string.Empty; }
        public virtual string GetBlobSasFromCmd(ICloudBlob blob, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false) { return string.Empty; }

        public virtual string GetContainerSasFromCmd(string containerName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false) { return string.Empty; }

        public virtual string GetQueueSasFromCmd(string queueName, string policy, string permission,
                    DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false) { return string.Empty; }

        public virtual string GetTableSasFromCmd(string tableName, string policy, string permission,
                    DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false,
                    string startpk = "", string startrk = "", string endpk = "", string endrk = "") { return string.Empty; }

        #region xSMB operations

        public abstract bool HadErrors { get; }

        public abstract object CreateStorageContextObject(string connectionString);

        public abstract void SetVariable(string variableName, object value);

        public abstract void ChangeLocation(string path);

        public abstract void NewFileShare(string fileShareName, object contextObject = null);

        public abstract bool NewFileShares(string[] names);
        public abstract bool GetFileSharesByPrefix(string prefix);
        public abstract bool RemoveFileShares(string[] names);
        public abstract bool NewDirectories(string fileShareName, string[] directoryNames);
        public abstract bool ListDirectories(string fileShareName);
        public abstract bool RemoveDirectories(string fileShareName, string[] directoryNames);

        public abstract void NewFileShareFromPipeline();

        public abstract void NewDirectoryFromPipeline(string fileShareName);

        public abstract void UploadFilesFromPipeline(string fileShareName, string localFileName);

        public abstract void UploadFilesInFolderFromPipeline(string fileShareName, string folder);

        public abstract void RemoveFileShareFromPipeline();

        public abstract void RemoveDirectoriesFromPipeline(string fileShareName);

        public abstract void RemoveFilesFromPipeline(string fileShareName);

        public abstract void GetFileShareByName(string fileShareName);

        public abstract void GetFileShareByPrefix(string prefix);

        public abstract void RemoveFileShareByName(string fileShareName, bool passThru = false, object contextObject = null);

        public abstract void NewDirectory(CloudFileShare fileShare, string directoryName);

        public abstract void NewDirectory(CloudFileDirectory directory, string directoryName);

        public abstract void NewDirectory(string fileShareName, string directoryName, object contextObject = null);

        public abstract void RemoveDirectory(CloudFileShare fileShare, string directoryName);

        public abstract void RemoveDirectory(CloudFileDirectory directory, string path);

        public abstract void RemoveDirectory(string fileShareName, string directoryName, object contextObject = null);

        public abstract void RemoveFile(CloudFileShare fileShare, string fileName);

        public abstract void RemoveFile(CloudFileDirectory directory, string fileName);

        public abstract void RemoveFile(CloudFile file);

        public abstract void RemoveFile(string fileShareName, string fileName, object contextObject = null);

        public abstract void ListFiles(string fileShareName, string path = null);

        public abstract void ListFiles(CloudFileShare fileShare, string path = null);

        public abstract void ListFiles(CloudFileDirectory directory, string path = null);

        public abstract void DownloadFile(CloudFile file, string destination, bool overwrite = false);

        public abstract void DownloadFile(CloudFileDirectory directory, string path, string destination, bool overwrite = false);

        public abstract void DownloadFile(CloudFileShare fileShare, string path, string destination, bool overwrite = false);

        public abstract void DownloadFile(string fileShareName, string path, string destination, bool overwrite = false, object contextObject = null);

        public abstract void DownloadFiles(string fileShareName, string path, string destination, bool overwrite = false);

        public abstract void UploadFile(CloudFileShare fileShare, string source, string path, bool overwrite = false, bool passThru = false);

        public abstract void UploadFile(CloudFileDirectory directory, string source, string path, bool overwrite = false, bool passThru = false);

        public abstract void UploadFile(string fileShareName, string source, string path, bool overwrite = false, bool passThru = false, object contextObject = null);

        public abstract void AssertNoError();

        public abstract IExecutionResult Invoke(IEnumerable input = null, bool traceCommand = true);

        public abstract void AssertErrors(Action<IExecutionError> assertErrorAction, int expectedErrorCount = 1);

        public abstract void Clear();

        public void Dispose()
        {
            this.DisposeInternal();
        }

        #endregion

        protected Collection<Dictionary<string, object>> _Output = new Collection<Dictionary<string, object>>();
        protected Collection<string> _ErrorMessages = new Collection<string>();
        
        protected virtual void DisposeInternal()
        {
        }
    }
}
