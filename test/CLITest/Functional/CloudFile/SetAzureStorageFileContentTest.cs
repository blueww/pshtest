namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    public class SetAzureStorageFileContentTest : TestBase
    {
        private Random randomProvider = new Random();

        private CloudFileShare fileShare;

        [ClassInitialize]
        public static void SetAzureStorageFileContentTestInitialize(TestContext context)
        {
            TestBase.TestClassInitialize(context);
        }

        [ClassCleanup]
        public static void SetAzureStorageFileContentTestCleanup()
        {
            TestBase.TestClassCleanup();
        }

        public override void OnTestSetup()
        {
            this.fileShare = fileUtil.EnsureFileShareExists(CloudFileUtil.GenerateUniqueFileShareName());
        }

        public override void OnTestCleanUp()
        {
            fileUtil.DeleteFileShareIfExists(this.fileShare.Name);
        }

        /// <summary>
        /// Positive functional test case 5.9.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadAndOverwrite()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.CreateFile(this.fileShare, cloudFileName);
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName, true);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            if (lang == Language.NodeJS)
            {
                result.AssertObjectCollection(obj => result.AssertCloudFile(obj, "/" + cloudFileName));
            }
            else
            {
                result.AssertNoResult();
            }
        }

        /// <summary>
        /// Positive functional test case 5.9.4
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void UploadWithPassThru()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName, false, true);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFile(cloudFileName));
        }

        /// <summary>
        /// Positive functional test case 5.9.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void UploadToCloudDirectory()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudDirectoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = fileUtil.EnsureDirectoryExists(this.fileShare, cloudDirectoryName);
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(directory, localFilePath, cloudFileName, true);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertNoResult();
        }

        /// <summary>
        /// Positive functional test case 5.9.8
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadUsingPath()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "/a/b/c/" + cloudFileName;
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudPath);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(baseDir.GetFileReference(cloudFileName).Exists(), "File should exist after uploaded.");
        }

        /// <summary>
        /// Positive functional test case 5.9.9
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadUsingRelativePathFromRoot()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string relativeCloudPath = "a/b/../b/./c/" + cloudFileName;
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, relativeCloudPath);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(baseDir.GetFileReference(cloudFileName).Exists(), "File should exist after uploaded.");
        }

        /// <summary>
        /// Positive functional test case 5.9.10
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void UploadUsingRelativePathFromDirectoryObject()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "a/b/c/" + cloudFileName;
            string relativeCloudPath = "../../b/./c/" + cloudFileName;
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(baseDir, localFilePath, relativeCloudPath);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(baseDir.GetFileReference(cloudFileName).Exists(), "File should exist after uploaded.");
        }

        /// <summary>
        /// Positive functional test case 5.9.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void UploadLocalFileUsingRelativePathAfterChangedDefaultLocation()
        {
            string currentPath = CommandAgent.GetCurrentLocation();
            try
            {
                string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
                string localFilePath = Path.Combine(Test.Data.Get("TempDir"), cloudFileName);
                FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
                CommandAgent.ChangeLocation(Test.Data.Get("TempDir"));
                CommandAgent.UploadFile(this.fileShare, cloudFileName, cloudFileName, true);
                var result = CommandAgent.Invoke();
                result.AssertNoResult();
                var file = this.fileShare.GetRootDirectoryReference().GetFileReference(cloudFileName);
                Test.Assert(file.Exists(), "File shold exist after uploaded.");
            }
            finally
            {
                CommandAgent.Clear();
                CommandAgent.ChangeLocation(currentPath);
            }
        }

        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadAndDownload0SizeFile()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            string localFilePath2 = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, 0, true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName, true);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(fileShare.GetRootDirectoryReference().GetFileReference(cloudFileName).Exists(), "File should exist after uploaded.");

            CommandAgent.Clear();
            CommandAgent.DownloadFile(this.fileShare.GetRootDirectoryReference(), cloudFileName, localFilePath2, true);
            result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(File.Exists(localFilePath2), "File should exist after uploaded.");
            Test.Assert(FileUtil.GetFileContentMD5(localFilePath) == FileUtil.GetFileContentMD5(localFilePath2), "The download file MD5 {0} should match Uploaded File MD5 {1}.", FileUtil.GetFileContentMD5(localFilePath2), FileUtil.GetFileContentMD5(localFilePath));
        }

        /// <summary>
        /// Negative functional test case 5.9.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadWithInvalidFileName_TooLong()
        {
            string cloudFileName = FileNamingGenerator.GenerateValidateASCIIName(256);
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName, true);
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.9.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadWithInvalidFileName_InvalidCharacters()
        {
            string cloudFileName = FileNamingGenerator.GenerateASCIINameWithInvalidCharacters(25);
            cloudFileName = cloudFileName.Replace(@"\", "*");
            cloudFileName = cloudFileName.Replace(@"/", "*");
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName, true);
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.9.2 with invalid account
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadWithInvalidAccountTest()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);

            // Creates an storage context object with invalid account
            // name.
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountName: true);
            object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
            CommandAgent.UploadFile(this.fileShare.Name, localFilePath, cloudFileName, false, false, invalidStorageContextObject);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AccountIsDisabledFullQualifiedErrorId, AssertUtil.NameResolutionFailureFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId, AssertUtil.InvalidResourceFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.9.2 with invalid key value
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadWithInvalidKeyValueTest()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);

            // Creates an storage context object with invalid key value
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountKey: true);
            object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
            CommandAgent.UploadFile(this.fileShare.Name, localFilePath, cloudFileName, false, false, invalidStorageContextObject);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AuthenticationFailedFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.9.3
        /// </summary>
        ////[TestMethod]
        ////[TestCategory(PsTag.File)]
        public void UploadToExistingFileWithoutOverwrite()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            var file = fileUtil.CreateFile(this.fileShare, cloudFileName);
            file.FetchAttributes();
            var etag = file.Properties.ETag;
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName, false);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ResourceAlreadyExistsFullQualifiedErrorId));
            file.FetchAttributes();
            Test.Assert(etag == file.Properties.ETag, "File should not be overwritten without overwrite flag.");
        }

        /// <summary>
        /// Negative functional test case 5.9.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void PipelineUploadingMultipleFilesWhichExistTest()
        {
            int numberOfFiles = this.randomProvider.Next(2, 33);
            Test.Info("About to upload {0} files.", numberOfFiles);
            this.UploadMultipleThroughPipeline(numberOfFiles, numberOfFiles);
        }

        /// <summary>
        /// Negative functional test case 5.9.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void PipelineUploadingMultipleFilesWhileSomeFilesAlreadyExistTest()
        {
            int numberOfFiles = this.randomProvider.Next(2, 33);
            int numberOfExistingFiles = this.randomProvider.Next(1, numberOfFiles);
            Test.Info("About to upload {0} files with {1} files already exists.", numberOfFiles, numberOfExistingFiles);
            this.UploadMultipleThroughPipeline(numberOfFiles, numberOfExistingFiles);
        }

        /// <summary>
        /// Negative functional test case 5.9.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadingToNonExistingDirectory()
        {
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);

            string cloudDirectoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            fileUtil.DeleteDirectoryIfExists(this.fileShare, cloudDirectoryName);
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            var file = this.fileShare.GetRootDirectoryReference().GetDirectoryReference(cloudDirectoryName).GetFileReference(cloudFileName);
            CommandAgent.UploadFile(this.fileShare, localFilePath, CloudFileUtil.GetFullPath(file));
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ParentNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.9.8
        /// </summary>
        ////[TestMethod]
        ////[TestCategory(PsTag.File)]
        public void UploadingToExistingFileWithNoOverwriteOption()
        {
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.CreateFile(this.fileShare, cloudFileName);

            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName);
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ResourceAlreadyExistsFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.9.9
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadAnNonExistingLocalFile()
        {
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.RemoveFile(localFilePath);

            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();

            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName);
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.PathNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.9.11
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadToAFileWhichHasJustBeenRemoved()
        {
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();

            // Creates the file and delete it.
            var file = fileUtil.CreateFile(this.fileShare, cloudFileName);
            file.Delete();

            // Upload the file immediately
            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            fileUtil.AssertFileExists(this.fileShare, cloudFileName, "File should exist after uploaded.");
        }

        /// <summary>
        /// Negative functional test case 5.9.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadAFileWithInvalidSpecialName()
        {
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            string cloudFileName = "CLOCK$";

            CommandAgent.UploadFile(this.fileShare, localFilePath, cloudFileName);
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.9.13
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadToADeletedFolder()
        {
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            var dir = fileUtil.EnsureDirectoryExists(this.fileShare, CloudFileUtil.GenerateUniqueDirectoryName());
            dir.Delete();
            CommandAgent.UploadFile(this.fileShare, localFilePath, CloudFileUtil.GetFullPath(dir) + "/");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ParentNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.9.14
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void UploadFileFromSubDirectoryOfRootTest()
        {
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, "../a");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidResourceFullQualifiedErrorId, AssertUtil.AuthenticationFailedFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.9.15
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void UploadFileFromRelativePathWhereIntermediatePathMightNotExistTest()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string relativeCloudPath = "../../ddd/../b/./c/" + cloudFileName;
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(baseDir, localFilePath, relativeCloudPath);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(baseDir.GetFileReference(cloudFileName).Exists(), "File should exist after uploaded.");
        }

        private void UploadMultipleThroughPipeline(int numberOfFiles, int numberOfExistingFiles)
        {
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);

            int[] indexes = Enumerable.Range(0, numberOfFiles).ToArray();
            string[] names = indexes.Select(i => CloudFileUtil.GenerateUniqueFileName()).ToArray();

            List<int> indexToBeRemoved = new List<int>(indexes);
            for (int i = 0; i < numberOfExistingFiles; i++)
            {
                int id = this.randomProvider.Next(indexToBeRemoved.Count);
                fileUtil.CreateFile(this.fileShare, names[indexToBeRemoved[id]]);
                indexToBeRemoved.RemoveAt(id);
            }

            CommandAgent.UploadFilesFromPipeline(this.fileShare.Name, localFilePath);
            var result = CommandAgent.Invoke(names);

            // Assert all files are created
            foreach (string name in names)
            {
                fileUtil.AssertFileExists(this.fileShare, name, string.Format("File {0} should exist after created.", name));
            }
        }
    }
}
