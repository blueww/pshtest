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

using System.Collections;
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
using StorageBlob = Microsoft.WindowsAzure.Storage.Blob;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional tests for Set-ContainerAcl
    /// </summary>
    [TestClass]
    public class SetBlobContent : TestBase
    {
        private static string uploadDirRoot;
        private static List<string> files = new List<string>();

        //TODO upload a already opened read/write file
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
            uploadDirRoot = Test.Data.Get("UploadDir");
            SetupUploadDir();
        }

        [ClassCleanup()]
        public static void SetBlobContentClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// create upload dir and temp files
        /// </summary>
        private static void SetupUploadDir()
        {
            Test.Verbose("Create Upload dir {0}", uploadDirRoot);
            int minDirDepth = 1, maxDirDepth = 3;
            int dirDepth = random.Next(minDirDepth, maxDirDepth);

            FileUtil.CreateDirIfNotExits(uploadDirRoot);
            FileUtil.CleanDirectory(uploadDirRoot);

            Test.Info("Generate Temp files for upload blobs");
            files = FileUtil.GenerateTempFiles(uploadDirRoot, dirDepth);
            files.Sort();
        }

        /// <summary>
        /// set azure blob content by mutilple files
        /// 8.14	Set-AzureStorageBlobContent
        ///     3.	Upload a list of new blob files
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        public void SetBlobContentByMultipleFiles()
        {
            SetBlobContentByMultipleFiles(StorageBlob.BlobType.BlockBlob);
            SetBlobContentByMultipleFiles(StorageBlob.BlobType.PageBlob);
        }

        internal void SetBlobContentByMultipleFiles(StorageBlob.BlobType blobType)
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                List<IListBlobItem> blobLists = container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                Test.Assert(blobLists.Count == 0, string.Format("container {0} should contain {1} blobs, and actually it contain {2} blobs", containerName, 0, blobLists.Count));

                DirectoryInfo rootDir = new DirectoryInfo(uploadDirRoot);

                FileInfo[] rootFiles = rootDir.GetFiles();

                Test.Info("Upload files...");
                Test.Assert(agent.UploadLocalFiles(uploadDirRoot, containerName, blobType), "upload multiple files should be successful");
                Test.Info("Upload finished...");
                blobLists = container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                Test.Assert(blobLists.Count == rootFiles.Count(), string.Format("set-azurestorageblobcontent should upload {0} files, and actually it's {1}", rootFiles.Count(), blobLists.Count));

                ICloudBlob blob = null;
                for (int i = 0, count = rootFiles.Count(); i < count; i++)
                {
                    blob = blobLists[i] as ICloudBlob;

                    if (blob == null)
                    {
                        Test.AssertFail("blob can't be null");
                    }

                    Test.Assert(rootFiles[i].Name == blob.Name, string.Format("blob name should be {0}, and actully it's {1}", rootFiles[i].Name, blob.Name));
                    string localMd5 = FileUtil.GetFileContentMD5(Path.Combine(uploadDirRoot, rootFiles[i].Name));
                    Test.Assert(blob.BlobType == blobType, string.Format("blob type should be equal {0} = {1}", blob.BlobType, blobType));

                    if (blobType == BlobType.BlockBlob)
                    {
                        Test.Assert(localMd5 == blob.Properties.ContentMD5, string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, blob.Properties.ContentMD5));
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// upload files in subdirectory
        /// 8.14	Set-AzureStorageBlobContent positive functional cases.
        ///     4. Upload a block blob file and a page blob file with a subdirectory
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        public void SetBlobContentWithSubDirectory()
        {
            DirectoryInfo rootDir = new DirectoryInfo(uploadDirRoot);

            DirectoryInfo[] dirs = rootDir.GetDirectories();

            foreach (DirectoryInfo dir in dirs)
            {
                string containerName = Utility.GenNameString("container");
                CloudBlobContainer container = blobUtil.CreateContainer(containerName);

                try
                {
                    List<IListBlobItem> blobLists = container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                    Test.Assert(blobLists.Count == 0, string.Format("container {0} should contain {1} blobs, and actually it contain {2} blobs", containerName, 0, blobLists.Count));

                    StorageBlob.BlobType blobType = StorageBlob.BlobType.BlockBlob;

                    if (dir.Name.StartsWith("dirpage"))
                    {
                        blobType = Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob;
                    }

                    ((PowerShellAgent)agent).AddPipelineScript(string.Format("ls -File -Recurse -Path {0}", dir.FullName));
                    Test.Info("Upload files...");
                    Test.Assert(agent.SetAzureStorageBlobContent(string.Empty, containerName, blobType), "upload multiple files should be successsed");
                    Test.Info("Upload finished...");

                    blobLists = container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                    List<string> dirFiles = files.FindAll(item => item.StartsWith(dir.Name));
                    Test.Assert(blobLists.Count == dirFiles.Count(), string.Format("set-azurestorageblobcontent should upload {0} files, and actually it's {1}", dirFiles.Count(), blobLists.Count));

                    ICloudBlob blob = null;
                    for (int i = 0, count = dirFiles.Count(); i < count; i++)
                    {
                        blob = blobLists[i] as ICloudBlob;

                        if (blob == null)
                        {
                            Test.AssertFail("blob can't be null");
                        }

                        string convertedName = blobUtil.ConvertBlobNameToFileName(blob.Name, dir.Name);
                        Test.Assert(dirFiles[i] == convertedName, string.Format("blob name should be {0}, and actully it's {1}", dirFiles[i], convertedName));
                        string localMd5 = Helper.GetFileContentMD5(Path.Combine(uploadDirRoot, dirFiles[i]));
                        Test.Assert(blob.BlobType == blobType, "blob type should be block blob");

                        if (blobType == BlobType.BlockBlob)
                        {
                            Test.Assert(localMd5 == blob.Properties.ContentMD5, string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, blob.Properties.ContentMD5));
                        }
                    }
                }
                finally
                {
                    blobUtil.RemoveContainer(containerName);
                }
            }
        }

        /// <summary>
        /// set blob content with invalid bob name
        /// 8.14	Set-AzureStorageBlobContent negative functional cases
        ///     1. Upload a block blob file and a page blob file with a subdirectory
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void SetBlobContentWithInvalidBlobName()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                int MaxBlobNameLength = 1024;
                // need to change the invalid blob name as "One of the request inputs is out of range" for NodeJS
                string blobName = new string('a', MaxBlobNameLength + 1);

                List<IListBlobItem> blobLists = container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                Test.Assert(blobLists.Count == 0, string.Format("container {0} should contain {1} blobs, and actually it contain {2} blobs", containerName, 0, blobLists.Count));

                Test.Assert(!agent.SetAzureStorageBlobContent(Path.Combine(uploadDirRoot, files[0]), containerName, StorageBlob.BlobType.BlockBlob, blobName), "upload blob with invalid blob name should fail");

                agent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, blobName);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// set blob content with invalid blob type
        /// 8.14	Set-AzureStorageBlobContent negative functional cases
        ///     6.	Upload a blob file with the same name but with different BlobType
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void SetBlobContentWithInvalidBlobType()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                string blobName = files[0];

                List<IListBlobItem> blobLists = container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                Test.Assert(blobLists.Count == 0, string.Format("container {0} should contain {1} blobs, and actually it contain {2} blobs", containerName, 0, blobLists.Count));

                Test.Assert(agent.SetAzureStorageBlobContent(Path.Combine(uploadDirRoot, files[0]), containerName, StorageBlob.BlobType.BlockBlob, blobName), "upload blob should be successful.");
                blobLists = container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                Test.Assert(blobLists.Count == 1, string.Format("container {0} should contain {1} blobs, and actually it contain {2} blobs", containerName, 1, blobLists.Count));
                string convertBlobName = blobUtil.ConvertFileNameToBlobName(blobName);
                Test.Assert(((ICloudBlob)blobLists[0]).Name == convertBlobName, string.Format("blob name should be {0}, actually it's {1}", convertBlobName, ((ICloudBlob)blobLists[0]).Name));

                Test.Assert(!agent.SetAzureStorageBlobContent(Path.Combine(uploadDirRoot, files[0]), containerName, StorageBlob.BlobType.PageBlob, blobName), "upload blob should be with invalid blob should fail.");

                agent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, ((ICloudBlob)blobLists[0]).Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// upload page blob with invalid file size
        /// 8.14	Set-AzureStorageBlobContent negative functional cases
        ///     8.	Upload a page blob the size of which is not 512*n
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void SetPageBlobWithInvalidFileSize()
        {
            string fileName = Utility.GenNameString("tinypageblob");
            string filePath = Path.Combine(uploadDirRoot, fileName);
            int fileSize = 480;
            FileUtil.GenerateTinyFile(filePath, fileSize);
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                List<IListBlobItem> blobLists = container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                Test.Assert(blobLists.Count == 0, string.Format("container {0} should contain {1} blobs, and actually it contain {2} blobs", containerName, 0, blobLists.Count));
                Test.Assert(!agent.SetAzureStorageBlobContent(filePath, containerName, StorageBlob.BlobType.PageBlob), "upload page blob with invalid file size should fail.");

                agent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, fileSize.ToString());

                blobLists = container.ListBlobs(string.Empty, true, BlobListingDetails.All).ToList();
                Test.Assert(blobLists.Count == 0, string.Format("container {0} should contain {1} blobs, and actually it contain {2} blobs", containerName, 0, blobLists.Count));
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
                FileUtil.RemoveFile(filePath);
            }
        }

        /// <summary>
        /// Set blob content with blob properties
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void SetBlobContentWithProperties()
        {
            SetBlobContentWithProperties(StorageBlob.BlobType.BlockBlob);
            SetBlobContentWithProperties(StorageBlob.BlobType.PageBlob);
        }

        /// <summary>
        /// set blob content with blob meta data
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void SetBlobContentWithMetadata()
        {
            SetBlobContentWithMetadata(StorageBlob.BlobType.BlockBlob);
            SetBlobContentWithMetadata(StorageBlob.BlobType.PageBlob);
        }

        /// <summary>
        /// set blob content without force
        /// </summary>
        ////[TestMethod()]
        ////[TestCategory(Tag.Function)]
        ////[TestCategory(PsTag.Blob)]
        ////[TestCategory(PsTag.SetBlobContent)]
        public void SetBlobContentForEixstsBlobWithoutForce()
        {
            string filePath = FileUtil.GenerateOneTempTestFile();
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = Utility.GenNameString("blob");
            ICloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                string previousMd5 = blob.Properties.ContentMD5;
                Test.Assert(!agent.SetAzureStorageBlobContent(filePath, container.Name, blob.BlobType, blob.Name, false), "set blob content without force parameter should fail");
                ExpectedContainErrorMessage(ConfirmExceptionMessage);
                blob.FetchAttributes();

                ExpectEqual(previousMd5, blob.Properties.ContentMD5, "content md5");
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                FileUtil.RemoveFile(filePath);
            }
        }

        /// <summary>
        /// set blob content with force
        /// 8.9	Blob upload
        ///     7.	Upload a blob file that already exists
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void SetBlobContentForEixstsBlobWithForce()
        {
            string filePath = FileUtil.GenerateOneTempTestFile();
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = Utility.GenNameString("blob");
            ICloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                string localMD5 = FileUtil.GetFileContentMD5(filePath);
                Test.Assert(agent.SetAzureStorageBlobContent(filePath, container.Name, blob.BlobType, blob.Name, true), "set blob content with force parameter should succeed");
                blob = CloudBlobUtil.GetBlob(container, blobName, blob.BlobType);
                blob.FetchAttributes();

                ExpectEqual(localMD5, blob.Properties.ContentMD5, "content md5");
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                FileUtil.RemoveFile(filePath);
            }
        }

        /// <summary>
        /// 8.9	Blob upload
        ///     8.	Upload a blob which is 0 size
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadBlobWithZeroSize()
        {
            UploadBlobWithZeroSize(BlobType.BlockBlob);
            UploadBlobWithZeroSize(BlobType.PageBlob);
        }

        public void UploadBlobWithZeroSize(StorageBlob.BlobType blobType)
        {
            string filePath = FileUtil.GenerateOneTempTestFile(0, 0);
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = Utility.GenNameString("blob");

            try
            {
                string localMD5 = FileUtil.GetFileContentMD5(filePath);
                Test.Assert(agent.SetAzureStorageBlobContent(filePath, container.Name, blobType, blobName, true), "upload blob with zero size should succeed");
                ICloudBlob blob = CloudBlobUtil.GetBlob(container, blobName, blobType);
                blob.FetchAttributes();

                ExpectEqual(localMD5, blob.Properties.ContentMD5, "content md5");
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                FileUtil.RemoveFile(filePath);
            }
        }

        /// <summary>
        /// 8.9	Blob upload
        ///     8.	Upload a blob name with special chars
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadBlobWithSpeicialChars()
        {
            UploadBlobWithSpeicialChars(BlobType.BlockBlob);
            UploadBlobWithSpeicialChars(BlobType.PageBlob);
        }

        public void UploadBlobWithSpeicialChars(StorageBlob.BlobType blobType)
        {
            string filePath = FileUtil.GenerateOneTempTestFile();
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = SpecialChars;

            try
            {
                string localMD5 = FileUtil.GetFileContentMD5(filePath);
                Test.Assert(agent.SetAzureStorageBlobContent(filePath, container.Name, blobType, blobName, true), "upload a blob name with special chars should succeed");
                ICloudBlob blob = CloudBlobUtil.GetBlob(container, blobName, blobType);
                blob.FetchAttributes();

                ExpectEqual(localMD5, blob.Properties.ContentMD5, "content md5");
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                FileUtil.RemoveFile(filePath);
            }
        }

        public void SetBlobContentWithProperties(StorageBlob.BlobType blobType)
        {
            string filePath = FileUtil.GenerateOneTempTestFile();
            CloudBlobContainer container = blobUtil.CreateContainer();
            Hashtable properties = new Hashtable();
            properties.Add("CacheControl", Utility.GenNameString(string.Empty));
            properties.Add("ContentEncoding", Utility.GenNameString(string.Empty));
            properties.Add("ContentLanguage", Utility.GenNameString(string.Empty));
            properties.Add("ContentType", Utility.GenNameString(string.Empty));

            try
            {
                Test.Assert(agent.SetAzureStorageBlobContent(filePath, container.Name, blobType, string.Empty, true, -1, properties), "set blob content with property should succeed");
                ICloudBlob blob = container.GetBlobReferenceFromServer(Path.GetFileName(filePath));
                blob.FetchAttributes();
                ExpectEqual(properties["CacheControl"].ToString(), blob.Properties.CacheControl, "Cache control");
                ExpectEqual(properties["ContentEncoding"].ToString(), blob.Properties.ContentEncoding, "Content Encoding");
                ExpectEqual(properties["ContentLanguage"].ToString(), blob.Properties.ContentLanguage, "Content Language");
                ExpectEqual(properties["ContentType"].ToString(), blob.Properties.ContentType, "Content Type");
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                FileUtil.RemoveFile(filePath);
            }
        }

        public void SetBlobContentWithMetadata(StorageBlob.BlobType blobType)
        {
            string filePath = FileUtil.GenerateOneTempTestFile();
            CloudBlobContainer container = blobUtil.CreateContainer();
            Hashtable metadata = new Hashtable();
            int metaCount = Utility.GetRandomTestCount();

            for (int i = 0; i < metaCount; i++)
            {
                string key = Utility.GenRandomAlphabetString();
                string value = Utility.GenNameString(string.Empty);

                if (!metadata.ContainsKey(key))
                {
                    Test.Info(string.Format("Add meta key: {0} value : {1}", key, value));
                    metadata.Add(key, value);
                }
            }

            try
            {
                Test.Assert(agent.SetAzureStorageBlobContent(filePath, container.Name, blobType, string.Empty, true, -1, null, metadata), "set blob content with meta should succeed");
                ICloudBlob blob = container.GetBlobReferenceFromServer(Path.GetFileName(filePath));
                blob.FetchAttributes();
                ExpectEqual(metadata.Count, blob.Metadata.Count, "meta data count");

                foreach (string key in metadata.Keys)
                {
                    if (blob.Metadata.ContainsKey(key))
                    {
                        ExpectEqual(metadata[key].ToString(), blob.Metadata[key], "Meta data key " + key);
                    }
                    else if (blob.Metadata.ContainsKey(key.ToLower()))
                    {
                        // NodeJS stores key in lower case
                        ExpectEqual(metadata[key].ToString().ToLower(), blob.Metadata[key.ToLower()], "Meta data key " + key);
                    }
                    else
                    {
                        Test.AssertFail("Could not find meta data key " + key + " in blob entity");
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
                FileUtil.RemoveFile(filePath);
            }
        }
    }
}
