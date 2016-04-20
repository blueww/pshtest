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
using System.Threading;

namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    [TestClass]
    public class FileAsyncCopyTest : TestBase
    {
        private static CloudFileUtil FileUtil2 = null;
        private static readonly string[] InvalidFileNameChar = { "\"", ":", "|", "<", ">", "*", "?" };

        [ClassInitialize]
        public static void FileAsyncCopyTestInitialize(TestContext context)
        {
            TestBase.TestClassInitialize(context);
            CloudStorageAccount SecondaryAccount = TestBase.GetCloudStorageAccountFromConfig("Secondary");
            if (lang == Language.PowerShell)
            {
                Agent.SecondaryContext = PowerShellAgent.GetStorageContext(SecondaryAccount.ToString(true));
            }
            else
            {
                Agent.SecondaryContext = SecondaryAccount;
            }

            FileUtil2 = new CloudFileUtil(SecondaryAccount);
        }

        [ClassCleanup]
        public static void FileAsyncCopyTestCleanup()
        {
            TestBase.TestClassCleanup();
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyToExistFile()
        {
            string filePath = Utility.GenNameString("folder") + "/" + Utility.GenNameString("folder") + "/" + Utility.GenNameString("fileName");

            CopyFromBlob(null, filePath, null, true);
            CopyFromFile(filePath, null, true);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyFromRootContainer()
        {
            CopyFromBlob("$root", null, null);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyFromBlobSnapshot()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = Utility.GenNameString("fileName");
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
            var blobSnapshot = blob.Snapshot();

            Test.Info("{0}", blobSnapshot.Exists());

            CopyFromBlob(blobSnapshot, null);
            CopyToFile(blobSnapshot);
            CopyFromBlobWithUri(blobSnapshot);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyFromBlobWithLongName()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("", 1024);
            object context = Agent.Context ?? TestBase.StorageAccount;

            string destFileName = this.GetDeepestFilePath();

            this.CopyFromBlob(containerName, blobName, destFileName);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyFromBlobWithSpecialChar()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("\"\\:|<>&*?");

            this.CopyFromBlob(containerName, blobName, null);

            containerName = Utility.GenNameString("container");
            this.CopyFromBlob(containerName, blobName, null, true);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
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
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
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
                        Test.Assert(CommandAgent.StartFileCopy(srcFile, destShareName, destFilePath, Agent.SecondaryContext), "Start copy from file to file should succeed.");

                        Test.Assert(CommandAgent.GetFileCopyState(destFile, Agent.SecondaryContext, true), "Get file copy state should succeed.");
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
                        Test.Assert(CommandAgent.StartFileCopy(srcFile, destFile), "Start copy from file to file should succeed.");

                        Test.Assert(CommandAgent.GetFileCopyState(destFile, Agent.SecondaryContext, true), "Get file copy state should succeed.");
                    }, false, true);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyFromFileInDeepestDir()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare srcShare = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                bool toSecondaryAccout = true;
                CloudFileUtil destFileUtil = toSecondaryAccout ? FileUtil2 : fileUtil;
                object destContext = toSecondaryAccout ? Agent.SecondaryContext : Agent.Context;

                //TODO: Currently "copy file" only support path with 1024 character at maximum, update the code accordingly after the FE's behavior get updated
                string srcFileName = this.GetDeepestFilePath(1024);
                StorageFile.CloudFile srcFile = fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), srcFileName);

                string destShareName = Utility.GenNameString("destshare");
                string destFilePath = Utility.GenNameString("destFilePath");

                CloudFileShare destShare = destFileUtil.GetShareReference(destShareName);
                var destFile = destFileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath);

                this.CopyToFile(srcFile, destShareName, destFilePath,
                    () =>
                    {
                        Test.Assert(CommandAgent.StartFileCopyFromFile(shareName, srcFileName, destShareName, destFilePath, destContext), "Start copy from file to file should succeed.");

                        Test.Assert(CommandAgent.GetFileCopyState(destFile, destContext, true), "Get file copy state should succeed.");
                    }, false, toSecondaryAccout);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
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
                        Test.Assert(CommandAgent.StartFileCopy(srcFile, destShareName, destFilePath, Agent.Context), "Start copy from file to file should succeed.");

                        Test.Assert(CommandAgent.GetFileCopyState(destFile, Agent.Context, true), "Get file copy state should succeed.");
                    }, false, false);
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

                PowerShellAgent psAgent = CommandAgent as PowerShellAgent;

                Test.Assert(psAgent.StartFileCopyFromContainer(StorageAccount.ToString(true), StorageAccount.ToString(true), containerName, shareName),
                    "Start file copy should succeed.");

                psAgent.AddPipelineScript(string.Format("Get-AzureStorageFile -ShareName {0}", shareName));

                Test.Assert(CommandAgent.GetFileCopyState(file: null, context: Agent.Context, waitForComplete: true), "Get file copy state should succeed.");

                foreach (CloudBlob blob in blobs)
                {
                    CloudFileUtil.ValidateCopyResult(blob, fileUtil.GetFileReference(share.GetRootDirectoryReference(), blob.Name));
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

                PowerShellAgent psAgent = CommandAgent as PowerShellAgent;

                Test.Assert(psAgent.StartFileCopyFromShare(StorageAccount.ToString(true), StorageAccount.ToString(true), srcShareName, destShareName),
                    "Start file copy should succeed.");

                psAgent.AddPipelineScript(string.Format("Get-AzureStorageFile -ShareName {0}", destShareName));

                Test.Assert(CommandAgent.GetFileCopyState(file: null, context: Agent.Context, waitForComplete: true), "Get file copy state should succeed.");

                foreach (var file in files)
                {
                    CloudFileUtil.ValidateCopyResult(file, fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), file.Name));
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
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyToTheSameFile()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                StorageFile.CloudFile file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                Test.Assert(CommandAgent.StartFileCopyFromFile(share.Name, fileName, share.Name, fileName, Agent.Context),
                    "Starting async copying from file to the same file should succeed.");

            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void StartFileAsyncCopyNegativeCases()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");

                // From invalid container name
                Test.Assert(!CommandAgent.StartFileCopyFromBlob("CONTAINER", fileName, share.Name, fileName, Agent.Context),
                    "Starting async copying from invalid container name should fail.");

                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Container name 'CONTAINER' is invalid");
                }
                else
                {
                    ExpectedContainErrorMessage("Container name format is incorrect");
                }

                // From invalid share name
                Test.Assert(!CommandAgent.StartFileCopyFromFile("SHARE", fileName, share.Name, fileName, Agent.Context),
                    "Starting async copying from invalid share name should fail.");

                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The given share name/prefix 'SHARE' is not a valid name for a file share of Microsoft Azure File Service");
                }
                else
                {
                    ExpectedContainErrorMessage("Share name format is incorrect");
                }

                // To invalid share name
                Test.Assert(!CommandAgent.StartFileCopyFromFile(share.Name, fileName, "SHARE", fileName, Agent.Context),
                    "Starting async copying to invalid share name should fail.");

                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The given share name/prefix 'SHARE' is not a valid name for a file share of Microsoft Azure File Service");
                }
                else
                {
                    ExpectedContainErrorMessage("Share name format is incorrect");
                }

                if (lang == Language.PowerShell)
                {
                    // From null blob instance
                    Test.Assert(!CommandAgent.StartFileCopy(blob: null, shareName: shareName, filePath: fileName, destContext: Agent.Context),
                        "Starting async copying from null blob instance should fail.");

                    ExpectedContainErrorMessage("Parameter set cannot be resolved using the specified named parameters.");

                    // From null file instance
                    Test.Assert(!CommandAgent.StartFileCopy(srcFile: null, shareName: shareName, filePath: fileName, destContext: Agent.Context),
                        "Starting async copying from null file instance should fail.");

                    ExpectedContainErrorMessage("Parameter set cannot be resolved using the specified named parameters.");
                }

                // From non-exist share
                string nonExistShareName = Utility.GenNameString("sharename");
                fileUtil.DeleteFileShareIfExists(nonExistShareName);
                Test.Assert(!CommandAgent.StartFileCopyFromFile(nonExistShareName, fileName, shareName, fileName, Agent.Context),
                    "Starting async copying from non-exist file should fail.");

                ExpectedContainErrorMessage("The specified share does not exist");

                // From non-exist file
                string nonExistFileName = Utility.GenNameString("filename");
                Test.Assert(!CommandAgent.StartFileCopyFromFile(shareName, fileName, shareName, nonExistFileName, Agent.Context),
                    "Starting async copying from non-exist file should fail.");

                ExpectedContainErrorMessage("The specified resource does not exist");

                // From non-exist container
                string containerName = Utility.GenNameString("container");
                blobUtil.RemoveContainer(containerName);

                Test.Assert(!CommandAgent.StartFileCopyFromBlob(containerName, fileName, shareName, fileName, Agent.Context),
                    "Starting async copying from non-exist container should fail.");

                ExpectedContainErrorMessage("The specified container does not exist.");

                // From non-exist blob
                try
                {
                    CloudBlobContainer container = blobUtil.CreateContainer(containerName);
                    Test.Assert(!CommandAgent.StartFileCopyFromBlob(containerName, fileName, shareName, fileName, Agent.Context),
                        "Starting async copying from non-exist blob should fail.");

                    ExpectedContainErrorMessage("The specified blob does not exist.");
                }
                finally
                {
                    blobUtil.RemoveContainer(containerName);
                }

                StorageFile.CloudFile file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);
                if (lang == Language.PowerShell)
                {
                    // To null file instance
                    Test.Assert(!CommandAgent.StartFileCopy(file, null),
                        "Starting async copying to null file instance should fail.");

                    ExpectedContainErrorMessage("Parameter set cannot be resolved using the specified named parameters.");
                }

                // Copy to file with pending copying
                string destFileName = Utility.GenNameString("destFileName");
                StorageFile.CloudFile destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), destFileName);

                string bigBlobUri = Test.Data.Get("BigBlobUri");
                destFile.StartCopy(new Uri(bigBlobUri));
                destFile.FetchAttributes();
                Test.Assert(destFile.CopyState.Status == CopyStatus.Pending, "Copying status should be pending. {0}", destFile.CopyState.Status);

                Test.Assert(!CommandAgent.StartFileCopy(file, destFile), "Start copying to a file with pending copying should fail.");

                ExpectedContainErrorMessage("There is currently a pending copy operation.");

                Test.Assert(CommandAgent.StopFileCopy(destFile, destFile.CopyState.CopyId), "Stop copying to a file should succeed.");

                // To too long dest file path
                destFileName = GetDeepestFilePath();
                destFileName = destFileName + "/" + Utility.GenNameString("fileName", 64);
                fileUtil.CreateFileFolders(share, destFileName);
                Test.Assert(!CommandAgent.StartFileCopy(file, shareName, destFileName, Agent.Context), "Start copying to a file with too long name should fail.");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage(string.Format("The length of the given path/prefix '{0}' exceeded the max allowed length 2048 for Microsoft Azure File Service REST API.", destFileName));
                }
                else
                {
                    ExpectedContainErrorMessage("File or directory path is too long");
                }

                // To invalid dest file name
                string prefix = Utility.GenNameString("");
                string suffix = Utility.GenNameString("");

                StringBuilder invalidChars = new StringBuilder();
                for (int i = 0; i < random.Next(2, 255 - prefix.Length - suffix.Length); ++i)
                {
                    invalidChars.Append(InvalidFileNameChar[random.Next(0, InvalidFileNameChar.Count())]);
                }

                destFileName = prefix + invalidChars.ToString() + suffix;
                Test.Assert(!CommandAgent.StartFileCopy(file, shareName, destFileName, Agent.Context), "Start copying to a invalid file should fail.");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage(string.Format("The given path/prefix '{0}' is not a valid name for a file or directory or does match the requirement for Microsoft Azure File Service REST API.", destFileName));
                }
                else
                {
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters");
                }

                // To null dest file name
                Test.Assert(!CommandAgent.StartFileCopy(file, shareName, null, Agent.Context), "Start copying to null file path should fail.");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Cannot process command because of one or more missing mandatory parameters");
                }
                else
                {
                    ExpectedContainErrorMessage("--dest-path is required when copying to a file");
                }

                //Test.Assert(!agent.StartFileCopy("http://www.bing.com", destFile), "Start copying from invalid Uri should fail.");

                // Start from invalid uri
                Test.Assert(!CommandAgent.StartFileCopy("invalidUri", destFile), "Start copying from invalid Uri should fail.");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The format of the URI could not be determined.");
                }
                else
                {
                    ExpectedContainErrorMessage("The value for one of the HTTP headers is not in the correct format");
                }
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.GetFileCopyState)]
        public void GetStateOnPendingCopyFromBlobTest()
        {
            this.GetStateOnPendingCopy(Test.Data.Get("BigBlobUri"));
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.GetFileCopyState)]
        public void GetStateOnPendingCopyFromFileTest()
        {
            this.GetStateOnPendingCopy(Test.Data.Get("BigAzureFileUri"));
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.GetFileCopyState)]
        public void GetStateOnFinishedCopyTest()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string srcFileName = Utility.GenNameString("sourceFileName");
                StorageFile.CloudFile file = fileUtil.CreateFile(share.GetRootDirectoryReference(), srcFileName);

                string destFileName = Utility.GenNameString("destFileName");
                StorageFile.CloudFile destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), destFileName);

                Test.Assert(CommandAgent.StartFileCopy(file, destFile), "Start file async copy should succeed.");

                while (true)
                {
                    destFile.FetchAttributes();

                    if (destFile.CopyState.Status != CopyStatus.Pending)
                    {
                        break;
                    }

                    Thread.Sleep(2000);
                }

                DateTimeOffset beginTime = DateTimeOffset.UtcNow;

                Test.Assert(CommandAgent.GetFileCopyState(destFile, Agent.Context, true), "Get file copy state should succeed.");

                DateTimeOffset endTime = DateTimeOffset.UtcNow;

                Test.Assert(beginTime - endTime < TimeSpan.FromSeconds(2), "Get file copy state should finish immediately.");

                Utility.VerifyCopyState(destFile.CopyState, Utility.GetCopyState(CommandAgent, lang));
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.GetFileCopyState)]
        public void GetCopyOnDeepestDirCrossAccountTest()
        {
            CloudStorageAccount srcAccount = TestBase.GetCloudStorageAccountFromConfig("Secondary");
            object srcContext;
            if (lang == Language.PowerShell)
            {
                srcContext = PowerShellAgent.GetStorageContext(srcAccount.ToString(true));
            }
            else
            {
                srcContext = srcAccount;
            }
            CloudFileUtil srcFileUtil = new CloudFileUtil(srcAccount);

            string srcShareName = Utility.GenNameString("srcshare");
            CloudFileShare srcShare = srcFileUtil.EnsureFileShareExists(srcShareName);

            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                //TODO: Currently "copy file" only support path with 1024 character at maximum, update the code accordingly after the FE's behavior get updated
                string filePath = this.GetDeepestFilePath(1024);
                StorageFile.CloudFile srcFile = srcFileUtil.CreateFile(srcShare.GetRootDirectoryReference(), filePath);

                fileUtil.CreateFileFolders(destShare, filePath);

                Test.Assert(CommandAgent.StartFileCopy(srcFile, destShare.Name, filePath, Agent.Context), "Start copying from file should succeed.");

                Test.Assert(CommandAgent.GetFileCopyState(destShareName, filePath, Agent.Context, true), "Monitoring copy state of file should succeed.");

                var destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), filePath);
                destFile.FetchAttributes();

                Utility.VerifyCopyState(destFile.CopyState, Utility.GetCopyState(CommandAgent, lang));
            }
            finally
            {
                srcFileUtil.DeleteFileShareIfExists(srcShareName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.GetFileCopyState)]
        public void GetCopyStateNegativeCases()
        {
            Test.Assert(!CommandAgent.GetFileCopyState("SHARE", Utility.GenNameString("fileName"), Agent.Context), "Get file copy state should fail.");

            if (lang == Language.PowerShell)
            {
                ExpectedContainErrorMessage("The given share name/prefix 'SHARE' is not a valid name for a file share of Microsoft Azure File Service");
            }
            else
            {
                ExpectedContainErrorMessage("Share name format is incorrect");
            }

            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                Test.Assert(!CommandAgent.GetFileCopyState(shareName, "file???", Agent.Context), "Get file copy state with invalid file name should fail.");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The given path/prefix 'file???' is not a valid name for a file or directory or does match the requirement for Microsoft Azure File Service REST API.");
                }
                else
                {
                    ExpectedContainErrorMessage("BadRequest");
                }

                if (lang == Language.PowerShell)
                {
                    Test.Assert(!CommandAgent.GetFileCopyState(file: null, context: Agent.Context), "Get file copy state with null file instance should fail.");
                    ExpectedContainErrorMessage("Parameter set cannot be resolved using the specified named parameters");
                }
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void StopAListOfFileCopyTest()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();

            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                var blobs = blobUtil.CreateRandomBlob(container, true);
                PowerShellAgent psAgent = CommandAgent as PowerShellAgent;

                Test.Assert(psAgent.StartFileCopyFromContainer(StorageAccount.ToString(true), StorageAccount.ToString(true), container.Name, shareName), 
                    "Start file copy should succeed.");

                psAgent.AddPipelineScript(string.Format("Get-AzureStorageFile -ShareName {0}", shareName));
                Test.Assert(CommandAgent.StopFileCopy(file: null, copyId: null), "Stop file copy should succeed.");

                foreach (var fileItem in share.GetRootDirectoryReference().ListFilesAndDirectories())
                {
                    StorageFile.CloudFile file = fileItem as StorageFile.CloudFile;

                    if (null != file)
                    {
                        file.FetchAttributes();
                        Test.Assert(file.CopyState.Status == CopyStatus.Aborted, "Copy status of file {0} should be aborted, actual it's {1}", file.Name, file.CopyState.Status);
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(container);
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StopCopyFile)]
        public void StopFileCopyOnDeepestDirCrossAccountTest()
        {
            CloudStorageAccount srcAccount = TestBase.GetCloudStorageAccountFromConfig("Secondary");
            object srcContext;
            if (lang == Language.PowerShell)
            {
                srcContext = PowerShellAgent.GetStorageContext(srcAccount.ToString(true));
            }
            else
            {
                srcContext = srcAccount;
            }
            CloudBlobUtil srcBlobUtil = new CloudBlobUtil(srcAccount);

            string srcContainerName = Utility.GenNameString("srccontainer");
            CloudBlobContainer srcContainer = srcBlobUtil.CreateContainer(srcContainerName);

            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                string blobPath = Utility.GenNameString("blobName");
                CloudBlob srcBlob = srcBlobUtil.CreateBlockBlob(srcContainer, blobPath, createBigBlob: true);

                string filePath = this.GetDeepestFilePath();
                fileUtil.CreateFileFolders(destShare, filePath);

                Test.Assert(CommandAgent.StartFileCopy(srcBlob, destShareName, filePath, Agent.Context), "Start copying from blob cross account should succeed.");

                string copyId = null;
                if (lang == Language.NodeJS)
                {
                    copyId = CommandAgent.Output[0]["copyId"] as string;
                }

                Test.Assert(CommandAgent.StopFileCopy(destShareName, filePath, copyId), "Stop copying on deepest file path file should succeed.");

                var file = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), filePath);
                file.FetchAttributes();
                Test.Assert(CopyStatus.Aborted == file.CopyState.Status, "File copy status should be aborted, actual it's {0}", file.CopyState.Status);
            }
            finally
            {
                srcBlobUtil.RemoveContainer(srcContainerName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StopCopyFile)]
        public void StopWithCopyIdTest()
        {
            CloudStorageAccount srcAccount = TestBase.GetCloudStorageAccountFromConfig("Secondary");
            object srcContext;
            if (lang == Language.PowerShell)
            {
                srcContext = PowerShellAgent.GetStorageContext(srcAccount.ToString(true));
            }
            else
            {
                srcContext = srcAccount;
            }
            CloudBlobUtil srcBlobUtil = new CloudBlobUtil(srcAccount);

            string srcContainerName = Utility.GenNameString("srccontainer");
            CloudBlobContainer srcContainer = srcBlobUtil.CreateContainer(srcContainerName);

            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                string filePath = Utility.GenNameString("fileName");
                string destFilePath = Utility.GenNameString("fileName");
                CloudBlob srcBlob = srcBlobUtil.CreateBlockBlob(srcContainer, filePath, createBigBlob: true);

                Test.Assert(CommandAgent.StartFileCopy(srcBlob, destShareName, destFilePath, Agent.Context), "Start copying from blob cross account should succeed.");

                var file = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath);

                file.FetchAttributes();

                Test.Assert(CommandAgent.StopFileCopy(destShareName, destFilePath, file.CopyState.CopyId, false), "Stop copying with copy id should succeed.");

                file.FetchAttributes();

                Test.Assert(CopyStatus.Aborted == file.CopyState.Status, "File copy status should be aborted, actual it's {0}", file.CopyState.Status);

                if (lang == Language.PowerShell)
                {
                    Test.Assert(CommandAgent.StartFileCopy(srcBlob, destShareName, destFilePath, Agent.Context), "Start copying from blob to file should succeed.");
                    Test.Assert(CommandAgent.StopFileCopy(destShareName, destFilePath, Guid.NewGuid().ToString()), "Stop copying with unmatched copy id and force should succeed.");
                    file.FetchAttributes();
                    Test.Assert(CopyStatus.Aborted == file.CopyState.Status, "File copy status should be aborted, actual it's {0}", file.CopyState.Status);
                }
            }
            finally
            {
                srcBlobUtil.RemoveContainer(srcContainerName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StopCopyFile)]
        public void StopFileCopyNegativeCases()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                StorageFile.CloudFile file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                // Against a no copying file
                Test.Assert(!CommandAgent.StopFileCopy(file, Guid.NewGuid().ToString()), "Stop file copy against a file without copying should fail.");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find copy task on the specified file");
                }
                else
                {
                    ExpectedContainErrorMessage("There is currently no pending copy operation");
                }

                // Against a succeeded copying file.
                string destFileName = Utility.GenNameString("destFileName");
                StorageFile.CloudFile destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), destFileName);

                Test.Assert(CommandAgent.StartFileCopy(file, destFile), "Start copy from file to file should succeed.");
                Utility.WaitCopyToFinish(() =>
                {
                    destFile.FetchAttributes();
                    return destFile.CopyState;
                });

                Test.Assert(!CommandAgent.StopFileCopy(destFile, destFile.CopyState.CopyId), "Stop file copy against a succeeded copying should fail.");
                ExpectedContainErrorMessage("There is currently no pending copy operation.");

                // Invalid share Name
                Test.Assert(!CommandAgent.StopFileCopy("SHARE", Utility.GenNameString(""), destFile.CopyState.CopyId), "Stop file copy with an invalid share name should fail.");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The given share name/prefix 'SHARE' is not a valid name for a file share of Microsoft Azure File Service");
                }
                else
                {
                    ExpectedContainErrorMessage("Share name format is incorrect");
                }

                // Invalid file name
                Test.Assert(!CommandAgent.StopFileCopy(shareName, "file???", destFile.CopyState.CopyId), "Stop file copy with an invalid file path should fail.");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The given path/prefix 'file???' is not a valid name for a file or directory or does match the requirement for Microsoft Azure File Service REST API.");
                }
                else
                {
                    // TODO: fix the typo "specifed" when server fixes it
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters");
                }

                // Non exist share
                string nonExistShareName = Utility.GenNameString("nonexist");
                Test.Assert(!CommandAgent.StopFileCopy(nonExistShareName, Utility.GenNameString(""), destFile.CopyState.CopyId), "Stop file copy under a non-exist share should fail.");
                ExpectedContainErrorMessage("The specified share does not exist.");

                // Non exist file
                string nonExistFileName = Utility.GenNameString("NonExistFileName");
                Test.Assert(!CommandAgent.StopFileCopy(shareName, nonExistFileName, destFile.CopyState.CopyId), "Stop file copy under a non-exist share should fail.");
                ExpectedContainErrorMessage("The specified resource does not exist.");

                if (lang == Language.PowerShell)
                {
                    // Null file instance
                    Test.Assert(!CommandAgent.StopFileCopy(file: null, copyId: null), "Stop file copy with an null file instance should fail.");
                    ExpectedContainErrorMessage("Parameter set cannot be resolved using the specified named parameters.");
                }
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        private void GetStateOnPendingCopy(string bigFileUri)
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                StorageFile.CloudFile file = fileUtil.GetFileReference(share.GetRootDirectoryReference(), fileName);

                file.StartCopy(new Uri(bigFileUri));

                Test.Assert(CommandAgent.GetFileCopyState(shareName, fileName, Agent.Context), "Get file copy state should succeed.");
                file.FetchAttributes();

                Utility.VerifyCopyState(file.CopyState, Utility.GetCopyState(CommandAgent, lang));

                file.AbortCopy(file.CopyState.CopyId);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        private void CopyToFile(CloudBlob srcBlob, string destShareName, string destFilePath, Action copyAction, bool destExist = false, bool toSecondaryAccount = false)
        {
            CloudFileUtil localFileUtil = toSecondaryAccount ? FileUtil2 : fileUtil;
            CloudFileShare share = localFileUtil.EnsureFileShareExists(destShareName);

            try
            {
                var destFile = localFileUtil.GetFileReference(share.GetRootDirectoryReference(), destFilePath);
                localFileUtil.CreateFileFolders(share, destFilePath);

                if (destExist)
                {
                    localFileUtil.CreateFile(share.GetRootDirectoryReference(), destFilePath);
                }

                copyAction();

                CloudFileUtil.ValidateCopyResult(srcBlob, destFile);
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
                    string blobUri = CommandAgent.GetBlobSasFromCmd(blob, null, "r", null, DateTime.UtcNow.AddHours(1), true);
                    Test.Assert(CommandAgent.StartFileCopy(blobUri, destShareName, destPath, Agent.Context), "Copy from blob to file should succeed.");

                    Test.Assert(CommandAgent.GetFileCopyState(destFile, Agent.Context, true), "Get file copy state should succeed");
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
                    string blobUri = CommandAgent.GetBlobSasFromCmd(blob, null, "r", null, DateTime.UtcNow.AddHours(1), true);
                    Test.Assert(CommandAgent.StartFileCopy(blobUri, destFile), "Copy from blob to file should succeed.");

                    Test.Assert(CommandAgent.GetFileCopyState(destFile, Agent.Context, true), "Get file copy state should succeed");
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
                () =>
                {
                    object context = toSecondaryAccount ? Agent.SecondaryContext : Agent.Context;
                    Test.Assert(CommandAgent.StartFileCopy(blob, destFile, context),
                        "Copy from blob to file should succeed.");

                    Test.Assert(CommandAgent.GetFileCopyState(destFile, context, true), "Get file copy state should succeed");
                }, false, toSecondaryAccount);
        }

        void CopyFromBlob(CloudBlob blob, string destFilePath, bool destExist = false, bool toSecondaryAccount = false)
        {
            CloudFileUtil localFileUtil = toSecondaryAccount ? FileUtil2 : fileUtil;

            string destShareName = Utility.GenNameString("share");
            CloudFileShare share = localFileUtil.GetShareReference(destShareName);

            if (string.IsNullOrEmpty(destFilePath))
            {
                destFilePath = Utility.GenNameString("file");
            }

            var destFile = localFileUtil.GetFileReference(share.GetRootDirectoryReference(), destFilePath);

            this.CopyToFile(blob, destShareName, destFilePath,
                () =>
                {
                    object context = toSecondaryAccount ? Agent.SecondaryContext : Agent.Context;
                    if (blob.IsSnapshot)
                    {
                        Test.Assert(CommandAgent.StartFileCopy(blob, destShareName, destFilePath, context),
                            "Copy from blob to file shoule succeed.");

                        Test.Assert(CommandAgent.GetFileCopyState(destShareName, destFilePath, context), "Get file copy state should succeed");
                    }
                    else
                    {
                        if (random.Next(0, 2) == 0)
                        {
                            Test.Assert(CommandAgent.StartFileCopyFromBlob(blob.Container.Name, blob.Name, destShareName, destFilePath, context),
                                "Copy from blob to file with container name parameter set should succeed.");
                        }
                        else
                        {
                            Test.Assert(CommandAgent.StartFileCopy(blob.Container, blob.Name, destShareName, destFilePath, context),
                                "Copy from blob to file with container instance parameter set should succeed.");
                        }

                        Test.Assert(CommandAgent.GetFileCopyState(destFile.Share.Name, destFilePath, context, true), "Get file copy state should succeed");
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
                if (!string.Equals(containerName, "$root"))
                {
                    blobUtil.RemoveContainer(containerName);
                }
                else
                {
                    blobUtil.CleanupContainer(containerName);
                }
            }
        }

        private void CopyToFile(StorageFile.CloudFile srcFile, string destShareName, string destFilePath, Action copyAction, bool destExist = false, bool toSecondaryAccount = false)
        {
            CloudFileUtil localFileUtil = toSecondaryAccount ? FileUtil2 : fileUtil;

            CloudFileShare destShare = localFileUtil.EnsureFileShareExists(destShareName);

            try
            {
                var destFile = localFileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath);
                localFileUtil.CreateFileFolders(destShare, destFilePath);

                if (destExist)
                {
                    localFileUtil.CreateFile(destShare, destFilePath);
                }

                copyAction();

                CloudFileUtil.ValidateCopyResult(srcFile, destFile, destExist);
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
            destShare.CreateIfNotExists();

            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Utility.GenNameString("file");
                }

                if (string.IsNullOrEmpty(destFilePath))
                {
                    destFilePath = Utility.GenNameString("file");
                }

                var sourceFile = fileUtil.CreateFile(sourceShare, filePath);
                var destFile = destFileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFilePath ?? filePath);

                this.CopyToFile(sourceFile, destShareName, destFilePath ?? filePath,
                    () =>
                    {
                        object context = toSecondaryAccount ? Agent.SecondaryContext : Agent.Context;
                        Test.Assert(CommandAgent.StartFileCopyFromFile(sourceShareName, filePath, destShareName, destFilePath, context),
                            "Copy from file to overwrite an existig file should succeed.");

                        Test.Assert(CommandAgent.GetFileCopyState(destFile.Share.Name, destFile.Name, context, true), "Get copy state should succeed.");
                    }, destExist, toSecondaryAccount);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(sourceShareName);
            }
        }

        private string GetDeepestFilePath(int maxLength = 2048)
        {
            StringBuilder sb = new StringBuilder();
            int maxDirLength = maxLength - 16;
            while (sb.Length < maxDirLength)
            {
                sb.Append(Utility.GenNameString("", Math.Min(16, maxDirLength - sb.Length)));
                sb.Append("/");
            }

            sb.Append(Utility.GenNameString("", maxLength - sb.Length));

            return sb.ToString();
        }
    }
}
