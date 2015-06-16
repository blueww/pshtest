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
using Microsoft.WindowsAzure.Storage;
using Management.Storage.ScenarioTest.Util;

namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    [TestClass]
    public class FileAsyncCopyTest : TestBase
    {
        private static object SecondaryContext = null;
        private static CloudStorageAccount SecondaryAccount = null;
        private static CloudFileUtil FileUtil2 = null;

        [ClassInitialize]
        public static void FileAsyncCopyTestInitialize(TestContext context)
        {
            TestBase.TestClassInitialize(context);
            CloudStorageAccount SecondaryAccount = TestBase.GetCloudStorageAccountFromConfig("Secondary");
            SecondaryContext = PowerShellAgent.GetStorageContext(SecondaryAccount.ToString(true));
            FileUtil2 = new CloudFileUtil(SecondaryAccount);
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

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromBlobCrossAccount()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                string blobName = Utility.GenNameString("append");
                CloudBlob blob = blobUtil.CreateAppendBlob(container, blobName);

                this.CopyToFile(blob, true);

                blobName = Utility.GenNameString("block");
                blob = blobUtil.CreateBlockBlob(container, blobName);

                this.CopyToFile(blob, true);

                blobName = Utility.GenNameString("page");
                blob = blobUtil.CreatePageBlob(container, blobName);

                this.CopyFromBlob(blob, Utility.GenNameString("fileName"), false, true);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromFileCrossAccount()
        {
            this.CopyFromFile(Utility.GenNameString("sourcefile"), Utility.GenNameString("destfile"), false, true);

            string shareName = Utility.GenNameString("share");
            CloudFileShare srcShare = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string srcFileName = Utility.GenNameString("sourcefile");
                StorageFile.CloudFile srcFile = fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), srcFileName);
                string destShareName = Utility.GenNameString("destshare");
                string destFilePath = Utility.GenNameString("destFilePath");

                CloudFileShare destShare = FileUtil2.GetShareReference(destShareName);
                var destFile = FileUtil2.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath);

                this.CopyToFile(srcFile, destShareName, destFilePath,
                    () =>
                    {
                        Test.Assert(agent.StartFileCopy(srcFile, destShareName, destFilePath, SecondaryContext), "Start copy from file to file should succeed.");

                        Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed.");
                    }, false, true);

                srcFileName = Utility.GenNameString("sourcefile");
                srcFile = fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), srcFileName);
                destShareName = Utility.GenNameString("destshare");
                destFilePath = Utility.GenNameString("destFilePath");

                destShare = FileUtil2.GetShareReference(destShareName);
                destFile = FileUtil2.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath);

                this.CopyToFile(srcFile, destShareName, destFilePath,
                    () =>
                    {
                        Test.Assert(agent.StartFileCopy(srcFile, destFile), "Start copy from file to file should succeed.");

                        Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed.");
                    }, false, true);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromFileInDeepestDir()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare srcShare = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string srcFileName = this.GetDeepestFilePath();
                StorageFile.CloudFile srcFile = fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), srcFileName);

                string destShareName = Utility.GenNameString("destshare");
                string destFilePath = Utility.GenNameString("destFilePath");

                CloudFileShare destShare = fileUtil.GetShareReference(destShareName);
                var destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath);

                this.CopyToFile(srcFile, destShareName, destFilePath,
                    () =>
                    {
                        Test.Assert(agent.StartFileCopy(srcFile.Parent, srcFile.Name, destShareName, destFilePath, PowerShellAgent.Context), "Start copy from file to file should succeed.");

                        Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed.");
                    }, false, true);

                destShareName = Utility.GenNameString("destshare");
                destFilePath = Utility.GenNameString("destFilePath");

                destShare = fileUtil.GetShareReference(destShareName);
                destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath);

                this.CopyToFile(srcFile, destShareName, destFilePath,
                    () =>
                    {
                        Test.Assert(agent.StartFileCopyFromFile(shareName, srcFileName, destShareName, destFilePath, PowerShellAgent.Context), "Start copy from file to file should succeed.");

                        Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed.");
                    }, false, true);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyToFileInDeepestDir()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare srcShare = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string srcFileName = Utility.GenNameString("sourceFileName");
                StorageFile.CloudFile srcFile = fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), srcFileName);

                string destShareName = Utility.GenNameString("destshare");

                string destFilePath = this.GetDeepestFilePath();

                CloudFileShare destShare = fileUtil.GetShareReference(destShareName);
                var destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath);

                this.CopyToFile(srcFile, destShareName, destFilePath,
                    () =>
                    {
                        Test.Assert(agent.StartFileCopy(srcFile, destShareName, destFilePath, PowerShellAgent.Context), "Start copy from file to file should succeed.");

                        Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed.");
                    }, false, true);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromContainer()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            fileUtil.CleanupDirectory(share.GetRootDirectoryReference());

            try
            {
                var blobs = blobUtil.CreateRandomBlob(container);

                PowerShellAgent psAgent = agent as PowerShellAgent;

                psAgent.AddPipelineScript(string.Format("Get-AzureStorageBlob -Container {0}", containerName));

                Test.Assert(agent.StartFileCopy(blob: null, shareName: shareName, filePath: null, destContext: PowerShellAgent.Context), "Start file copy should succeed.");

                psAgent.AddPipelineScript(string.Format("Get-AzureStorageFile -ShareName {0}", shareName));

                Test.Assert(agent.GetFileCopyState(file:null, waitForComplete: true), "Get file copy state should succeed.");

                foreach (CloudBlob blob in blobs)
                {
                    this.ValidateCopyFromBlob(blob, fileUtil.GetFileReference(share.GetRootDirectoryReference(), blob.Name));
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromShare()
        {
            string srcShareName = Utility.GenNameString("srcshare");
            CloudFileShare srcShare = fileUtil.EnsureFileShareExists(srcShareName);
            fileUtil.CleanupDirectory(srcShare.GetRootDirectoryReference());

            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);
            fileUtil.CleanupDirectory(destShare.GetRootDirectoryReference());

            try
            {
                List<StorageFile.CloudFile> files = new List<StorageFile.CloudFile>();

                for (int i = 0; i < random.Next(1, 5); ++i)
                {
                    string fileName = Utility.GenNameString(string.Format("fileName{0}", i));
                    files.Add(fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), fileName));
                }

                PowerShellAgent psAgent = agent as PowerShellAgent;

                psAgent.AddPipelineScript(string.Format("Get-AzureStorageFile -ShareName {0}", srcShareName));

                Test.Assert(agent.StartFileCopy(blob: null, shareName: destShareName, filePath: null, destContext: PowerShellAgent.Context), "Start file copy should succeed.");

                psAgent.AddPipelineScript(string.Format("Get-AzureStorageFile -ShareName {0}", destShareName));

                Test.Assert(agent.GetFileCopyState(file: null, waitForComplete: true), "Get file copy state should succeed.");

                foreach (var file in files)
                {
                    this.ValidateCopyFromFile(file, fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), file.Name));
                }
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(srcShareName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromBlobSnapshotWithTooLongName()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = this.GetDeepestFilePath();
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
            var blobSnapshot = blob.Snapshot();

            this.CopyFromBlob(blobSnapshot, null);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromBlobWithTooLongNameAndSpecialChar()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = this.GetDeepestFilePath();
            blobName = blobName.Substring(0, blobName.Length - 10) + "\"\\:|<>*?";
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            this.CopyFromBlob(blob, null);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyToTheSameFile()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                StorageFile.CloudFile file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                Test.Assert(agent.StartFileCopyFromFile(share.Name, fileName, share.Name, fileName, PowerShellAgent.Context),
                    "Starting async copying from file to the same file should succeed.");

            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void StartFileAsyncCopyNegativeCases()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");

                // From invalid container name
                Test.Assert(!agent.StartFileCopyFromBlob("CONTAINER", fileName, share.Name, fileName, PowerShellAgent.Context),
                    "Starting async copying from invalid container name should fail.");

                ExpectedContainErrorMessage("Container name 'CONTAINER' is invalid");

                // From invalid share name
                Test.Assert(!agent.StartFileCopyFromFile("SHARE", fileName, share.Name, fileName, PowerShellAgent.Context),
                    "Starting async copying from invalid share name should fail.");

                ExpectedContainErrorMessage("The given share name/prefix 'SHARE' is not a valid name for a file share of Microsoft Azure File Service.");

                // To invalid share name
                Test.Assert(!agent.StartFileCopyFromFile(share.Name, fileName, "SHARE", fileName, PowerShellAgent.Context),
                    "Starting async copying to invalid share name should fail.");

                ExpectedContainErrorMessage("The given share name/prefix 'SHARE' is not a valid name for a file share of Microsoft Azure File Service.");

                // From null blob instance
                Test.Assert(!agent.StartFileCopy(blob: null, shareName: shareName, filePath: fileName, destContext: PowerShellAgent.Context),
                    "Starting async copying from null blob instance should fail.");

                ExpectedContainErrorMessage("Cannot validate argument on parameter 'SrcBlob'");

                // From null file instance
                Test.Assert(!agent.StartFileCopy(srcFile: null, shareName: shareName, filePath: fileName, destContext: PowerShellAgent.Context),
                    "Starting async copying from null file instance should fail.");

                ExpectedContainErrorMessage("Cannot validate argument on parameter 'SrcFile'");

                // From non-exist share
                string nonExistShareName = Utility.GenNameString("sharename");
                fileUtil.DeleteFileShareIfExists(nonExistShareName);
                Test.Assert(!agent.StartFileCopyFromFile(nonExistShareName, fileName, shareName, null, PowerShellAgent.Context),
                    "Starting async copying from non-exist file should fail.");

                ExpectedContainErrorMessage("The specified share does not exist");

                // From non-exist file
                Test.Assert(!agent.StartFileCopyFromFile(shareName, fileName, shareName, null, PowerShellAgent.Context),
                    "Starting async copying from non-exist file should fail.");

                ExpectedContainErrorMessage("The specified resource does not exist");

                // From non-exist directory
                CloudFileDirectory dir = share.GetRootDirectoryReference().GetDirectoryReference("nonexist");
                Test.Assert(!agent.StartFileCopy(dir, fileName, shareName, null, PowerShellAgent.Context),
                    "Starting async copying from non-exist directory should fail.");

                ExpectedContainErrorMessage("The specified parent path does not exist");

                // From non-exist container
                string containerName = Utility.GenNameString("container");
                blobUtil.RemoveContainer(containerName);

                Test.Assert(!agent.StartFileCopyFromBlob(containerName, fileName, shareName, null, PowerShellAgent.Context),
                    "Starting async copying from non-exist container should fail.");

                ExpectedContainErrorMessage("The specified container does not exist.");

                // From non-exist blob
                try
                {
                    blobUtil.CreateContainer(containerName);
                    Test.Assert(!agent.StartFileCopyFromBlob(containerName, fileName, shareName, null, PowerShellAgent.Context),
                        "Starting async copying from non-exist blob should fail.");

                    ExpectedContainErrorMessage("The specified blob does not exist.");
                }
                finally 
                {
                    blobUtil.RemoveContainer(containerName);
                }


                // To null file instance

                StorageFile.CloudFile file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);
                Test.Assert(!agent.StartFileCopy(file, null),
                    "Starting async copying to null file instance should fail.");

                ExpectedContainErrorMessage("Cannot validate argument on parameter 'DestFile'");

            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        private void ValidateCopyFromBlob(CloudBlob srcBlob, StorageFile.CloudFile destFile)
        {
            destFile.FetchAttributes();
            srcBlob.FetchAttributes();

            Test.Assert(destFile.Metadata.SequenceEqual(srcBlob.Metadata), "Destination's metadata should be the same with source's");
            Test.Assert(destFile.Properties.ContentMD5 == srcBlob.Properties.ContentMD5, "MD5 should be the same.");
            Test.Assert(destFile.Properties.ContentType == srcBlob.Properties.ContentType, "Content type should be the same.");
        }

        private void ValidateCopyFromFile(StorageFile.CloudFile srcFile, StorageFile.CloudFile destFile)
        {
            destFile.FetchAttributes();
            srcFile.FetchAttributes();

            Test.Assert(destFile.Metadata.SequenceEqual(srcFile.Metadata), "Destination's metadata should be the same with source's");
            Test.Assert(destFile.Properties.ContentMD5 == srcFile.Properties.ContentMD5, "MD5 should be the same.");
            Test.Assert(destFile.Properties.ContentType == srcFile.Properties.ContentType, "Content type should be the same.");
        }

        private void CopyToFile(CloudBlob srcBlob, string destShareName, string destFilePath, Action copyAction, bool destExist = false, bool toSecondaryAccount = false)
        {
            CloudFileUtil localFileUtil = toSecondaryAccount ? FileUtil2 : fileUtil;
            CloudFileShare share = localFileUtil.EnsureFileShareExists(destShareName);

            try
            {
                var destFile = localFileUtil.GetFileReference(share.GetRootDirectoryReference(), destFilePath);

                if (destExist)
                {
                    localFileUtil.CreateFile(share.GetRootDirectoryReference(), destFilePath);
                }

                copyAction();

                this.ValidateCopyFromBlob(srcBlob, destFile);
            }
            finally
            {
                localFileUtil.DeleteFileShareIfExists(destShareName);
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

        void CopyToFile(CloudBlob blob, bool toSecondaryAccount = false)
        {
            CloudFileUtil localFileUtil = toSecondaryAccount ? FileUtil2 : fileUtil;

            string destShareName = Utility.GenNameString("share");
            CloudFileShare share = localFileUtil.GetShareReference(destShareName);

            string destPath = Utility.GenNameString("filename");
            var destFile = localFileUtil.GetFileReference(share.GetRootDirectoryReference(), destPath);

            CopyToFile(blob, destShareName, destPath,
                ()=>
                {
                    Test.Assert(agent.StartFileCopy(blob, destFile, toSecondaryAccount ? SecondaryContext : PowerShellAgent.Context),
                        "Copy from blob to file should succeed.");

                    Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed");
                }, false, toSecondaryAccount);
        }

        void CopyFromBlob(CloudBlob blob, string destFilePath, bool destExist = false, bool toSecondaryAccount = false)
        {
            CloudFileUtil localFileUtil = toSecondaryAccount ? FileUtil2 : fileUtil;

            string destShareName = Utility.GenNameString("share");
            CloudFileShare share = localFileUtil.GetShareReference(destShareName);

            string actualDestPath = destFilePath ?? localFileUtil.ResolveFileName(blob);
            var destFile = localFileUtil.GetFileReference(share.GetRootDirectoryReference(), actualDestPath);

            this.CopyToFile(blob, destShareName, actualDestPath,
                () =>
                {
                    if (blob.IsSnapshot)
                    {
                        Test.Assert(agent.StartFileCopy(blob, destShareName, destFilePath, toSecondaryAccount ? SecondaryContext : PowerShellAgent.Context),
                            "Copy from blob to file shoule succeed.");

                        Test.Assert(agent.GetFileCopyState(destShareName, actualDestPath), "Get file copy state should succeed");
                    }
                    else
                    {
                        if (random.Next(0, 2) == 0)
                        {
                            Test.Assert(agent.StartFileCopyFromBlob(blob.Container.Name, blob.Name, destShareName, destFilePath, toSecondaryAccount ? SecondaryContext : PowerShellAgent.Context),
                                "Copy from blob to file with container name parameter set should succeed.");
                        }
                        else
                        {
                            Test.Assert(agent.StartFileCopy(blob.Container, blob.Name, destShareName, destFilePath, toSecondaryAccount ? SecondaryContext : PowerShellAgent.Context),
                                "Copy from blob to file with container instance parameter set should succeed.");
                        }

                        Test.Assert(agent.GetFileCopyState(destFile, true), "Get file copy state should succeed");
                    }
                }, destExist, toSecondaryAccount);
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

        private void CopyToFile(StorageFile.CloudFile srcFile, string destShareName, string destFilePath, Action copyAction, bool destExist = false, bool toSecondaryAccount = false)
        {
            CloudFileUtil localFileUtil = toSecondaryAccount ? FileUtil2 : fileUtil;

            CloudFileShare destShare = localFileUtil.EnsureFileShareExists(destShareName);

            try
            {
                var destFile = localFileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath);

                if (destExist)
                {
                    localFileUtil.CreateFile(destShare, destFilePath);
                }

                copyAction();

                this.ValidateCopyFromFile(srcFile, destFile);
            }
            finally
            {
                localFileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        void CopyFromFile(string filePath, string destFilePath, bool destExist = false, bool toSecondaryAccount = false)
        {
            string sourceShareName = Utility.GenNameString("share");
            CloudFileShare sourceShare = fileUtil.EnsureFileShareExists(sourceShareName);

            string destShareName = Utility.GenNameString("share");
            CloudFileUtil destFileUtil = toSecondaryAccount ? FileUtil2 : fileUtil;
            CloudFileShare destShare = destFileUtil.GetShareReference(destShareName);

            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Utility.GenNameString("file");
                }

                var sourceFile = fileUtil.CreateFile(sourceShare, filePath);
                var destFile = destFileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath ?? filePath);

                this.CopyToFile(sourceFile, destShareName, destFilePath ?? filePath,
                    () =>
                    {
                        Test.Assert(agent.StartFileCopyFromFile(sourceShareName, filePath, destShareName, destFilePath, toSecondaryAccount ? SecondaryContext : PowerShellAgent.Context),
                            "Copy from file to overwrite an existig file should succeed.");

                        Test.Assert(agent.GetFileCopyState(destFile, true), "Get copy state should succeed.");
                    }, destExist, toSecondaryAccount);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(sourceShareName);
            }
        }

        private string GetDeepestFilePath()
        {
            StringBuilder sb = new StringBuilder();
            int maxDirLength = 1008;
            while (sb.Length < maxDirLength + 1)
            {
                sb.Append(Utility.GenNameString("", Math.Min(8, maxDirLength - sb.Length)));
                sb.Append("/");
            }

            sb.Append(Utility.GenNameString("", 1024 - sb.Length));

            return sb.ToString();
        }
    }
}
