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

using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Management.Storage.ScenarioTest
{
    /// <summary>
    /// this class contains all the functional test cases for PowerShell Blob cmdlets
    /// </summary>
    [TestClass]
    public class CLIBlobFunc : TestBase
    {
        private static CloudBlobHelper BlobHelper;
        private static string BlockFilePath;
        private static string PageFilePath;

        #region Additional test attributes

        [ClassInitialize()]
        public static void CLIBlobFuncClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            //init the blob helper for blob related operations
            BlobHelper = new CloudBlobHelper(StorageAccount);

            BlockFilePath = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());
            PageFilePath = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());
            FileUtil.CreateDirIfNotExits(Path.GetDirectoryName(BlockFilePath));
            FileUtil.CreateDirIfNotExits(Path.GetDirectoryName(PageFilePath));

            // Generate block file and page file which are used for uploading
            FileUtil.GenerateMediumFile(BlockFilePath, Utility.GetRandomTestCount(1, 10));
            FileUtil.GenerateMediumFile(PageFilePath, Utility.GetRandomTestCount(1, 10));
        }

        [ClassCleanup()]
        public static void CLIBlobFuncClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        #endregion

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RootBlobOperations()
        {
            string DownloadDirPath = Test.Data.Get("DownloadDir");
            FileUtil.CreateDirIfNotExits(DownloadDirPath);
            RootBlobOperations(agent, BlockFilePath, DownloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            RootBlobOperations(agent, PageFilePath, DownloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void GetNonExistingBlob()
        {
            GetNonExistingBlob(agent);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveNonExistingBlob()
        {
            RemoveNonExistingBlob(agent);
        }

        /// <summary>
        /// Positive Functional Cases : get non existing blob (only for nodejs)
        /// 1. Get a non-existing blob (Negative 1)
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetNonExistingBlobNodeJS()
        {
            string ContainerName = Utility.GenNameString("upload-");

            // create the container
            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetContainerReference(ContainerName);
            container.CreateIfNotExists();

            try
            {
                string BlobName = Utility.GenNameString("nonexisting");

                // Delete the blob if it exists
                ICloudBlob blob = BlobHelper.QueryBlob(ContainerName, BlobName);
                if (blob != null)
                    blob.DeleteIfExists();

                //--------------Get operation--------------
                Test.Assert(!agent.GetAzureStorageBlob(BlobName, ContainerName), Utility.GenComparisonData("get blob", true));
                // Verification for returned values
                Test.Assert(agent.Output.Count == 0, "Only 0 row returned : {0}", agent.Output.Count);
                Test.Assert(agent.ErrorMessages.Count == 1, "1 error message returned : {0}", agent.ErrorMessages.Count);
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Functional Cases:
        /// 1. Upload a new blob file in the root container     (Set-AzureStorageBlobContent Positive 2)
        /// 2. Get an existing blob in the root container       (Get-AzureStorageBlob Positive 2)
        /// 3. Download an existing blob in the root container  (Get-AzureStorageBlobContent Positive 2)
        /// 4. Remove an existing blob in the root container    (Remove-AzureStorageBlob Positive 2)
        /// </summary>
        internal void RootBlobOperations(Agent agent, string UploadFilePath, string DownloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            const string ROOT_CONTAINER_NAME = "$root";
            string blobName = Path.GetFileName(UploadFilePath);
            string downloadFilePath = Path.Combine(DownloadDirPath, blobName);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, blobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetRootContainerReference();
            container.CreateIfNotExists();

            //--------------Upload operation--------------
            Test.Assert(agent.SetAzureStorageBlobContent(UploadFilePath, ROOT_CONTAINER_NAME, Type), Utility.GenComparisonData("SendAzureStorageBlob", true));
            ICloudBlob blob = BlobHelper.QueryBlob(ROOT_CONTAINER_NAME, blobName);
            blob.FetchAttributes();
            // Verification for returned values
            CloudBlobUtil.PackBlobCompareData(blob, dic);
            agent.OutputValidation(comp);

            Test.Assert(blob.Exists(), "blob " + blobName + " should exist!");

            // validate the ContentType value for GetAzureStorageBlob operation
            if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob)
            {
                dic["ContentType"] = "application/octet-stream";
            }

            //--------------Get operation--------------
            Test.Assert(agent.GetAzureStorageBlob(blobName, ROOT_CONTAINER_NAME), Utility.GenComparisonData("GetAzureStorageBlob", true));
            // Verification for returned values
            agent.OutputValidation(comp);

            if (agent is NodeJSAgent)
            {
                NodeJSAgent nodejsAgent = (NodeJSAgent)agent;
                //--------------Show operation--------------
                Test.Assert(nodejsAgent.ShowAzureStorageBlob(blobName, ROOT_CONTAINER_NAME), Utility.GenComparisonData("ShowAzureStorageBlob", true));
                // Verification for returned values
                nodejsAgent.OutputValidation(comp);
            }

            //--------------Download operation--------------
            downloadFilePath = Path.Combine(DownloadDirPath, blobName);    
            Test.Assert(agent.GetAzureStorageBlobContent(blobName, downloadFilePath, ROOT_CONTAINER_NAME),
                Utility.GenComparisonData("GetAzureStorageBlobContent", true));
            // Verification for returned values
            agent.OutputValidation(comp);

            Test.Assert(FileUtil.CompareTwoFiles(downloadFilePath, UploadFilePath),
                String.Format("File '{0}' should be bit-wise identicial to '{1}'", downloadFilePath, UploadFilePath));

            //--------------Remove operation--------------
            Test.Assert(agent.RemoveAzureStorageBlob(blobName, ROOT_CONTAINER_NAME), Utility.GenComparisonData("RemoveAzureStorageBlob", true));
            blob = BlobHelper.QueryBlob(ROOT_CONTAINER_NAME, blobName);
            Test.Assert(blob == null, "blob {0} should not exist!", blobName);
        }

        /// <summary>
        /// Negative Functional Cases : for Get-AzureStorageBlob 
        /// 1. Get a non-existing blob (Negative 1)
        /// </summary>
        internal void GetNonExistingBlob(Agent agent)
        {
            string CONTAINER_NAME = Utility.GenNameString("upload-");

            // create the container
            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetContainerReference(CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                string BLOB_NAME = Utility.GenNameString("nonexisting");

                // Delete the blob if it exists
                ICloudBlob blob = BlobHelper.QueryBlob(CONTAINER_NAME, BLOB_NAME);
                if (blob != null)
                    blob.DeleteIfExists();

                //--------------Get operation--------------
                Test.Assert(!agent.GetAzureStorageBlob(BLOB_NAME, CONTAINER_NAME), Utility.GenComparisonData("GetAzureStorageBlob", false));
                // Verification for returned values
                Test.Assert(agent.Output.Count == 0, "Only 0 row returned : {0}", agent.Output.Count);
                //the same error may output different error messages in different environments
                bool expectedError = agent.ErrorMessages[0].StartsWith(String.Format("Can not find blob '{0}' in container '{1}'.", BLOB_NAME, CONTAINER_NAME))
                    || agent.ErrorMessages[0].StartsWith("The remote server returned an error: (404) Not Found");
                Test.Assert(expectedError, agent.ErrorMessages[0]);
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Negative Functional Cases : for Remove-AzureStorageBlob 
        /// 1. Remove a non-existing blob (Negative 2)
        /// </summary>
        internal void RemoveNonExistingBlob(Agent agent)
        {
            string CONTAINER_NAME = Utility.GenNameString("upload-");
            string BLOB_NAME = Utility.GenNameString("nonexisting");

            // create the container
            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetContainerReference(CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                // Delete the blob if it exists
                ICloudBlob blob = BlobHelper.QueryBlob(CONTAINER_NAME, BLOB_NAME);
                if (blob != null)
                    blob.DeleteIfExists();

                //--------------Remove operation--------------
                Test.Assert(!agent.RemoveAzureStorageBlob(BLOB_NAME, CONTAINER_NAME), Utility.GenComparisonData("RemoveAzureStorageBlob", false));
                // Verification for returned values
                Test.Assert(agent.Output.Count == 0, "Only 0 row returned : {0}", agent.Output.Count);
                //the same error may output different error messages in different environments
                bool expectedError = agent.ErrorMessages[0].StartsWith(String.Format("Can not find blob '{0}' in container '{1}'.", BLOB_NAME, CONTAINER_NAME))
                    || agent.ErrorMessages[0].StartsWith("The remote server returned an error: (404) Not Found")
                    || agent.ErrorMessages[0].StartsWith("The specified blob does not exist.");
                Test.Assert(expectedError, agent.ErrorMessages[0]);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }
    }
}
