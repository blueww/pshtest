namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
    public class NewAzureStorageFileDirectoryTest : TestBase
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
        /// Positive functional test case 5.5.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void PipelineMultipleDirectoryNamesTest()
        {
            // TODO: Generate more random names for file shares after the
            // naming rules is settled down.
            int numberOfDirectories = this.randomProvider.Next(2, 33);
            string[] names = Enumerable.Range(0, numberOfDirectories)
                    .Select(i => CloudFileUtil.GenerateUniqueDirectoryName()).ToArray();

            this.agent.NewDirectoryFromPipeline(this.fileShare.Name);
            var result = this.agent.Invoke(names);

            this.agent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory(new List<string>(names)), numberOfDirectories);
        }

        /// <summary>
        /// Positive functional test case 5.5.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateDirectoryWhichHasJustBeenDeleted()
        {
            string dirName = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = fileUtil.EnsureDirectoryExists(this.fileShare, dirName);
            directory.Delete();
            this.CreateDirectoryInternal(dirName);
        }

        /// <summary>
        /// Positive functional test case 5.5.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryUnderExistingDirectory()
        {
            string dirName = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = fileUtil.EnsureDirectoryExists(this.fileShare, dirName);
            string subDirName = CloudFileUtil.GenerateUniqueDirectoryName();
            string fullPath = CloudFileUtil.GetFullPath(directory.GetDirectoryReference(subDirName));
            this.CreateDirectoryInternal(
                () => this.agent.NewDirectory(directory, subDirName),
                fullPath.TrimEnd('/'));
        }

        /// <summary>
        /// Positive functional test case 5.5.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateDirectoryWith255Unicodes()
        {
            if (OSType.Windows != AgentFactory.GetOSType())
            {
                return;
            }

            foreach (var dirName in FileNamingGenerator.GenerateValidateUnicodeName(FileNamingGenerator.MaxFileNameLength))
            {
                this.CreateDirectoryInternal(dirName, traceCommand: false);
                this.agent.Clear();
            }
        }

        /// <summary>
        /// Positive functional test case 5.5.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateDirectoryWith255ASCIIChars()
        {
            string dirName = FileNamingGenerator.GenerateValidateASCIIName(FileNamingGenerator.MaxFileNameLength);
            this.CreateDirectoryInternal(dirName);
        }

        /// <summary>
        /// Positive functional test case 5.5.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryPipeline()
        {
            string dir1 = CloudFileUtil.GenerateUniqueDirectoryName();
            string dir2 = CloudFileUtil.GenerateUniqueDirectoryName();
            string fullPathForDir2 = string.Concat(dir1, "/", dir2);
            this.agent.NewDirectory(this.fileShare, dir1);
            ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("New-AzureStorageDirectory");
            ((PowerShellAgent)this.agent).PowerShellSession.AddParameter("Path", dir2);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory(fullPathForDir2));
            fileUtil.AssertDirectoryExists(this.fileShare, dir1, "Base directory should be created.");
            fileUtil.AssertDirectoryExists(this.fileShare, fullPathForDir2, "Sub directory should be created.");
        }

        /// <summary>
        /// Positive functional test case 5.5.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateChainsOfDirectories()
        {
            StringBuilder expectedPathBuilder = new StringBuilder("a");
            this.agent.NewDirectory(this.fileShare, "a");
            for (int i = 1; i < 250; i++)
            {
                ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("New-AzureStorageDirectory");
                ((PowerShellAgent)this.agent).PowerShellSession.AddParameter("Path", "a");
                expectedPathBuilder.Append("/a");
            }

            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory(expectedPathBuilder.ToString()));
        }

        /// <summary>
        /// Positive functional test case 5.5.9
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateDirectoryWithPathStartsWithSlash()
        {
            string dir1 = CloudFileUtil.GenerateUniqueDirectoryName();
            this.CreateDirectoryInternal("/" + dir1);
            this.agent.Clear();

            string dir2 = CloudFileUtil.GenerateUniqueDirectoryName();
            this.CreateDirectoryInternal("\\" + dir2);
            this.agent.Clear();
        }

        /// <summary>
        /// Positive functional test case 5.5.10
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryWithRelativePathStartsWithSlash()
        {
            var baseDir = fileUtil.EnsureDirectoryExists(this.fileShare, CloudFileUtil.GenerateUniqueDirectoryName());

            string dir1 = CloudFileUtil.GenerateUniqueDirectoryName();
            this.CreateDirectoryInternal(
                () => this.agent.NewDirectory(baseDir, "/" + dir1),
                baseDir.Name + "/" + dir1);
            this.agent.Clear();

            string dir2 = CloudFileUtil.GenerateUniqueDirectoryName();
            this.CreateDirectoryInternal(
                () => this.agent.NewDirectory(baseDir, "\\" + dir2),
                baseDir.Name + "/" + dir2);
            this.agent.Clear();
        }

        /// <summary>
        /// Positive functional test case 5.5.11
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryWithRelativePath()
        {
            var baseDir = fileUtil.EnsureDirectoryExists(this.fileShare, CloudFileUtil.GenerateUniqueDirectoryName());

            string dir1 = CloudFileUtil.GenerateUniqueDirectoryName();
            this.CreateDirectoryInternal(
                () => this.agent.NewDirectory(baseDir, "../" + dir1),
                dir1);
            this.agent.Clear();

            string dir2 = CloudFileUtil.GenerateUniqueDirectoryName();
            this.CreateDirectoryInternal(
                () => this.agent.NewDirectory(baseDir, "./c/../../" + dir2),
                dir2);
            this.agent.Clear();
        }

        /// <summary>
        /// Positive functional test case 5.5.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryWithRelativePathContainsDoubleSlash()
        {
            var baseDir = fileUtil.EnsureDirectoryExists(this.fileShare, CloudFileUtil.GenerateUniqueDirectoryName());

            string dir1 = CloudFileUtil.GenerateUniqueDirectoryName();
            this.CreateDirectoryInternal(
                () => this.agent.NewDirectory(baseDir, "..//" + dir1),
                dir1);
            this.agent.Clear();

            baseDir.GetDirectoryReference("a").CreateIfNotExists();
            string dir2 = CloudFileUtil.GenerateUniqueDirectoryName();
            this.CreateDirectoryInternal(
                () => this.agent.NewDirectory(baseDir, @"//a\\" + dir2),
                string.Format("{0}/a/{1}", baseDir.Name, dir2));
            this.agent.Clear();
        }

        /// <summary>
        /// Negative functional test case 5.5.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateDirectoryUnderNonExistingShare()
        {
            string shareName = CloudFileUtil.GenerateUniqueFileShareName();
            string dirName = CloudFileUtil.GenerateUniqueDirectoryName();
            fileUtil.DeleteFileShareIfExists(shareName);
            this.CreateDirectoryInternal(
                () => this.agent.NewDirectory(shareName, dirName),
                dirName,
                false);
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.ShareNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.5.2 with invalid account
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void NewDirectoryWithInvalidAccountTest()
        {
            string dir = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = this.fileShare.GetRootDirectoryReference().GetDirectoryReference(dir);
            directory.DeleteIfExists();

            // Creates an storage context object with invalid account
            // name.
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountName: true);
            object invalidStorageContextObject = this.agent.CreateStorageContextObject(invalidAccount.ToString(true));
            this.agent.NewDirectory(this.fileShare.Name, dir, invalidStorageContextObject);
            var result = this.agent.Invoke();
            this.agent.AssertErrors(record => record.AssertError(AssertUtil.AccountIsDisabledFullQualifiedErrorId, AssertUtil.NameResolutionFailureFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId, AssertUtil.InvalidResourceFullQualifiedErrorId));
            fileUtil.AssertDirectoryNotExists(this.fileShare, dir, "Directory should not be created when providing invalid credentials.");
        }

        /// <summary>
        /// Negative functional test case 5.5.2 with invalid key value
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void NewDirectoryWithInvalidKeyValueTest()
        {
            string dir = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = this.fileShare.GetRootDirectoryReference().GetDirectoryReference(dir);
            directory.DeleteIfExists();

            // Creates an storage context object with invalid key value
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountKey: true);
            object invalidStorageContextObject = this.agent.CreateStorageContextObject(invalidAccount.ToString(true));
            this.agent.NewDirectory(this.fileShare.Name, dir, invalidStorageContextObject);
            var result = this.agent.Invoke();
            this.agent.AssertErrors(record => record.AssertError(AssertUtil.AuthenticationFailedFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId));
            fileUtil.AssertDirectoryNotExists(this.fileShare, dir, "Directory should not be created when providing invalid credentials.");
        }

        /// <summary>
        /// Negative functional test case 5.5.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateExistingDirectoryTest()
        {
            string dirName = CloudFileUtil.GenerateUniqueDirectoryName();
            var directory = fileUtil.EnsureDirectoryExists(this.fileShare, dirName);
            this.agent.NewDirectory(this.fileShare, dirName);
            var result = this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.ResourceAlreadyExistsFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.5.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void PipelineCreatingMultipleDirectoriesWhileSomeDirectoryExistsTest()
        {
            int numberOfDirectories = this.randomProvider.Next(2, 33);
            int[] indexes = Enumerable.Range(0, numberOfDirectories).ToArray();
            string[] names = indexes.Select(i => CloudFileUtil.GenerateUniqueDirectoryName()).ToArray();
            int numberOfExistingDirectories = this.randomProvider.Next(1, numberOfDirectories);

            List<int> indexToBeRemoved = new List<int>(indexes);
            for (int i = 0; i < numberOfExistingDirectories; i++)
            {
                int id = this.randomProvider.Next(indexToBeRemoved.Count);
                fileUtil.EnsureDirectoryExists(this.fileShare, names[indexToBeRemoved[id]]);
                indexToBeRemoved.RemoveAt(id);
            }

            this.agent.NewDirectoryFromPipeline(this.fileShare.Name);
            var result = this.agent.Invoke(names);

            // A total number of "numberOfExistingDirectories" errors should throw while others should success.
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.ResourceAlreadyExistsFullQualifiedErrorId), numberOfExistingDirectories);

            // Assert all directories are created
            foreach (string name in names)
            {
                fileUtil.AssertDirectoryExists(this.fileShare, name, string.Format("Directory {0} should exist after created.", name));
            }
        }

        /// <summary>
        /// Negative functional test case 5.5.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateDirectoryUnderNonExistingFileShareTest()
        {
            string nonExistingFileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.DeleteFileShareIfExists(nonExistingFileShareName);
            string dirName = CloudFileUtil.GenerateUniqueDirectoryName();
            this.agent.NewDirectory(nonExistingFileShareName, dirName);
            var result = this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.ShareNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.5.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateDirectoryWith256Chars()
        {
            string dirName = FileNamingGenerator.GenerateValidateASCIIName(FileNamingGenerator.MaxFileNameLength + 1);
            this.CreateDirectoryInternal(dirName, false);
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.5.8
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateDirectoryWithInvalidCharacters()
        {
            string dirName = FileNamingGenerator.GenerateASCIINameWithInvalidCharacters(this.randomProvider.Next(3, FileNamingGenerator.MaxFileNameLength + 1));
            this.CreateDirectoryInternal(dirName, false);
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.5.9
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateCasePreservingExistingDirectoryTest()
        {
            int length = this.randomProvider.Next(5, 50);
            string dirName = FileNamingGenerator.GenerateNameFromRange(length, new Tuple<int, int>((int)'a', (int)'z'));
            var directory = fileUtil.EnsureDirectoryExists(this.fileShare, dirName);

            // Randomly up case some letters
            StringBuilder sb = new StringBuilder(dirName);
            for (int i = 0; i < sb.Length; i++)
            {
                // 1/3 chance to up case the letters
                if (this.randomProvider.Next(3) == 0)
                {
                    sb[i] = Char.ToUpperInvariant(sb[i]);
                }
            }

            string newDirName = sb.ToString();

            Test.Info("Original dir name: {0}. New dir name: {1}.", dirName, newDirName);
            this.agent.NewDirectory(this.fileShare, newDirName);
            var result = this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.ResourceAlreadyExistsFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.5.10
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateChainsOfDirectoriesWithPathLengthTo1024()
        {
            // Create a 250 level depth chains of folder with each directory
            // named "123". And the first one is named "1234567890123456789012345678"
            // which is 28 characters long. So the total length would be
            // 249*(1+3)+28=1024.
            StringBuilder expectedPathBuilder = new StringBuilder("1234567890123456789012345678");
            this.agent.NewDirectory(this.fileShare, "1234567890123456789012345678");
            for (int i = 1; i < 250; i++)
            {
                ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("New-AzureStorageDirectory");
                ((PowerShellAgent)this.agent).PowerShellSession.AddParameter("Path", "123");
                expectedPathBuilder.Append("/123");
            }

            string expectedPath = expectedPathBuilder.ToString();
            Test.Assert(expectedPath.Length == 1024, "Generated path should be 1024 while it is {0}.", expectedPath.Length);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory(expectedPath));
        }

        /// <summary>
        /// Negative functional test case 5.5.11
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateChainsOfDirectoriesWithDepthTo251()
        {
            StringBuilder expectedPathBuilder = new StringBuilder("a");
            this.agent.NewDirectory(this.fileShare, "a");
            for (int i = 1; i < 251; i++)
            {
                ((PowerShellAgent)this.agent).PowerShellSession.AddCommand("New-AzureStorageDirectory");
                ((PowerShellAgent)this.agent).PowerShellSession.AddParameter("Path", "a");
                expectedPathBuilder.Append("/a");
            }

            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidFileOrDirectoryPathNameFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.5.14
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateDirectoryUnderRootsParent()
        {
            string dirName = CloudFileUtil.GenerateUniqueDirectoryName();
            this.CreateDirectoryInternal("../" + dirName, false);
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidResourceFullQualifiedErrorId, AssertUtil.AuthenticationFailedFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.5.15
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryWithRelativePathWhereIntermediatePathMightNotExist()
        {
            var dir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            this.CreateDirectoryInternal(
                () => this.agent.NewDirectory(dir, "d/../e/./../../f"),
                "a/b/f");
        }

        private void CreateDirectoryInternal(string dirName, bool assertForSuccess = true, bool traceCommand = true)
        {
            this.CreateDirectoryInternal(() => this.agent.NewDirectory(this.fileShare, dirName), dirName, assertForSuccess, traceCommand);
        }

        private void CreateDirectoryInternal(Action newDirectoryAction, string dirName, bool assertForSuccess = true, bool traceCommand = true)
        {
            newDirectoryAction();
            var result = this.agent.Invoke(traceCommand: traceCommand);
            if (assertForSuccess)
            {
                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => result.AssertCloudFileDirectory(obj, dirName), 1);
                fileUtil.AssertDirectoryExists(this.fileShare, dirName.Trim(CloudFileUtil.PathSeparators), "Directory should exist after creation.");
            }
        }
    }
}
