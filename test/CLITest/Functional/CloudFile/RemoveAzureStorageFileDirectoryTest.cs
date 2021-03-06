﻿namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Text;
    using System.Threading.Tasks;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    public class RemoveAzureStorageFileDirectoryTest : TestBase
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
        /// Positive functional test case 5.6.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void PipelineMultipleDirectoriesToRemoveTest()
        {
            int numberOfDirectories = this.randomProvider.Next(2, 33);
            string[] names = Enumerable.Range(0, numberOfDirectories)
                    .Select(i => CloudFileUtil.GenerateUniqueDirectoryName()).ToArray();
            foreach (var name in names)
            {
                fileUtil.EnsureDirectoryExists(this.fileShare, name);
            }

            CommandAgent.RemoveDirectoriesFromPipeline(this.fileShare.Name);
            var result = CommandAgent.Invoke(names);

            CommandAgent.AssertNoError();
            result.AssertNoResult();

            foreach (var name in names)
            {
                fileUtil.AssertDirectoryNotExists(this.fileShare, name, string.Format(CultureInfo.InvariantCulture, "Directory {0} should be removed.", name));
            }
        }

        /// <summary>
        /// Positive functional test case 5.6.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveSubDirectoryUsingPath()
        {
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            this.RemoveDirectoryInternal(
                () => CommandAgent.RemoveDirectory(this.fileShare, "/a/b/c"),
                dir);
        }

        /// <summary>
        /// Positive functional test case 5.6.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveSubDirectoryUsingRelativePath()
        {
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            this.RemoveDirectoryInternal(
                () => CommandAgent.RemoveDirectory(this.fileShare, "a/b/.././../a/b/c"),
                dir);
        }

        /// <summary>
        /// Positive functional test case 5.6.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveSubDirectoryUsingRelativePathFromAFolder()
        {
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            this.RemoveDirectoryInternal(
                () => CommandAgent.RemoveDirectory(dir, "../../b/./c"),
                dir);
        }

        /// <summary>
        /// Negative functional test case 5.6.1 with invalid account
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveDirectoryWithInvalidAccountTest()
        {
            string dir = CloudFileUtil.GenerateUniqueDirectoryName();
            fileUtil.EnsureDirectoryExists(this.fileShare, dir);

            // Creates an storage context object with invalid account
            // name.
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountName: true);
            object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
            CommandAgent.RemoveDirectory(this.fileShare.Name, dir, invalidStorageContextObject);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AccountIsDisabledFullQualifiedErrorId, AssertUtil.NameResolutionFailureFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId, AssertUtil.InvalidResourceFullQualifiedErrorId));
            fileUtil.AssertDirectoryExists(this.fileShare, dir, "Directory should not be created when providing invalid credentials.");
        }

        /// <summary>
        /// Negative functional test case 5.6.1 with invalid key value
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveDirectoryWithInvalidKeyValueTest()
        {
            string dir = CloudFileUtil.GenerateUniqueDirectoryName();
            fileUtil.EnsureDirectoryExists(this.fileShare, dir);

            // Creates an storage context object with invalid key value
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountKey: true);
            object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
            CommandAgent.RemoveDirectory(this.fileShare.Name, dir, invalidStorageContextObject);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AuthenticationFailedFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId));
            fileUtil.AssertDirectoryExists(this.fileShare, dir, "Directory should not be created when providing invalid credentials.");
        }

        /// <summary>
        /// Negative functional test case 5.6.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveNonExistingDirectoryTest()
        {
            string dir = CloudFileUtil.GenerateUniqueDirectoryName();
            fileUtil.DeleteDirectoryIfExists(this.fileShare, dir);

            CommandAgent.RemoveDirectory(this.fileShare, dir);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.6.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveNonEmptyDirectoryTest()
        {
            string dir = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = fileUtil.EnsureDirectoryExists(this.fileShare, dir);
            fileUtil.CreateFile(directory, CloudFileUtil.GenerateUniqueFileName());

            CommandAgent.RemoveDirectory(this.fileShare, dir);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.DirectoryNotEmptyFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.6.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveDirectoryUnderNonExistingShareTest()
        {
            string shareName = CloudFileUtil.GenerateUniqueFileShareName();
            string dir = CloudFileUtil.GenerateUniqueDirectoryName();
            fileUtil.DeleteFileShareIfExists(shareName);
            CommandAgent.RemoveDirectory(shareName, dir);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.ShareNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.6.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveRootDirectoryTest()
        {
            CommandAgent.RemoveDirectory(this.fileShare, "/");
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.InvalidResourceFullQualifiedErrorId, AssertUtil.AuthenticationFailedFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.6.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveDirectoryUnderRootsParent()
        {
            string dirName = CloudFileUtil.GenerateUniqueDirectoryName();
            this.RemoveDirectoryInternal(() => CommandAgent.RemoveDirectory(this.fileShare, "../" + dirName));
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidResourceFullQualifiedErrorId, AssertUtil.AuthenticationFailedFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.6.8
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveDirectoryWithRelativePathWhereIntermediatePathMightNotExist()
        {
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            this.RemoveDirectoryInternal(
                () => CommandAgent.RemoveDirectory(dir, "d/../e/./../../c"),
                dir);
        }

        private IExecutionResult RemoveDirectoryInternal(Action removeDirectoryAction, CloudFileDirectory dirToBeRemovedForValidation = null)
        {
            removeDirectoryAction();
            var result = CommandAgent.Invoke();
            if (dirToBeRemovedForValidation != null)
            {
                CommandAgent.AssertNoError();
                result.AssertNoResult();
                Test.Assert(!dirToBeRemovedForValidation.Exists(), "Directory should not exist after removed.");
            }

            return result;
        }
    }
}
