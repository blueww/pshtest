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
    internal class RemoveAzureStorageFileTest : TestBase
    {
        private Random randomProvider = new Random();

        private CloudFileShare fileShare;

        [ClassInitialize]
        public static void NewAzureStorageFileShareTestInitialize(TestContext context)
        {
            StorageAccount = Utility.ConstructStorageAccountFromConnectionString();
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
            this.agent.Dispose();
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

            this.agent.RemoveFilesFromPipeline(this.fileShare.Name);
            var result = this.agent.Invoke(names);

            this.agent.AssertNoError();
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
        public void RemoveFileUnderADirectory()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudDirectoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = fileUtil.EnsureDirectoryExists(this.fileShare, cloudDirectoryName);
            var file = fileUtil.CreateFile(directory, cloudFileName);

            this.agent.RemoveFile(file);
            var result = agent.Invoke();
            this.agent.AssertNoError();
            result.AssertNoResult();
            fileUtil.AssertFileNotExists(this.fileShare, file.Name, "File should not exist after deleting.");
        }

        /// <summary>
        /// Positive functional test case 5.10.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveUsingPath()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "/a/b/c/" + cloudFileName;
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudPath);
            this.agent.RemoveFile(this.fileShare, cloudPath);
            this.agent.Invoke();
            this.agent.AssertNoError();
            Test.Assert(!sourceFile.Exists(), "File should not exist after removed.");
        }

        /// <summary>
        /// Positive functional test case 5.10.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveUsingRelativePathFromRoot()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "a/b/c/" + cloudFileName;
            string relativeCloudPath = "a/b/../b/./c/" + cloudFileName;
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudPath);
            this.agent.RemoveFile(this.fileShare, relativeCloudPath);
            this.agent.Invoke();
            this.agent.AssertNoError();
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
            this.agent.RemoveFile(baseDir, relativeCloudPath);
            this.agent.Invoke();
            this.agent.AssertNoError();
            Test.Assert(!sourceFile.Exists(), "File should not exist after removed.");
        }

        /// <summary>
        /// Negative functional test case 5.10.1 with invalid account
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveFileWithInvalidAccountTest()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.CreateFile(this.fileShare, fileName);

            // Creates an storage context object with invalid account
            // name.
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountName: true);
            object invalidStorageContextObject = this.agent.CreateStorageContextObject(invalidAccount.ToString(true));
            this.agent.RemoveFile(this.fileShare.Name, fileName, invalidStorageContextObject);
            var result = this.agent.Invoke();
            this.agent.AssertErrors(record => record.AssertFullQualifiedErrorId(AssertUtil.AccountIsDisabledFullQualifiedErrorId, AssertUtil.NameResolutionFailureFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId, AssertUtil.InvalidResourceFullQualifiedErrorId));
            fileUtil.AssertFileExists(this.fileShare, fileName, "File should not be removed when providing invalid credentials.");
        }

        /// <summary>
        /// Negative functional test case 5.10.1 with invalid key value
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveFileWithInvalidKeyValueTest()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.CreateFile(this.fileShare, fileName);

            // Creates an storage context object with invalid key value
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountKey: true);
            object invalidStorageContextObject = this.agent.CreateStorageContextObject(invalidAccount.ToString(true));
            this.agent.RemoveFile(this.fileShare.Name, fileName, invalidStorageContextObject);
            var result = this.agent.Invoke();
            this.agent.AssertErrors(record => record.AssertFullQualifiedErrorId(AssertUtil.AuthenticationFailedFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId));
            fileUtil.AssertFileExists(this.fileShare, fileName, "File should not be removed when providing invalid credentials.");
        }

        /// <summary>
        /// Negative functional test case 5.10.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveNonExistingFile()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.DeleteFileIfExists(this.fileShare, fileName);

            this.agent.RemoveFile(this.fileShare, fileName);
            var result = agent.Invoke();
            this.agent.AssertErrors(err => err.AssertFullQualifiedErrorId(AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.10.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveNonExistingFileInANonExistingDirectory()
        {
            string directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            fileUtil.DeleteDirectoryIfExists(this.fileShare, directoryName);
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            var file = this.fileShare.GetRootDirectoryReference().GetDirectoryReference(directoryName).GetFileReference(fileName);

            this.agent.RemoveFile(this.fileShare, file.Name);
            var result = agent.Invoke();
            this.agent.AssertErrors(err => err.AssertFullQualifiedErrorId(AssertUtil.ParentNotFoundFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.10.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveNonExitingFileWhichMatchingPrefixOfAnExistingOne()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.DeleteFileIfExists(this.fileShare, fileName);
            string existingFile = fileName + "postfix";
            fileUtil.CreateFile(this.fileShare, existingFile);

            this.agent.RemoveFile(this.fileShare, fileName);
            var result = agent.Invoke();
            this.agent.AssertErrors(err => err.AssertFullQualifiedErrorId(AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.10.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveDirectoryUsingRemoveFileCmdlet()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            fileUtil.DeleteFileIfExists(this.fileShare, fileName);
            fileUtil.EnsureDirectoryExists(this.fileShare, fileName);

            this.agent.RemoveFile(this.fileShare, fileName);
            var result = agent.Invoke();
            this.agent.AssertErrors(err => err.AssertFullQualifiedErrorId(AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.10.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveFileFromSubDirectoryOfRootTest()
        {
            this.agent.RemoveFile(this.fileShare, "../a");
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertFullQualifiedErrorId(AssertUtil.InvalidResourceFullQualifiedErrorId));
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
            this.agent.RemoveFile(baseDir, relativeCloudPath);
            this.agent.Invoke();
            this.agent.AssertNoError();
            Test.Assert(!baseDir.GetFileReference(cloudFileName).Exists(), "File should exist after uploaded.");
        }
    }
}
