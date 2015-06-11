using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using StorageFile = Microsoft.WindowsAzure.Storage.File;
using MS.Test.Common.MsTestLib;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    [TestClass]
    public class FileAsyncCopyTest : TestBase
    {
        [ClassInitialize]
        public static void FileAsyncCopyTestInitialize(TestContext context)
        {
            TestBase.TestClassInitialize(context);
        }

        [ClassCleanup]
        public static void FileAsyncCopyTestCleanup()
        {
            TestBase.TestClassCleanup();
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyToExistFile()
        {
            string filePath = Utility.GenNameString("folder") + "/" + Utility.GenNameString("folder") + "/" + Utility.GenNameString("fileName");

            CopyFromBlob(null, filePath, null, true);
            CopyFromFile(filePath, null, true);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromRootContainer()
        { 
            CopyFromBlob("$root", null, null);
            string filePath = Utility.GenNameString("folder") + "/" + Utility.GenNameString("folder") + "/" + Utility.GenNameString("fileName");
            CopyFromBlob("$root", filePath, null);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromBlobSnapshot()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = Utility.GenNameString("fileName");
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
            var blobSnapshot = blob.Snapshot();

            CopyFromBlob(blobSnapshot, null);
            CopyToFile(blobSnapshot);
            CopyFromBlobWithUri(blobSnapshot);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromBlobWithLongName()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("fileName", 1016);

            this.CopyFromBlob(containerName, blobName, null);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromBlobWithSpecialChar()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("\"\\:|<>*?");

            this.CopyFromBlob(containerName, blobName, null);
            this.CopyFromBlob(containerName, blobName, null, true);
        }

        private void CopyToFile(CloudBlob srcBlob, string destShareName, string destFilePath, Action copyAction, bool destExist = false)
        {
            CloudFileShare share = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                var destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), destFilePath);

                if (destExist)
                {
                    fileUtil.CreateFile(share.GetRootDirectoryReference(), destFilePath);
                }

                copyAction();

                destFile.FetchAttributes();
                srcBlob.FetchAttributes();

                Test.Assert(destFile.Metadata.SequenceEqual(srcBlob.Metadata), "Destination's metadata should be the same with source's");
                Test.Assert(destFile.Properties.ContentMD5 == srcBlob.Properties.ContentMD5, "MD5 should be the same.");
                Test.Assert(destFile.Properties.ContentType == srcBlob.Properties.ContentType, "Content type should be the same.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        void CopyFromBlobWithUri(CloudBlob blob)
        {
            Test.Info("Copying from blob URI to dest share name and dest file path.");

            string destShareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.GetShareReference(destShareName);

            string destPath = Utility.GenNameString("filename");
            var destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), destPath);

            CopyToFile(blob, destShareName, destPath,
                () =>
                {
                    string blobUri = agent.GetBlobSasFromCmd(blob, null, "r", null, DateTime.UtcNow.AddHours(1), true);
                    Test.Assert(agent.StartFileCopy(blobUri, destShareName, destPath, PowerShellAgent.Context), "Copy from blob to file should succeed.");

                    Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed");
                });
        }

        void CopyFromBlobToFileWithUri(CloudBlob blob)
        {
            Test.Info("Copying from blob URI to dest file instance.");

            string destShareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.GetShareReference(destShareName);

            string destPath = Utility.GenNameString("filename");
            var destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), destPath);

            CopyToFile(blob, destShareName, destPath,
                () =>
                {
                    string blobUri = agent.GetBlobSasFromCmd(blob, null, "r", null, DateTime.UtcNow.AddHours(1), true);
                    Test.Assert(agent.StartFileCopy(blobUri, destFile), "Copy from blob to file should succeed.");

                    Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed");
                });
        }

        void CopyToFile(CloudBlob blob)
        {
            string destShareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.GetShareReference(destShareName);

            string destPath = Utility.GenNameString("filename");
            var destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), destPath);

            CopyToFile(blob, destShareName, destPath,
                ()=>
                {
                    Test.Assert(agent.StartFileCopy(blob, destFile, PowerShellAgent.Context),
                        "Copy from blob to file should succeed.");

                    Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed");
                });
        }

        void CopyFromBlob(CloudBlob blob, string destFilePath, bool destExist = false)
        {
            string destShareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.GetShareReference(destShareName);

            string actualDestPath = destFilePath ?? fileUtil.ResolveFileName(blob);
            var destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), actualDestPath);

            this.CopyToFile(blob, destShareName, actualDestPath,
                () =>
                {

                    if (blob.IsSnapshot)
                    {
                        Test.Assert(agent.StartFileCopy(blob, destShareName, destFilePath, PowerShellAgent.Context),
                            "Copy from blob to file shoule succeed.");

                        Test.Assert(agent.GetFileCopyState(destShareName, actualDestPath), "Get file copy state should succeed");
                    }
                    else
                    {
                        Test.Assert(agent.StartFileCopyFromBlob(blob.Container.Name, blob.Name, destShareName, destFilePath, PowerShellAgent.Context),
                            "Copy from blob to file should succeed.");

                        Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed");
                    }
                }, destExist);
        }

        void CopyFromBlob(string containerName, string filePath, string destFilePath, bool destExist = false)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                containerName = Utility.GenNameString("container");
            }

            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Utility.GenNameString("file");
                }

                CloudBlob blob = blobUtil.CreateRandomBlob(container, filePath);

                CopyFromBlob(blob, destFilePath, destExist);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        void CopyFromFile(string filePath, string destFilePath, bool destExist = false)
        {
            string sourceShareName = Utility.GenNameString("share");
            CloudFileShare sourceShare = fileUtil.EnsureFileShareExists(sourceShareName);

            string destShareName = Utility.GenNameString("share");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Utility.GenNameString("file");
                }

                var sourceFile = fileUtil.CreateFile(sourceShare, destFilePath ?? filePath);

                var destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), filePath);

                if (destExist)
                {
                    fileUtil.CreateFile(destShare, destFilePath ?? filePath);
                }

                Test.Assert(agent.StartFileCopyFromFile(sourceShareName, filePath, destShareName, destFilePath, PowerShellAgent.Context),
                    "Copy from file to overwrite an existig file should succeed.");

                destFile.FetchAttributes();
                sourceFile.FetchAttributes();

                Test.Assert(destFile.Metadata.SequenceEqual(sourceFile.Metadata), "Destination's metadata should be the same with source's");
                Test.Assert(destFile.Properties.ContentMD5 == sourceFile.Properties.ContentMD5, "MD5 should be the same.");
                Test.Assert(destFile.Properties.ContentType == sourceFile.Properties.ContentType, "Content type should be the same.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(sourceShareName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }
    }
}
