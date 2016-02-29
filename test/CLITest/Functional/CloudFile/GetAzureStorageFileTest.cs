namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    public class GetAzureStorageFileTest : TestBase
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
            this.agent.Clear();
            this.fileShare = fileUtil.EnsureFileShareExists(CloudFileUtil.GenerateUniqueFileShareName());
        }

        public override void OnTestCleanUp()
        {
            this.agent.Clear();
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
            this.agent.GetFileShareByName(this.fileShare.Name);
            ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("Get-AzureStorageFile");
            ((PowerShellAgent)this.agent).PowerShellSession.AddParameter("Path", directoryName);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory(directoryName));

            this.agent.Clear();
            var fileName = CloudFileUtil.GenerateUniqueFileName();
            var file = fileUtil.CreateFile(this.fileShare.GetRootDirectoryReference(), fileName);
            this.agent.GetFileShareByName(this.fileShare.Name);
            ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("Get-AzureStorageFile");
            ((PowerShellAgent)this.agent).PowerShellSession.AddParameter("Path", fileName);
            result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFile(fileName));

            this.agent.Clear();
            List<CloudFileDirectory> dirs = new List<CloudFileDirectory>();
            List<CloudFile> files = new List<CloudFile>();
            dirs.Add(directory);
            files.Add(file);
            this.agent.GetFileShareByName(this.fileShare.Name);
            ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("Get-AzureStorageFile");
            result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, dirs);
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
            var dirNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var dir = this.fileShare.GetRootDirectoryReference();
            fileUtil.CleanupDirectory(dir);

            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();
            var dirs = dirNames.Select(name => fileUtil.EnsureDirectoryExists(dir, name)).ToList();

            this.agent.Clear();
            this.agent.GetFile(this.fileShare);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, dirs);

            // xPlat doesn't have the "file show" command. It only has the "file list" command which is only target for the directory
            if (lang != Language.NodeJS)
            {
                this.agent.Clear();
                this.agent.GetFile(this.fileShare, fileNames[0]);
                result = this.agent.Invoke();
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile(fileNames[0]));

                this.agent.Clear();
                this.agent.GetFile(this.fileShare.Name, fileNames[0]);
                result = this.agent.Invoke();
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile(fileNames[0]));

                this.agent.Clear();
                this.agent.GetFile(this.fileShare.GetRootDirectoryReference(), fileNames[0]);
                result = this.agent.Invoke();
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile(fileNames[0]));
            }
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

            this.agent.GetFile(dir);
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
        public void GetFilesUsingPathTest()
        {
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            fileUtil.CleanupDirectory(dir);

            this.agent.Clear();
            IExecutionResult result;

            if (lang == Language.NodeJS)
            {
                this.agent.GetFile(this.fileShare, "/a/b");
                result = this.agent.Invoke();
                this.agent.AssertNoError();

                result.AssertFileListItems(Enumerable.Empty<CloudFile>(), new []{ dir });
            }
            else
            {
                this.agent.GetFile(this.fileShare, "/a/b/c");
                result = this.agent.Invoke();
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory("a/b/c"));
            }

            this.agent.Clear();
            var file = fileUtil.CreateFile(dir, "d");

            if (lang == Language.NodeJS)
            {
                this.agent.GetFile(this.fileShare, "/a/b/c");
                result = this.agent.Invoke();
                this.agent.AssertNoError();

                result.AssertFileListItems(new [] { file }, Enumerable.Empty<CloudFileDirectory>());
            }
            else
            {
                this.agent.GetFile(this.fileShare, "/a/b/c/d");
                result = this.agent.Invoke();
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile("d", "a/b/c"));
            }
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
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            fileUtil.CleanupDirectory(dir);

            this.agent.Clear();
            IExecutionResult result;

            if (lang == Language.NodeJS)
            {
                this.agent.GetFile(this.fileShare, "a/b/.././b/../b/");
                result = this.agent.Invoke();
                this.agent.AssertNoError();

                result.AssertFileListItems(Enumerable.Empty<CloudFile>(), new[] { dir });
            }
            else
            {
                this.agent.GetFile(this.fileShare, "a/b/.././b/../b/c");
                result = this.agent.Invoke();
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory("a/b/c"));
            }

            this.agent.Clear();
            var file = fileUtil.CreateFile(dir, "d");
            if (lang == Language.NodeJS)
            {
                this.agent.GetFile(this.fileShare, "a/b/.././b/../b/c");
                result = this.agent.Invoke();
                this.agent.AssertNoError();

                result.AssertFileListItems(new[] { file }, Enumerable.Empty<CloudFileDirectory>());
            }
            else
            {
                this.agent.Clear();
                this.agent.GetFile(this.fileShare, "a/b/.././b/../b/c/d");
                result = this.agent.Invoke();
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile("d", "a/b/c"));
            }
        }

        /// <summary>
        /// Positive functional test cases 5.7.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void GetFilesFromSubDirectoryUsingRelativePathTest()
        {
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            fileUtil.CleanupDirectory(dir);
            var file = fileUtil.CreateFile(dir, "d");

            this.agent.Clear();
            this.agent.GetFile(dir.Parent, "../b/./c");
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory("a/b/c"));

            this.agent.Clear();
            this.agent.GetFile(dir.Parent, "../b/./c/d");
            result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFile("d", "a/b/c"));
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

            this.agent.GetFile(this.fileShare);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(Enumerable.Empty<CloudFile>(), Enumerable.Empty<CloudFileDirectory>());
        }

        /// <summary>
        /// Positive functional test cases 5.7.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void GetFilesToGetFileContent()
        {
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            fileUtil.CleanupDirectory(this.fileShare.GetRootDirectoryReference());
            string sourceFolder = Path.Combine(Test.Data.Get("TempDir"), Utility.GenNameString(""));
            FileUtil.CreateNewFolder(sourceFolder);
            var filePaths = fileNames.Select(name => Path.Combine(sourceFolder, name)).ToList();
            foreach(var filePath in filePaths)
            {
                FileUtil.GenerateSmallFile(filePath, Utility.GetRandomTestCount(5, 10), true);
            }

            var files = fileNames.Select(name => fileUtil.CreateFile(this.fileShare, name, Path.Combine(sourceFolder, name))).ToList();

            string destFolder = Path.Combine(Test.Data.Get("TempDir"), Utility.GenNameString(""));
            FileUtil.CreateNewFolder(destFolder);
            this.agent.GetFile(this.fileShare, fileNames[0]);
            ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("Get-AzureStorageFileContent");
            ((PowerShellAgent)this.agent).PowerShellSession.AddParameter("Destination", destFolder);
            this.agent.Invoke();
            this.agent.AssertNoError();
            string destMD5 = FileUtil.GetFileContentMD5(Path.Combine(destFolder, fileNames[0]));
            string srcMD5 = FileUtil.GetFileContentMD5(Path.Combine(sourceFolder, fileNames[0]));
            Test.Assert(destMD5.Equals(srcMD5), "Destination content should be the same with the source");

            FileUtil.CleanDirectory(destFolder);
            this.agent.Clear();
            this.agent.GetFile(this.fileShare);
            ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("Get-AzureStorageFileContent");
            ((PowerShellAgent)this.agent).PowerShellSession.AddParameter("Destination", destFolder);
            this.agent.Invoke();
            this.agent.AssertNoError();

            foreach (var fileName in fileNames)
            {
                destMD5 = FileUtil.GetFileContentMD5(Path.Combine(destFolder, fileNames[0]));
                srcMD5 = FileUtil.GetFileContentMD5(Path.Combine(sourceFolder, fileNames[0]));
                Test.Assert(destMD5.Equals(srcMD5), "Destination content should be the same with the source");
            }
        }

        /// <summary>
        /// Positive functional test cases 5.7.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void GetDirsToListDir()
        {
            fileUtil.CleanupDirectory(this.fileShare.GetRootDirectoryReference());
            var dirNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            fileUtil.CleanupDirectory(this.fileShare.GetRootDirectoryReference());
            var dirs = dirNames.Select(name => fileUtil.EnsureDirectoryExists(this.fileShare, name)).ToList();
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var files = fileNames.Select(name => fileUtil.CreateFile(dirs[0], name)).ToList();

            this.agent.GetFile(this.fileShare, dirNames[0]);
            ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("Get-AzureStorageFile");
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());

            foreach (var dir in dirs)
            {
                fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
                files.AddRange(fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList());
            }

            this.agent.Clear();
            this.agent.GetFile(this.fileShare);
            ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("Get-AzureStorageFile");
            result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
            fileUtil.CleanupDirectory(this.fileShare.GetRootDirectoryReference());
        }

        /// <summary>
        /// Nagitive functional test cases 5.7.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void GetNonExistFile()
        {
            fileUtil.CleanupDirectory(this.fileShare.GetRootDirectoryReference());

            this.agent.Clear();
            this.agent.GetFile(this.fileShare, "NonExistFile");
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            this.agent.Clear();
            this.agent.GetFile(this.fileShare.Name, "NonExistFile");
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            this.agent.Clear();
            this.agent.GetFile(this.fileShare.GetRootDirectoryReference(), "NonExistFile");
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.7.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void GetFileInNonExistShare()
        {
            this.agent.Clear();
            this.agent.GetFile("nonexistshare", "NonExistFile");
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            this.agent.Clear();
            this.agent.GetFile(fileUtil.GetShareReference("nonexistshare"), "NonExistFile");
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            this.agent.Clear();
            this.agent.GetFile(this.fileShare.GetRootDirectoryReference().GetDirectoryReference("NonExistDir"), "NonExistFile");
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            this.agent.Clear();
            this.agent.GetFile("nonexistshare");
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ShareNotFoundFullQualifiedErrorId));

            this.agent.Clear();
            this.agent.GetFile(fileUtil.GetShareReference("nonexistshare"));
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ShareNotFoundFullQualifiedErrorId));

            this.agent.Clear();
            this.agent.GetFile(this.fileShare.GetRootDirectoryReference().GetDirectoryReference("NonExistDir"));
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));
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

            this.agent.GetFile(invalidFileShareName);
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

            this.agent.GetFile(fileUtil.Client.GetShareReference(invalidFileShareName));
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
            this.agent.GetFile(this.fileShare, "../a");
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
            var fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 10)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            fileUtil.CleanupDirectory(dir);

            this.agent.Clear();
            IExecutionResult result;

            if (lang == Language.NodeJS)
            {
                this.agent.GetFile(this.fileShare, "a/c/./../b/./e/..");
                result = this.agent.Invoke();
                this.agent.AssertNoError();

                result.AssertFileListItems(Enumerable.Empty<CloudFile>(), new[] { dir });
            }
            else
            {
                this.agent.GetFile(this.fileShare, "a/c/./../b/./c/e/..");
                result = this.agent.Invoke();
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory("a/b/c"));
            }

            this.agent.Clear();
            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();

            if (lang == Language.NodeJS)
            {
                this.agent.GetFile(this.fileShare, "a/c/./../b/./c/e/..");
                result = this.agent.Invoke();
                this.agent.AssertNoError();

                result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
            }
            else
            {
                this.agent.Clear();
                this.agent.GetFile(this.fileShare, string.Format("a/c/./../b/./c/e/../{0}", fileNames[0]));
                result = this.agent.Invoke();
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile(fileNames[0], "a/b/c"));

            }
        }
    }
}
