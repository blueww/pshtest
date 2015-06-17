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

namespace Management.Storage.ScenarioTest.BVT.HTTPS
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.IO;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using StorageBlob = Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// bvt cases for anonymous storage account
    /// </summary>
    [TestClass]
    public class AnonymousBVT : TestBase
    {
        protected static string downloadDirRoot;

        private static string ContainerPrefix = "anonymousbvt";
        protected static string StorageAccountName;
        protected static string StorageEndPoint;

        [ClassInitialize()]
        public static void AnonymousBVTClassInitialize(TestContext testContext)
        {
            useHttps = true;
            Initialize(testContext, useHttps);
        }

        [ClassCleanup()]
        public static void AnonymousBVTClassCleanup()
        {
            FileUtil.CleanDirectory(downloadDirRoot);
            CLICommonBVT.RestoreSubScriptionAndEnvConnectionString();
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// create download dir
        /// </summary>
        //TODO remove code redundancy
        protected static void SetupDownloadDir()
        {
            FileUtil.CreateDirIfNotExits(downloadDirRoot);
            FileUtil.CleanDirectory(downloadDirRoot);
        }

        [TestMethod]
        [TestCategory(Tag.BVT)]
        public void ListContainerWithContianerPermission()
        {
            string containerName = Utility.GenNameString(ContainerPrefix);
            CloudBlobContainer container = blobUtil.CreateContainer(containerName, BlobContainerPublicAccessType.Container);

            try
            {
                Test.Assert(agent.GetAzureStorageContainer(containerName), Utility.GenComparisonData("GetAzureStorageContainer", true));

                Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, containerName);

                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };
                //remove the permssion information for anonymous storage account
                CloudBlobUtil.PackContainerCompareData(container, dic);
                dic["PublicAccess"] = null;
                dic["Permission"] = null;
                // Verification for returned values
                agent.OutputValidation(comp);

                //check the http or https usage
                CloudBlobContainer retrievedContainer = (CloudBlobContainer)agent.Output[0]["CloudBlobContainer"]; ;
                string uri = retrievedContainer.Uri.ToString();
                string uriPrefix = string.Empty;

                if (useHttps)
                {
                    uriPrefix = "https";
                }
                else
                {
                    uriPrefix = "http";
                }

                Test.Assert(uri.ToString().StartsWith(uriPrefix), string.Format("The prefix of container uri should be {0}, actually it's {1}", uriPrefix, uri));
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// list blobs when container's public access level is public
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        public void ListBlobsWithBlobPermission()
        {
            string containerName = Utility.GenNameString(ContainerPrefix);
            CloudBlobContainer container = blobUtil.CreateContainer(containerName, BlobContainerPublicAccessType.Blob);

            try
            {
                string pageBlobName = Utility.GenNameString("pageblob");
                string blockBlobName = Utility.GenNameString("blockblob");
                string appendBlobName = Utility.GenNameString("appendblob");
                CloudBlob blockBlob = blobUtil.CreateBlockBlob(container, blockBlobName);
                CloudBlob pageBlob = blobUtil.CreatePageBlob(container, pageBlobName);
                CloudBlob appendBlob = blobUtil.CreateAppendBlob(container, appendBlobName);

                Test.Assert(agent.GetAzureStorageBlob(blockBlobName, containerName), Utility.GenComparisonData("Get-AzureStorageBlob", true));
                agent.OutputValidation(new List<CloudBlob> { blockBlob });
                Test.Assert(agent.GetAzureStorageBlob(pageBlobName, containerName), Utility.GenComparisonData("Get-AzureStorageBlob", true));
                agent.OutputValidation(new List<CloudBlob> { pageBlob });
                Test.Assert(agent.GetAzureStorageBlob(appendBlobName, containerName), Utility.GenComparisonData("Get-AzureStorageBlob", true));
                agent.OutputValidation(new List<CloudBlob> { appendBlob });
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// download blob when container's public access level is container
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        public void GetBlobContentWithContainerPermission()
        {
            string containerName = Utility.GenNameString(ContainerPrefix);
            CloudBlobContainer container = blobUtil.CreateContainer(containerName, BlobContainerPublicAccessType.Container);

            try
            {
                DownloadBlobFromContainerTest(container);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// download blob when container's public access level is blob
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        public void GetBlobContentWithBlobPermission()
        {
            string containerName = Utility.GenNameString(ContainerPrefix);
            CloudBlobContainer container = blobUtil.CreateContainer(containerName, BlobContainerPublicAccessType.Blob);

            try
            {
                DownloadBlobFromContainerTest(container);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// download test in specified container
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        private void DownloadBlobFromContainerTest(CloudBlobContainer container)
        {
            DownloadBlobFromContainer(container, StorageBlob.BlobType.BlockBlob);
            DownloadBlobFromContainer(container, StorageBlob.BlobType.PageBlob);
            DownloadBlobFromContainer(container, StorageBlob.BlobType.AppendBlob);
        }

        /// <summary>
        /// download specified blob
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blob"></param>
        private void DownloadBlobFromContainer(CloudBlobContainer container, StorageBlob.BlobType type)
        {
            string blobName = Utility.GenNameString("blob");
            CloudBlob blob = blobUtil.CreateBlob(container, blobName, type);

            string filePath = Path.Combine(downloadDirRoot, blob.Name);
            Test.Assert(agent.GetAzureStorageBlobContent(blob.Name, filePath, container.Name, true), "download blob should be successful");
            string localMd5 = FileUtil.GetFileContentMD5(filePath);
            Test.Assert(localMd5 == blob.Properties.ContentMD5, string.Format("local content md5 should be {0}, and actually it's {1}", blob.Properties.ContentMD5, localMd5));
            agent.OutputValidation(new List<CloudBlob> { blob });
        }

        [TestMethod()]
        [TestCategory(Tag.BVT)]
        public void MakeSureBvtUsingAnonymousContext()
        {
            //TODO EnvKey is not empty since we called SaveAndCleanSubScriptionAndEnvConnectionString when initializing
            string key = System.Environment.GetEnvironmentVariable(CLICommonBVT.EnvKey);
            Test.Assert(string.IsNullOrEmpty(key), string.Format("env connection string {0} should be null or empty", key));
            Test.Assert(PowerShellAgent.Context != null, "PowerShell context should be not null when running bvt against Anonymous storage account");
        }

        /// <summary>
        /// Anonymous storage context should work with specified end point
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.BVT)]
        public void AnonymousContextWithEndPoint()
        {
            PowerShellAgent psAgent = (PowerShellAgent)agent;
            string containerName = Utility.GenNameString(ContainerPrefix);
            CloudBlobContainer container = blobUtil.CreateContainer(containerName, BlobContainerPublicAccessType.Blob);

            try
            {
                string pageBlobName = Utility.GenNameString("pageblob");
                string blockBlobName = Utility.GenNameString("blockblob");
                string appendBlobName = Utility.GenNameString("appendblob");
                CloudBlob blockBlob = blobUtil.CreateBlockBlob(container, blockBlobName);
                CloudBlob pageBlob = blobUtil.CreatePageBlob(container, pageBlobName);
                CloudBlob appendBlob = blobUtil.CreateAppendBlob(container, appendBlobName);

                string protocol = useHttps ? "https" : "http";

                psAgent.UseContextParam = false;
                string cmd = string.Format("New-AzureStorageContext -StorageAccountName {0} -Anonymous -Protocol {1} -EndPoint {2}",
                    StorageAccountName, protocol, StorageEndPoint);
                psAgent.AddPipelineScript(cmd);
                Test.Assert(agent.GetAzureStorageBlob(blockBlobName, containerName), Utility.GenComparisonData("Get-AzureStorageBlob", true));
                agent.OutputValidation(new List<CloudBlob> { blockBlob });
                psAgent.AddPipelineScript(cmd);
                Test.Assert(agent.GetAzureStorageBlob(pageBlobName, containerName), Utility.GenComparisonData("Get-AzureStorageBlob", true));
                agent.OutputValidation(new List<CloudBlob> { pageBlob });
                psAgent.AddPipelineScript(cmd);
                Test.Assert(agent.GetAzureStorageBlob(appendBlobName, containerName), Utility.GenComparisonData("Get-AzureStorageBlob", true));
                agent.OutputValidation(new List<CloudBlob> { appendBlob });
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Anonymous storage context should work with specified end point
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.BVT)]
        public void CopyFromPublicBlobToFile()
        {
            this.CopyFromPublicBlobToFile(StorageBlob.BlobType.AppendBlob);
            this.CopyFromPublicBlobToFile(StorageBlob.BlobType.BlockBlob);
            this.CopyFromPublicBlobToFile(StorageBlob.BlobType.PageBlob);
        }

        private void CopyFromPublicBlobToFile(StorageBlob.BlobType blobType)
        {
            PowerShellAgent psAgent = (PowerShellAgent)agent;
            string containerName = Utility.GenNameString(ContainerPrefix);
            CloudBlobContainer container = blobUtil.CreateContainer(containerName, BlobContainerPublicAccessType.Blob);

            string destShareName = Utility.GenNameString("share");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                CloudBlob blob = blobUtil.CreateRandomBlob(container, fileName, blobType);

                var file = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), fileName);

                Test.Assert(agent.StartFileCopy(blob.Uri.ToString(), destShareName, fileName, PowerShellAgent.Context),
                    "Start copying from public blob URI to file should succeed.");

                Test.Assert(agent.GetFileCopyState(file, true), "Get file copying state should succeed.");

                file.FetchAttributes();
                blob.FetchAttributes();

                Test.Assert(file.Metadata.SequenceEqual(blob.Metadata), "Destination's metadata should be the same with source's");
                Test.Assert(file.Properties.ContentMD5 == blob.Properties.ContentMD5, "MD5 should be the same.");
                Test.Assert(file.Properties.ContentType == blob.Properties.ContentType, "Content type should be the same.");
            }
            finally
            {
            }
        }

        public static void Initialize(TestContext testContext, bool useHttps)
        {
            StorageAccount = null;
            TestBase.TestClassInitialize(testContext);
            CLICommonBVT.SaveAndCleanSubScriptionAndEnvConnectionString();
            StorageAccountName = Test.Data.Get("StorageAccountName");
            StorageEndPoint = Test.Data.Get("StorageEndPoint").Trim();

            if (lang == Language.PowerShell)
            {
                PowerShellAgent.SetAnonymousStorageContext(StorageAccountName, useHttps, StorageEndPoint);
            }

            downloadDirRoot = Test.Data.Get("DownloadDir");
            SetupDownloadDir();
        }
    }
}
