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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using BlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional tests for Set-ContainerAcl
    /// </summary>
    [TestClass]
    public class GetBlobContent: TestBase
    {
        //TODO add invalid md5sum for page blob
        private static string downloadDirRoot;

        private string ContainerName = string.Empty;
        private string BlobName = string.Empty;
        private CloudBlob Blob = null;
        private CloudBlobContainer Container = null;

        private static CloudBlobHelper CommonBlobHelper;

        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
            downloadDirRoot = Test.Data.Get("DownloadDir");
            SetupDownloadDir();

            CommonBlobHelper = new CloudBlobHelper(StorageAccount);  
        }

        [ClassCleanup()]
        public static void GetBlobContentClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        public override void OnTestSetup()
        {
            FileUtil.CleanDirectory(downloadDirRoot);
        }

        /// <summary>
        /// create download dir
        /// </summary>
        private static void SetupDownloadDir()
        {
            FileUtil.CreateDirIfNotExits(downloadDirRoot);
            FileUtil.CleanDirectory(downloadDirRoot);
        }

        /// <summary>
        /// create a random container with a random blob
        /// </summary>
        private void SetupTestContainerAndBlob()
        {
            string fileName = Utility.GenNameString("download");
            string filePath = Path.Combine(downloadDirRoot, fileName);
            int minFileSize = 1;
            int maxFileSize = 5;
            int fileSize = random.Next(minFileSize, maxFileSize);
            string md5sum = Helper.GenerateRandomTestFile(filePath, fileSize);

            ContainerName = Utility.GenNameString("container");
            BlobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(ContainerName);
            CloudBlob blobRef = blobUtil.GetRandomBlobReference(container, BlobName);

            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = System.IO.File.OpenRead(filePath))
            {
                blobRef.UploadFromStream(fileStream);
            }
            
            blobRef.FetchAttributes();

            if (null == blobRef.Properties.ContentMD5)
            {
                blobRef.Properties.ContentMD5 = md5sum;
                blobRef.SetProperties();
            }

            File.Delete(filePath);
            Blob = blobRef;
            Container = container;
        }

        /// <summary>
        /// clean test container and blob
        /// </summary>
        private void CleanupTestContainerAndBlob()
        {
            blobUtil.RemoveContainer(ContainerName);
            FileUtil.CleanDirectory(downloadDirRoot);
            ContainerName = string.Empty;
            BlobName = string.Empty;
            Blob = null;
            Container = null;
        }

        /// <summary>
        /// get blob content by container name and blob name
        /// 8.15	Get-AzureStorageBlobContent positive function cases
        ///     3.	Download an existing blob file using the container name specified by the param
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetBlobContentByName()
        {
            SetupTestContainerAndBlob();

            try
            {
                string destFileName = Utility.GenNameString("download");
                string destFilePath = Path.Combine(downloadDirRoot, destFileName);
                Test.Assert(agent.GetAzureStorageBlobContent(BlobName, destFilePath, ContainerName, true), "download blob should be successful");
                string localMd5 = FileUtil.GetFileContentMD5(destFilePath);
                Test.Assert(localMd5 == Blob.Properties.ContentMD5, string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, Blob.Properties.ContentMD5));
            }
            finally
            {
                CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// get blob content by container pipeline
        /// 8.15	Get-AzureStorageBlobContent positive function cases
        ///     4.	Download an existing blob file using the container object retrieved by Get-AzureContainer
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobContent)]
        public void GetBlobContentByContainerPipeline()
        {
            SetupTestContainerAndBlob();

            try
            {
                string destFileName = Utility.GenNameString("download");
                string destFilePath = Path.Combine(downloadDirRoot, destFileName);

                ((PowerShellAgent)agent).AddPipelineScript(string.Format("Get-AzureStorageContainer {0}", ContainerName));
                Test.Assert(agent.GetAzureStorageBlobContent(BlobName, destFilePath, string.Empty, true), "download blob should be successful");
                string localMd5 = FileUtil.GetFileContentMD5(destFilePath);
                Test.Assert(localMd5 == Blob.Properties.ContentMD5, string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, Blob.Properties.ContentMD5));
            }
            finally
            {
                CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// get blob content by container pipeline
        /// 8.15	Get-AzureStorageBlobContent positive function cases
        ///     5.	Download a block blob file and a page blob file with a subdirectory
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetBlobContentInSubDirectory()
        {
            string ContainerName = Utility.GenNameString("container");
            FileUtil.CleanDirectory(downloadDirRoot);
            List<string> files = FileUtil.GenerateTempFiles(downloadDirRoot, 2);
            files.Sort();

            CloudBlobContainer Container = blobUtil.CreateContainer(ContainerName);

            try
            {
                foreach (string file in files)
                {
                    string filePath = Path.Combine(downloadDirRoot, file);
                    string blobName = string.Empty;
                    using (var fileStream = System.IO.File.OpenRead(filePath))
                    {
                        blobName = file;
                        CloudBlockBlob blockBlob = Container.GetBlockBlobReference(blobName);
                        blockBlob.UploadFromStream(fileStream);
                    }
                }

                List<IListBlobItem> blobLists = Container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                Test.Assert(blobLists.Count == files.Count, string.Format("container {0} should contain {1} blobs, and actually it contain {2} blobs", ContainerName, files.Count, blobLists.Count));

                FileUtil.CleanDirectory(downloadDirRoot);

                Test.Assert(agent.DownloadBlobFiles(downloadDirRoot, ContainerName, true), "download blob should be successful");
                Test.Assert(agent.Output.Count == files.Count, "Get-AzureStroageBlobContent should download {0} blobs, and actually it's {1}", files.Count, agent.Output.Count);

                for (int i = 0, count = files.Count(); i < count; i++)
                {
                    string path = Path.Combine(downloadDirRoot, files[i]);
                    CloudBlob blob = blobLists[i] as CloudBlob;
                    if (!File.Exists(path))
                    {
                        Test.AssertFail(string.Format("local file '{0}' doesn't exist.", path));
                    }

                    string localMd5 = FileUtil.GetFileContentMD5(path);
                    string convertedName = blobUtil.ConvertBlobNameToFileName(blob.Name, string.Empty);
                    Test.Assert(files[i] == convertedName, string.Format("converted blob name should be {0}, actually it's {1}", files[i], convertedName));
                    Test.Assert(localMd5 == blob.Properties.ContentMD5, string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, blob.Properties.ContentMD5));
                }
            }
            finally
            {
                FileUtil.CleanDirectory(downloadDirRoot);
                blobUtil.RemoveContainer(ContainerName);
            }
        }

        /// <summary>
        /// get blob content by container pipeline
        /// 8.15 Get-AzureStorageBlobContent positive function cases
        ///    6. Validate that all the blob snapshots can be downloaded
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobContent)]
        public void GetBlobContentFromSnapshot()
        {
            SetupTestContainerAndBlob();

            try
            {
                List<CloudBlob> blobs = new List<CloudBlob>();
                int minSnapshot = 1;
                int maxSnapshot = 5;
                int snapshotCount = random.Next(minSnapshot, maxSnapshot);

                for (int i = 0; i < snapshotCount; i++)
                {
                    CloudBlob blob = Blob.Snapshot();
                    blobs.Add(blob);
                }

                blobs.Add(Blob);

                List<IListBlobItem> blobLists = Container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                Test.Assert(blobLists.Count == blobs.Count, string.Format("container {0} should contain {1} blobs, and actually it contain {2} blobs", ContainerName, blobs.Count, blobLists.Count));

                FileUtil.CleanDirectory(downloadDirRoot);

                ((PowerShellAgent)agent).AddPipelineScript(string.Format("Get-AzureStorageContainer {0}", ContainerName));
                ((PowerShellAgent)agent).AddPipelineScript("Get-AzureStorageBlob");
                Test.Assert(agent.GetAzureStorageBlobContent(string.Empty, downloadDirRoot, string.Empty, true), "download blob should be successful");
                Test.Assert(agent.Output.Count == blobs.Count, "Get-AzureStroageBlobContent should download {0} blobs, and actully it's {1}", blobs.Count, agent.Output.Count);

                for (int i = 0, count = blobs.Count(); i < count; i++)
                {
                    CloudBlob blob = blobLists[i] as CloudBlob;
                    string path = Path.Combine(downloadDirRoot, blobUtil.ConvertBlobNameToFileName(blob.Name, string.Empty, blob.SnapshotTime));

                    Test.Assert(File.Exists(path), string.Format("local file '{0}' should exists after downloading.", path));

                    string localMd5 = FileUtil.GetFileContentMD5(path);
                    string convertedName = blobUtil.ConvertBlobNameToFileName(blob.Name, string.Empty);
                    Test.Assert(localMd5 == blob.Properties.ContentMD5, string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, blob.Properties.ContentMD5));
                }
            }
            finally
            {
                FileUtil.CleanDirectory(downloadDirRoot);
                CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// download a not existing blob
        /// </summary>
        /// 8.15 Get-AzureStorageBlobContent negative function cases
        ///    1.	Download a non-existing blob file
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetBlobContentWithNotExistsBlob()
        {
            SetupTestContainerAndBlob();

            try
            {
                string notExistingBlobName = Utility.GenNameString("notexisting");
                DirectoryInfo dir = new DirectoryInfo(downloadDirRoot);
                int filesCountBeforeDowloading = dir.GetFiles().Count();
                string downloadFileName = Path.Combine(downloadDirRoot, Utility.GenNameString("download"));
                Test.Assert(!agent.GetAzureStorageBlobContent(notExistingBlobName, downloadFileName, ContainerName, true), "download not existing blob should fail");
                agent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, notExistingBlobName, ContainerName);
                int filesCountAfterDowloading = dir.GetFiles().Count();
                Test.Assert(filesCountBeforeDowloading == filesCountAfterDowloading, "the files count should be equal after a failed downloading");
            }
            finally
            {
                CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// download a not existing blob
        /// </summary>
        /// 8.15 Get-AzureStorageBlobContent negative function cases
        ///    3. Download a blob file with an invalid container name or container object
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetBlobContentWithNotExistsContainer()
        {
            string containerName = Utility.GenNameString("notexistingcontainer");
            string blobName = Utility.GenNameString("blob");
            DirectoryInfo dir = new DirectoryInfo(downloadDirRoot);
            int filesCountBeforeDowloading = dir.GetFiles().Count();
            string downloadFileName = Path.Combine(downloadDirRoot, Utility.GenNameString("download"));
            Test.Assert(!agent.GetAzureStorageBlobContent(blobName, downloadFileName, containerName, true), "download blob from not existing container should fail");

            try
            {
                agent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, blobName, containerName);

                int filesCountAfterDowloading = dir.GetFiles().Count();
                Test.Assert(filesCountBeforeDowloading == filesCountAfterDowloading, "the files count should be equal after download failure");
            }
            finally
            {
                FileUtil.CleanDirectory(downloadDirRoot);
            }
        }

        /// <summary>
        /// 8.9	Blob download
        ///     8.	Download a blob name with special chars
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadBlobWithSpecialChars()
        {
            DownloadBlobWithSpecialChars(BlobType.BlockBlob);
            DownloadBlobWithSpecialChars(BlobType.PageBlob);
            DownloadBlobWithSpecialChars(BlobType.AppendBlob);
        }

        public void DownloadBlobWithSpecialChars(BlobType blobType)
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = SpecialChars;
            CloudBlob blob = blobUtil.CreateBlob(container, blobName, blobType);
            
            string downloadFileName = Path.Combine(downloadDirRoot, Utility.GenNameString("download"));

            try
            {
                Test.Assert(agent.GetAzureStorageBlobContent(blobName, downloadFileName, container.Name, true), "download blob name with special chars should succeed");
                blob.FetchAttributes();
                string downloadedMD5 = FileUtil.GetFileContentMD5(downloadFileName);
                ExpectEqual(downloadedMD5, blob.Properties.ContentMD5, "content md5");
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                FileUtil.RemoveFile(downloadFileName);
            }
        }

        /// <summary>
        /// 8.9	Blob download
        ///     8.	Download a blob which is 0 size
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadBlobWithZeroSize()
        {
            DownloadBlobWithZeroSize(BlobType.BlockBlob);
            DownloadBlobWithZeroSize(BlobType.PageBlob);
            DownloadBlobWithZeroSize(BlobType.AppendBlob);
        }

        public void DownloadBlobWithZeroSize(BlobType blobType)
        {
            CloudBlobContainer container = blobUtil.CreateContainer();        
            string blobName = Utility.GenNameString("blob");
            string downloadFileName = Path.Combine(downloadDirRoot, Utility.GenNameString("download"));

            try
            {
                CloudBlob blob = blobUtil.CreateBlob(container, blobName, blobType);
                Test.Assert(agent.GetAzureStorageBlobContent(blobName, downloadFileName, container.Name, true), "download blob with zero size should succeed");
                string downloadedMD5 = FileUtil.GetFileContentMD5(downloadFileName);

                ExpectEqual(downloadedMD5, blob.Properties.ContentMD5, "content md5");
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                FileUtil.RemoveFile(downloadFileName);
            }
        }

        /// <summary>
        /// 8.9	Blob download
        ///     8.	Download a page blob with many page ranges
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadPageBlobWithManyRanges()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = Utility.GenNameString("blob");
            string downloadFileName = Path.Combine(downloadDirRoot, Utility.GenNameString("download"));
            string tmpFileName = Path.Combine(downloadDirRoot, Utility.GenNameString("download"));

            try
            {
                // create a random blob size from 148M to 200M
                int blobSize = 1024 * 1024 * Utility.GetRandomTestCount(148, 200);
                blobUtil.CreatePageBlobWithManySmallRanges(container.Name, blobName, blobSize);

                // download the page blob by CLI agent
                Test.Assert(agent.GetAzureStorageBlobContent(blobName, downloadFileName, container.Name, true), "download page blob should succeed");
                string downloadedMD5 = FileUtil.GetFileContentMD5(downloadFileName);

                Test.Info("using xscl to download the page blob in order to check the MD5 value");
                CommonBlobHelper.DownloadFile(container.Name, blobName, tmpFileName);
                string previousMD5 = Helper.GetFileContentMD5(tmpFileName);

                ExpectEqual(previousMD5, downloadedMD5, "content md5");
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                File.Delete(tmpFileName);
                FileUtil.RemoveFile(downloadFileName);
            }
        }

        /// <summary>
        /// This is to validate CheckMd5 option in the cmdlet.
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        public void DownloadBlobCheckMD5()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = Utility.GenNameString("blob");
            string downloadFileName = Path.Combine(downloadDirRoot, Utility.GenNameString("download"));

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                string previousMD5 = blob.Properties.ContentMD5;

                // download blob and check MD5.
                Test.Assert(agent.GetAzureStorageBlobContent(blobName, downloadFileName, container.Name, true, CheckMd5:true), "download blob with CheckMd5 should succeed.");

                string downloadedMD5 = FileUtil.GetFileContentMD5(downloadFileName);
                ExpectEqual(previousMD5, downloadedMD5, "content md5");

                blob.Properties.ContentMD5 = "";
                blob.SetProperties();

                // Blob's ContentMD5 property is empty, download file and check MD5.
                Test.Assert(!agent.GetAzureStorageBlobContent(blobName, downloadFileName, container.Name, true, CheckMd5: true), "It should fail to download blob whose Content-MD5 property is incorrect with CheckMd5");
                ExpectedContainErrorMessage("The MD5 hash calculated from the downloaded data does not match the MD5 hash stored in the property of source");

                downloadedMD5 = FileUtil.GetFileContentMD5(downloadFileName);
                ExpectEqual(previousMD5, downloadedMD5, "content md5");

                // Blob's ContentMD5 property is empty, download file without check MD5
                Test.Assert(agent.GetAzureStorageBlobContent(blobName, downloadFileName, container.Name, true, CheckMd5: false), "It should suceed to download blob whose Content-MD5 property is incorrect without CheckMd5");

                downloadedMD5 = FileUtil.GetFileContentMD5(downloadFileName);
                ExpectEqual(previousMD5, downloadedMD5, "content md5");
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                FileUtil.RemoveFile(downloadFileName);
            }
        }
    }
}
