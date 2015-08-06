namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.File;
    using StorageTestLib;

    [TestClass]
    internal class GetAzureStorageFileTest : TestBase
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
            this.agent.Dispose();
            fileUtil.DeleteFileShareIfExists(this.fileShare.Name);
        }

        /// <summary>
        /// Positive functional test case 5.7.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void GetFilePipeliningWithGetShareTest()
        {
            var directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = fileUtil.EnsureDirectoryExists(this.fileShare, directoryName);
            fileUtil.CleanupDirectory(directory);
            var fileName = CloudFileUtil.GenerateUniqueFileName();
            var file = fileUtil.CreateFile(directory, fileName);
            this.agent.GetFileShareByName(this.fileShare.Name);
            ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("Get-AzureStorageFile");
            ((PowerShellAgent)this.agent).PowerShellSession.AddParameter("Path", directoryName);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFile(fileName, directoryName));
        }

        /// <summary>
        /// Positive functional test cases 5.7.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetFilesFromRootTest()
        {
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var dir = this.fileShare.GetRootDirectoryReference();
            fileUtil.CleanupDirectory(dir);
            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();

            this.agent.ListFiles(this.fileShare);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
        }

        /// <summary>
        /// Positive functional test cases 5.7.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetFilesFromDirectoryUsingPathTest()
        {
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            var dir = fileUtil.EnsureDirectoryExists(this.fileShare, directoryName);
            fileUtil.CleanupDirectory(dir);
            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();

            this.agent.ListFiles(this.fileShare, directoryName);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
        }

        /// <summary>
        /// Positive functional test cases 5.7.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void GetFilesFromDirectoryTest()
        {
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            var dir = fileUtil.EnsureDirectoryExists(this.fileShare, directoryName);
            fileUtil.CleanupDirectory(dir);
            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();

            this.agent.ListFiles(dir);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
        }

        /// <summary>
        /// Positive functional test cases 5.7.10
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetFilesFromSubDirectoryUsingPathTest()
        {
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            fileUtil.CleanupDirectory(dir);
            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();

            this.agent.ListFiles(this.fileShare, "/a/b/c");
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
        }

        /// <summary>
        /// Positive functional test cases 5.7.11
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetFilesFromSubDirectoryUsingRelativePathFromRootTest()
        {
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            fileUtil.CleanupDirectory(dir);
            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();

            this.agent.ListFiles(this.fileShare, "a/b/.././b/../b/c");
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
        }

        /// <summary>
        /// Positive functional test cases 5.7.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void GetFilesFromSubDirectoryUsingRelativePathTest()
        {
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            fileUtil.CleanupDirectory(dir);
            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();

            this.agent.ListFiles(dir.Parent, "../b/./c");
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
        }

        /// <summary>
        /// Negative functional test cases 5.7.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetFilesFromEmptyFolderTest()
        {
            var dir = this.fileShare.GetRootDirectoryReference();
            fileUtil.CleanupDirectory(dir);

            this.agent.ListFiles(this.fileShare);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(Enumerable.Empty<CloudFile>(), Enumerable.Empty<CloudFileDirectory>());
        }

        /// <summary>
        /// Negative functional test cases 5.7.2 using file share name.
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetFilesUsingInvalidFileShareNameTest()
        {
            var invalidFileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.DeleteFileShareIfExists(invalidFileShareName);

            this.agent.ListFiles(invalidFileShareName);
            var result = this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ShareBeingDeletedFullQualifiedErrorId,
                AssertUtil.ShareNotFoundFullQualifiedErrorId,
                AssertUtil.ProtocolErrorFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.7.2 using file share object.
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void GetFilesUsingInvalidFileShareObjectTest()
        {
            var invalidFileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.DeleteFileShareIfExists(invalidFileShareName);

            this.agent.ListFiles(fileUtil.Client.GetShareReference(invalidFileShareName));
            var result = this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ShareBeingDeletedFullQualifiedErrorId,
                AssertUtil.ShareNotFoundFullQualifiedErrorId,
                AssertUtil.ProtocolErrorFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.7.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetFilesFromSubDirectoryOfRootTest()
        {
            this.agent.ListFiles(this.fileShare, "../a");
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidResourceFullQualifiedErrorId, AssertUtil.AuthenticationFailedFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.7.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetFilesFromRelativePathWhereIntermediatePathMightNotExistTest()
        {
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            fileUtil.CleanupDirectory(dir);
            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();

            this.agent.ListFiles(this.fileShare, "a/c/./../b/./c/e/..");
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
        }
    }
}
