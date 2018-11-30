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
            this.fileShare = fileUtil.EnsureFileShareExists(CloudFileUtil.GenerateUniqueFileShareName());
        }

        public override void OnTestCleanUp()
        {
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
            CommandAgent.GetFileShareByName(this.fileShare.Name);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzStorageFile");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzureStorageFile");
#endif
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddParameter("Path", directoryName);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory(directoryName));

            CommandAgent.Clear();
            var fileName = CloudFileUtil.GenerateUniqueFileName();
            var file = fileUtil.CreateFile(this.fileShare.GetRootDirectoryReference(), fileName);
            CommandAgent.GetFileShareByName(this.fileShare.Name);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzStorageFile");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzureStorageFile");
#endif
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddParameter("Path", fileName);
            result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFile(fileName));

            CommandAgent.Clear();
            List<CloudFileDirectory> dirs = new List<CloudFileDirectory>();
            List<CloudFile> files = new List<CloudFile>();
            dirs.Add(directory);
            files.Add(file);
            CommandAgent.GetFileShareByName(this.fileShare.Name);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzStorageFile");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzureStorageFile");
#endif
            result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
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

            CommandAgent.Clear();
            CommandAgent.GetFile(this.fileShare);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertFileListItems(files, dirs);

            // xPlat doesn't have the "file show" command. It only has the "file list" command which is only target for the directory
            if (lang != Language.NodeJS)
            {
                CommandAgent.Clear();
                CommandAgent.GetFile(this.fileShare, fileNames[0]);
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile(fileNames[0]));

                CommandAgent.Clear();
                CommandAgent.GetFile(this.fileShare.Name, fileNames[0]);
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile(fileNames[0]));

                CommandAgent.Clear();
                CommandAgent.GetFile(this.fileShare.GetRootDirectoryReference(), fileNames[0]);
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
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

            CommandAgent.GetFile(dir);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
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

            CommandAgent.Clear();
            IExecutionResult result;

            if (lang == Language.NodeJS)
            {
                CommandAgent.GetFile(this.fileShare, "/a/b");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();

                result.AssertFileListItems(Enumerable.Empty<CloudFile>(), new []{ dir });
            }
            else
            {
                CommandAgent.GetFile(this.fileShare, "/a/b/c");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory("a/b/c"));
            }

            CommandAgent.Clear();
            var file = fileUtil.CreateFile(dir, "d");

            if (lang == Language.NodeJS)
            {
                CommandAgent.GetFile(this.fileShare, "/a/b/c");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();

                result.AssertFileListItems(new [] { file }, Enumerable.Empty<CloudFileDirectory>());
            }
            else
            {
                CommandAgent.GetFile(this.fileShare, "/a/b/c/d");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
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

            CommandAgent.Clear();
            IExecutionResult result;

            if (lang == Language.NodeJS)
            {
                CommandAgent.GetFile(this.fileShare, "a/b/.././b/../b/");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();

                result.AssertFileListItems(Enumerable.Empty<CloudFile>(), new[] { dir });
            }
            else
            {
                CommandAgent.GetFile(this.fileShare, "a/b/.././b/../b/c");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory("a/b/c"));
            }

            CommandAgent.Clear();
            var file = fileUtil.CreateFile(dir, "d");
            if (lang == Language.NodeJS)
            {
                CommandAgent.GetFile(this.fileShare, "a/b/.././b/../b/c");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();

                result.AssertFileListItems(new[] { file }, Enumerable.Empty<CloudFileDirectory>());
            }
            else
            {
                CommandAgent.Clear();
                CommandAgent.GetFile(this.fileShare, "a/b/.././b/../b/c/d");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
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

            CommandAgent.Clear();
            CommandAgent.GetFile(dir.Parent, "../b/./c");
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory("a/b/c"));

            CommandAgent.Clear();
            CommandAgent.GetFile(dir.Parent, "../b/./c/d");
            result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
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

            CommandAgent.GetFile(this.fileShare);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
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
            CommandAgent.GetFile(this.fileShare, fileNames[0]);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzStorageFileContent");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzureStorageFileContent");
#endif
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddParameter("Destination", destFolder);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            string destMD5 = FileUtil.GetFileContentMD5(Path.Combine(destFolder, fileNames[0]));
            string srcMD5 = FileUtil.GetFileContentMD5(Path.Combine(sourceFolder, fileNames[0]));
            Test.Assert(destMD5.Equals(srcMD5), "Destination content should be the same with the source");

            FileUtil.CleanDirectory(destFolder);
            CommandAgent.Clear();
            CommandAgent.GetFile(this.fileShare);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzStorageFileContent");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzureStorageFileContent");
#endif
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddParameter("Destination", destFolder);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();

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

            CommandAgent.GetFile(this.fileShare, dirNames[0]);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzStorageFile");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzureStorageFile");
#endif
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());

            foreach (var dir in dirs)
            {
                fileNames = Enumerable.Range(0, this.randomProvider.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
                files.AddRange(fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList());
            }

            CommandAgent.Clear();
            CommandAgent.GetFile(this.fileShare);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzStorageFile");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzureStorageFile");
#endif
            result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
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

            CommandAgent.Clear();
            CommandAgent.GetFile(this.fileShare, "NonExistFile");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            CommandAgent.Clear();
            CommandAgent.GetFile(this.fileShare.Name, "NonExistFile");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            CommandAgent.Clear();
            CommandAgent.GetFile(this.fileShare.GetRootDirectoryReference(), "NonExistFile");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
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
            CommandAgent.Clear();
            CommandAgent.GetFile("nonexistshare", "NonExistFile");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            CommandAgent.Clear();
            CommandAgent.GetFile(fileUtil.GetShareReference("nonexistshare"), "NonExistFile");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            CommandAgent.Clear();
            CommandAgent.GetFile(this.fileShare.GetRootDirectoryReference().GetDirectoryReference("NonExistDir"), "NonExistFile");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId));

            CommandAgent.Clear();
            CommandAgent.GetFile("nonexistshare");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
                AssertUtil.ShareNotFoundFullQualifiedErrorId));

            CommandAgent.Clear();
            CommandAgent.GetFile(fileUtil.GetShareReference("nonexistshare"));
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
                AssertUtil.ShareNotFoundFullQualifiedErrorId));

            CommandAgent.Clear();
            CommandAgent.GetFile(this.fileShare.GetRootDirectoryReference().GetDirectoryReference("NonExistDir"));
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
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

            CommandAgent.GetFile(invalidFileShareName);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
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

            CommandAgent.GetFile(fileUtil.Client.GetShareReference(invalidFileShareName));
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(
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
            CommandAgent.GetFile(this.fileShare, "../a");
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidResourceFullQualifiedErrorId, AssertUtil.AuthenticationFailedFullQualifiedErrorId));
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

            CommandAgent.Clear();
            IExecutionResult result;

            if (lang == Language.NodeJS)
            {
                CommandAgent.GetFile(this.fileShare, "a/c/./../b/./e/..");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();

                result.AssertFileListItems(Enumerable.Empty<CloudFile>(), new[] { dir });
            }
            else
            {
                CommandAgent.GetFile(this.fileShare, "a/c/./../b/./c/e/..");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory("a/b/c"));
            }

            CommandAgent.Clear();
            var files = fileNames.Select(name => fileUtil.CreateFile(dir, name)).ToList();

            if (lang == Language.NodeJS)
            {
                CommandAgent.GetFile(this.fileShare, "a/c/./../b/./c/e/..");
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();

                result.AssertFileListItems(files, Enumerable.Empty<CloudFileDirectory>());
            }
            else
            {
                CommandAgent.Clear();
                CommandAgent.GetFile(this.fileShare, string.Format("a/c/./../b/./c/e/../{0}", fileNames[0]));
                result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile(fileNames[0], "a/b/c"));

            }
        }
    }
}
