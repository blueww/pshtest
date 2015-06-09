using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
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
        }

        void CopyFromBlob(CloudBlob blob, string destFilePath, bool destExist = false)
        {
            string destShareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                string actualDestPath = destFilePath ?? fileUtil.ResolveFileName(blob);

                var destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), actualDestPath);

                if (destExist)
                {
                    fileUtil.CreateFile(share, actualDestPath);
                }

                if (blob.IsSnapshot)
                {
                    Test.Assert(agent.StartFileCopyFromBlob(blob, destShareName, destFilePath, PowerShellAgent.Context),
                        "Copy from blob to file shoule succeed.");

                    Test.Assert(agent.GetFileCopyState(destShareName, actualDestPath), "Get file copy state should succeed");                     
                }
                else
                {
                    Test.Assert(agent.StartFileCopyFromBlob(blob.Container.Name, blob.Name, destShareName, destFilePath, PowerShellAgent.Context),
                        "Copy from blob to file should succeed.");

                    Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed");
                }

                destFile.FetchAttributes();
                blob.FetchAttributes();

                Test.Assert(destFile.Metadata.SequenceEqual(blob.Metadata), "Destination's metadata should be the same with source's");
                Test.Assert(destFile.Properties.ContentMD5 == blob.Properties.ContentMD5, "MD5 should be the same.");
                Test.Assert(destFile.Properties.ContentType == blob.Properties.ContentType, "Content type should be the same.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
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
