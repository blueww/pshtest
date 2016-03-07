namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
    public class RemoveAzureStorageFileTest : TestBase
    {
        private Random randomProvider = new Random();

        private CloudFileShare fileShare;

        [ClassInitialize]
        public static void NewAzureStorageFileShareTestInitialize(TestContext context)
        {
            TestBase.TestClassInitialize(context);
        }

        [ClassCleanup]
        public static void NewAzureStorageFileShareTestCleanup()
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
        /// Positive functional test case 5.10.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void PipelineMultipleFilesToRemoveTest()
        {
            int numberOfFiles = this.randomProvider.Next(2, 33);
            string[] names = Enumerable.Range(0, numberOfFiles)
                    .Select(i => CloudFileUtil.GenerateUniqueFileName()).ToArray();
            foreach (var name in names)
            {
                fileUtil.CreateFile(this.fileShare, name);
            }

            CommandAgent.RemoveFilesFromPipeline(this.fileShare.Name);
            var result = CommandAgent.Invoke(names);

            CommandAgent.AssertNoError();
            result.AssertNoResult();

            foreach (var name in names)
            {
                fileUtil.AssertFileNotExists(this.fileShare, name, string.Format(CultureInfo.InvariantCulture, "File {0} should be removed.", name));
            }
        }

        /// <summary>
        /// Positive functional test case 5.10.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveFileUnderADirectory()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudDirectoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = fileUtil.EnsureDirectoryExists(this.fileShare, cloudDirectoryName);
            var file = fileUtil.CreateFile(directory, cloudFileName);

            CommandAgent.RemoveFile(file);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertNoResult();
            fileUtil.AssertFileNotExists(this.fileShare, file.Name, "File should not exist after deleting.");
        }

        /// <summary>
        /// Positive functional test case 5.10.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveUsingPath()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "/a/b/c/" + cloudFileName;
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudPath);
            CommandAgent.RemoveFile(this.fileShare, cloudPath);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(!sourceFile.Exists(), "File should not exist after removed.");
        }

        /// <summary>
        /// Positive functional test case 5.10.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveUsingRelativePathFromRoot()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "a/b/c/" + cloudFileName;
            string relativeCloudPath = "a/b/../b/./c/" + cloudFileName;
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudPath);
            CommandAgent.RemoveFile(this.fileShare, relativeCloudPath);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(!sourceFile.Exists(), "File should not exist after removed.");
        }

        /// <summary>
        /// Positive functional test case 5.10.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveUsingRelativePathFromDirectoryObject()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "a/b/c/" + cloudFileName;
            string relativeCloudPath = "../../b/./c/" + cloudFileName;
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudPath);
            CommandAgent.RemoveFile(baseDir, relativeCloudPath);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(!sourceFile.Exists(), "File should not exist after removed.");
        }

        /// <summary>
        /// Negative functional test case 5.10.1 with invalid account
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveFileWithInvalidAccountTest()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.CreateFile(this.fileShare, fileName);

            // Creates an storage context object with invalid account
            // name.
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountName: true);
            object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
            CommandAgent.RemoveFile(this.fileShare.Name, fileName, invalidStorageContextObject);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AccountIsDisabledFullQualifiedErrorId, AssertUtil.NameResolutionFailureFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId, AssertUtil.InvalidResourceFullQualifiedErrorId));
            fileUtil.AssertFileExists(this.fileShare, fileName, "File should not be removed when providing invalid credentials.");
        }

        /// <summary>
        /// Negative functional test case 5.10.1 with invalid key value
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveFileWithInvalidKeyValueTest()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.CreateFile(this.fileShare, fileName);

            // Creates an storage context object with invalid key value
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountKey: true);
            object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
            CommandAgent.RemoveFile(this.fileShare.Name, fileName, invalidStorageContextObject);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AuthenticationFailedFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId));
            fileUtil.AssertFileExists(this.fileShare, fileName, "File should not be removed when providing invalid credentials.");
        }

        /// <summary>
        /// Negative functional test case 5.10.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveNonExistingFile()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.DeleteFileIfExists(this.fileShare, fileName);

            CommandAgent.RemoveFile(this.fileShare, fileName);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.10.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveNonExistingFileInANonExistingDirectory()
        {
            string directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            fileUtil.DeleteDirectoryIfExists(this.fileShare, directoryName);
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            var file = this.fileShare.GetRootDirectoryReference().GetDirectoryReference(directoryName).GetFileReference(fileName);

            CommandAgent.RemoveFile(this.fileShare, file.Name);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ParentNotFoundFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.10.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveNonExitingFileWhichMatchingPrefixOfAnExistingOne()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.DeleteFileIfExists(this.fileShare, fileName);
            string existingFile = fileName + "postfix";
            fileUtil.CreateFile(this.fileShare, existingFile);

            CommandAgent.RemoveFile(this.fileShare, fileName);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.10.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveDirectoryUsingRemoveFileCmdlet()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.DeleteFileIfExists(this.fileShare, fileName);
            fileUtil.EnsureDirectoryExists(this.fileShare, fileName);

            CommandAgent.RemoveFile(this.fileShare, fileName);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.10.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveFileFromSubDirectoryOfRootTest()
        {
            CommandAgent.RemoveFile(this.fileShare, "../a");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidResourceFullQualifiedErrorId, AssertUtil.AuthenticationFailedFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.10.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveFileFromRelativePathWhereIntermediatePathMightNotExistTest()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string relativeCloudPath = "../../ddd/../b/./c/" + cloudFileName;
            fileUtil.CreateFile(baseDir, cloudFileName);
            CommandAgent.RemoveFile(baseDir, relativeCloudPath);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            Test.Assert(!baseDir.GetFileReference(cloudFileName).Exists(), "File should exist after uploaded.");
        }
    }
}
