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
    public class GetAzureStorageFileContentTest : TestBase
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
        /// Positive functional test case 5.8.1
        /// </summary>
        /// <remarks>
        /// RDTask 1414230:
        /// Test case DownloadExistingFile requires a 200G file already exist
        /// on the storage account in the file service. It does not make sense
        /// to generate a 200G file at runtime, upload it and download it in
        /// functional test cases.
        /// Currently, since the only test we could do is against dev fabric,
        /// we could not prepare such file on the cloud first. So the test case
        /// is temp blocked.
        /// Once the xSMB file service GAed, and we are testing against real
        /// storage accounts, we could put this case back online.
        /// </remarks>
        ////[TestMethod]
        ////[TestCategory(PsTag.File)]
        public void DownloadExistingFileTest()
        {
            string[] filesToDownload = Test.Data.Get("ExistingFilesToDownload").Split(';');
            string[] md5Checksum = Test.Data.Get("ExistingFilesMD5").Split(';');

            for (int i = 0; i < filesToDownload.Length; i++)
            {
                var destination = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());
                try
                {
                    Test.Info("About to download: {0} to {1}", filesToDownload[i], destination);
                    CommandAgent.DownloadFile(this.fileShare, filesToDownload[i], destination);
                    var result = CommandAgent.Invoke();
                    CommandAgent.AssertNoError();
                    result.AssertNoResult();

                    string destinationMD5 = FileUtil.GetFileContentMD5(destination);
                    Test.Assert(
                        destinationMD5.Equals(md5Checksum[i], StringComparison.Ordinal),
                        "MD5 checksum of the downloaded file mismatches. Expected: {0}, Actural: {1}.",
                        md5Checksum[i],
                        destinationMD5);
                }
                finally
                {
                    FileUtil.RemoveFile(destination);
                }
            }
        }

        /// <summary>
        /// Positive functional test case 5.8.3
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadFileUsingFileShareNameParameterSet()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudFileName, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(this.fileShare.Name, cloudFileName, destination, true));
        }

        /// <summary>
        /// Download a batch of files from the listing results.
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void DownloadBatchFilesFromListingResult()
        {
            int numberOfFilesToDownload = this.randomProvider.Next(5, 20);
            List<CloudFile> files = new List<CloudFile>();
            List<string> fileNames = new List<string>();
            for (int i = 0; i < numberOfFilesToDownload; i++)
            {
                string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
                string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
                FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
                files.Add(fileUtil.CreateFile(this.fileShare, cloudFileName, localFilePath));
                File.Delete(localFilePath);
                fileNames.Add(cloudFileName.ToLowerInvariant());
            }

            DirectoryInfo localDir = new DirectoryInfo(Test.Data.Get("TempDir"));

            CommandAgent.GetFile(this.fileShare);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzStorageFileContent");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddCommand("Get-AzureStorageFileContent");
#endif
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddParameter("Destination", localDir.FullName);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();

            var localFilesInfo = localDir.GetFiles();
            foreach (var fileInfo in localFilesInfo)
            {
                fileNames.Remove(fileInfo.Name.ToLowerInvariant());
            }

            Test.Assert(fileNames.Count == 0, "All files should be downloaded while missing: {0}", string.Join(",", fileNames));
        }

        /// <summary>
        /// Positive functional test case 5.8.5
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadFileInASubDirectory()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            var subDirectory = fileUtil.EnsureDirectoryExists(this.fileShare, directoryName);
            var sourceFile = fileUtil.CreateFile(subDirectory, cloudFileName, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(sourceFile, destination, true));
        }

        /// <summary>
        /// Positive functional test case 5.8.8
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadToAFolderAndOverwrite()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            string localExistingPath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            FileUtil.GenerateSmallFile(localExistingPath, Utility.GetRandomTestCount(5, 10), true);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudFileName, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(sourceFile, destination, true),
                () => Path.GetDirectoryName(localExistingPath));
        }

        /// <summary>
        /// Positive functional test case 5.8.9
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadToAFilePathAndOverwrite()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            string localExistingPath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            FileUtil.GenerateSmallFile(localExistingPath, Utility.GetRandomTestCount(5, 10), true);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudFileName, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(sourceFile, destination, true),
                () => localExistingPath);
        }

        /// <summary>
        /// Positive functional test case 5.8.10
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadUsingPath()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "/a/b/c/" + cloudFileName;
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudPath, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(this.fileShare, cloudPath, destination, true));
        }

        /// <summary>
        /// Positive functional test case 5.8.11
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadUsingRelativePathFromRoot()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "a/b/c/" + cloudFileName;
            string relativeCloudPath = "a/b/../b/./c/" + cloudFileName;
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudPath, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(this.fileShare, relativeCloudPath, destination, true));
        }

        /// <summary>
        /// Positive functional test case 5.8.12
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void DownloadUsingRelativePathFromDirectoryObject()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "a/b/c/" + cloudFileName;
            string relativeCloudPath = "../../b/./c/" + cloudFileName;
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudPath, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(baseDir, relativeCloudPath, destination, true));
        }

        /// <summary>
        /// Positive functional test case 5.8.14
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void DownloadFileUsingRelativePathAfterChangedDefaultLocation()
        {
            string currentPath = CommandAgent.GetCurrentLocation();
            try
            {
                string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
                string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
                FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
                var sourceFile = fileUtil.CreateFile(this.fileShare, cloudFileName, localFilePath);
                CommandAgent.ChangeLocation(Test.Data.Get("TempDir"));
                UploadAndDownloadFileInternal(
                    sourceFile,
                    FileUtil.GetFileContentMD5(localFilePath),
                    destination => CommandAgent.DownloadFile(this.fileShare, cloudFileName, ".", true));
                var result = CommandAgent.Invoke();
                result.AssertNoResult();
                Test.Assert(new FileInfo(Path.Combine(Test.Data.Get("TempDir"), cloudFileName)).Exists, "File should exist after downloaded.");
            }
            finally
            {
                CommandAgent.Clear();
                CommandAgent.ChangeLocation(currentPath);
            }
        }

        /// <summary>
        /// Negative functional test case 5.8.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadingNonExistingFile()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            var file = fileUtil.DeleteFileIfExists(this.fileShare, cloudFileName);
            CommandAgent.DownloadFile(file, Test.Data.Get("TempDir"), true);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidOperationExceptionFullQualifiedErrorId, AssertUtil.PathNotFoundFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.8.2 with invalid account
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadWithInvalidAccountTest()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            var file = fileUtil.DeleteFileIfExists(this.fileShare, cloudFileName);

            // Creates an storage context object with invalid account
            // name.
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountName: true);
            object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
            CommandAgent.DownloadFile(this.fileShare.Name, file.Name, Test.Data.Get("TempDir"), true, invalidStorageContextObject);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AccountIsDisabledFullQualifiedErrorId, AssertUtil.NameResolutionFailureFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId, AssertUtil.InvalidResourceFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.8.2 with invalid key value
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadWithInvalidKeyValueTest()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            var file = fileUtil.DeleteFileIfExists(this.fileShare, cloudFileName);

            // Creates an storage context object with invalid key value
            var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountKey: true);
            object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
            CommandAgent.DownloadFile(this.fileShare.Name, file.Name, Test.Data.Get("TempDir"), true, invalidStorageContextObject);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AuthenticationFailedFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.8.3
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadingFromNonExistingFileShare()
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.DeleteFileShareIfExists(fileShareName);

            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            CommandAgent.DownloadFile(fileShareName, cloudFileName, Test.Data.Get("TempDir"), true);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidOperationExceptionFullQualifiedErrorId, AssertUtil.PathNotFoundFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.8.4
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadingFromNonExistingDirectory()
        {
            string cloudDirectoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            fileUtil.DeleteDirectoryIfExists(this.fileShare, cloudDirectoryName);
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            var file = this.fileShare.GetRootDirectoryReference().GetDirectoryReference(cloudDirectoryName).GetFileReference(cloudFileName);
            CommandAgent.DownloadFile(file, Test.Data.Get("TempDir"), true);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidOperationExceptionFullQualifiedErrorId, AssertUtil.PathNotFoundFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.8.5
        /// </summary>
        ////[TestMethod]
        ////[TestCategory(PsTag.File)]
        public void DownloadToAnExistingFilePath()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            string localExistingPath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            FileUtil.GenerateSmallFile(localExistingPath, Utility.GetRandomTestCount(5, 10), true);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudFileName, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(sourceFile, destination, false),
                () => localExistingPath,
                false);

            CommandAgent.AssertErrors(err => err.AssertError("IOException"));
        }

        /// <summary>
        /// Negative functional test case 5.8.6
        /// </summary>
        ////[TestMethod]
        ////[TestCategory(PsTag.File)]
        public void DownloadToAFolderAndOverwriteWithoutForceOption()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            string localExistingPath = Path.Combine(Test.Data.Get("TempDir"), cloudFileName);
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            FileUtil.GenerateSmallFile(localExistingPath, Utility.GetRandomTestCount(5, 10), true);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudFileName, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(sourceFile, destination, false),
                () => Path.GetDirectoryName(localExistingPath),
                false);

            CommandAgent.AssertErrors(err => err.AssertError("IOException"));
        }

        /// <summary>
        /// Negative functional test case 5.8.9
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadToAnNonExistingFilePath()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            string localExistingPath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            var destinationFolder = new DirectoryInfo(Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueDirectoryName()));
            if (destinationFolder.Exists)
            {
                destinationFolder.Delete(true);
            }

            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudFileName, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(sourceFile, destination, false),
                () => Path.Combine(destinationFolder.FullName, CloudFileUtil.GenerateUniqueFileName()),
                false);

            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.TransferExceptionFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.8.14
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadFileFromSubDirectoryOfRootTest()
        {
            CommandAgent.DownloadFile(this.fileShare, "../a", Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName()), true);
            CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidResourceFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test cases 5.8.15
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void DownloadFileFromRelativePathWhereIntermediatePathMightNotExistTest()
        {
            var baseDir = fileUtil.EnsureFolderStructure(this.fileShare, "a/b/c");
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string cloudPath = "a/b/c/" + cloudFileName;
            string relativeCloudPath = "../../ddd/../b/./c/" + cloudFileName;
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudPath, localFilePath);
            UploadAndDownloadFileInternal(
                sourceFile,
                FileUtil.GetFileContentMD5(localFilePath),
                destination => CommandAgent.DownloadFile(baseDir, relativeCloudPath, destination, true));
        }

        /// <summary>
        /// Positive functional test case 5.8.3
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        public void DownloadFileCheckMD5()
        {
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);

            string previousMD5 = FileUtil.GetFileContentMD5(localFilePath);
            var sourceFile = fileUtil.CreateFile(this.fileShare, cloudFileName, localFilePath);

            if (sourceFile.Properties.ContentMD5 != previousMD5)
            {
                sourceFile.Properties.ContentMD5 = previousMD5;
                sourceFile.SetProperties();
            }

            var destination = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            
            // Download file and check its MD5
            CommandAgent.DownloadFile(this.fileShare.Name, cloudFileName, destination, true, CheckMd5: true);
            var result = CommandAgent.Invoke();
            result.AssertNoResult();

            string destinationMD5 = FileUtil.GetFileContentMD5(destination);
            Test.Assert(
                destinationMD5.Equals(previousMD5, StringComparison.Ordinal),
                "MD5 checksum of the downloaded file mismatches. Expected: {0}, Actural: {1}.",
                previousMD5,
                destinationMD5);

            sourceFile.Properties.ContentMD5 = "";
            sourceFile.SetProperties();

            // File's ContentMD5 property is empty, download file and check its MD5
            CommandAgent.Clear();
            CommandAgent.DownloadFile(this.fileShare.Name, cloudFileName, destination, true, CheckMd5: true);
            result = CommandAgent.Invoke();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidOperationExceptionFullQualifiedErrorId));

            destinationMD5 = FileUtil.GetFileContentMD5(destination);
            Test.Assert(
                destinationMD5.Equals(previousMD5, StringComparison.Ordinal),
                "MD5 checksum of the downloaded file mismatches. Expected: {0}, Actural: {1}.",
                previousMD5,
                destinationMD5);
            
            // File's ContentMD5 property is empty, download file without check MD5.
            CommandAgent.Clear();
            CommandAgent.DownloadFile(this.fileShare.Name, cloudFileName, destination, true, CheckMd5: false);
            result = CommandAgent.Invoke();
            result.AssertNoResult();

            destinationMD5 = FileUtil.GetFileContentMD5(destination);
            Test.Assert(
                destinationMD5.Equals(previousMD5, StringComparison.Ordinal),
                "MD5 checksum of the downloaded file mismatches. Expected: {0}, Actural: {1}.",
                previousMD5,
                destinationMD5);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void DownloadFileFromShareSnapshot_share()
        {
            string shareName = CloudFileUtil.GenerateUniqueFileShareName();
            string fileName = CloudFileUtil.GenerateUniqueFileName();

            try
            {
                CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
                CloudFileShare shareSnapshot1 = share.Snapshot();
                CloudFile file = fileUtil.CreateFile(share, fileName);
                CloudFileShare shareSnapshot2 = share.Snapshot();
                file.Delete();
                
                //Get File content
                string StorageConnectionString = Test.Data.Get("StorageConnectionString");
#if NEW_CMDLET_NAME
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(New-AzStorageContext -ConnectionString \"{4}\") | Get-AzStorageShare -Name {0} -SnapshotTime \"{1}\" | Get-AzStorageFileContent -Path {2} -Destination {3} -Force",
#else
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(New-AzureStorageContext -ConnectionString \"{4}\") | Get-AzureStorageShare -Name {0} -SnapshotTime \"{1}\" | Get-AzureStorageFileContent -Path {2} -Destination {3} -Force",
#endif
                    shareName, 
                    shareSnapshot2.SnapshotTime.Value, 
                    fileName, 
                    fileName, 
                    StorageConnectionString)),
                    string.Format("Download File {0} from share snapshot {1}, {2} should success.", fileName, shareName, shareSnapshot2.SnapshotTime.Value));

                //validate MD5
                CloudFile file2 = shareSnapshot2.GetRootDirectoryReference().GetFileReference(fileName);
                file2.FetchAttributes();
                Test.Assert(file2.Properties.ContentMD5 == FileUtil.GetFileContentMD5(fileName), "Expected MD5: {0}, real MD5: {1}", file2.Properties.ContentMD5, FileUtil.GetFileContentMD5(fileName));
            }
            finally
            {
                try
                {
                    fileUtil.DeleteFileShareIfExists(shareName);
                }
                catch (Exception e)
                {
                    Test.Warn("Unexpected exception when cleanup file share {0}: {1}", shareName, e);
                }
            }            
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void DownloadFileFromShareSnapshot_dir()
        {
            string shareName = CloudFileUtil.GenerateUniqueFileShareName();
            string dirName = CloudFileUtil.GenerateUniqueDirectoryName();
            string fileName = CloudFileUtil.GenerateUniqueFileName();

            try
            {
                CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
                CloudFileShare shareSnapshot1 = share.Snapshot();
                CloudFileDirectory dir = fileUtil.EnsureDirectoryExists(share, dirName);
                CloudFile file = fileUtil.CreateFile(dir, fileName);
                CloudFileShare shareSnapshot2 = share.Snapshot();
                file.Delete();
                dir.Delete();

                //Get File content
                string StorageConnectionString = Test.Data.Get("StorageConnectionString");
#if NEW_CMDLET_NAME
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(New-AzStorageContext -ConnectionString \"{5}\" | Get-AzStorageShare -Name {0} -SnapshotTime \"{1}\").GetRootDirectoryReference().GetDirectoryReference(\"{4}\") | Get-AzStorageFileContent -Path {2} -Destination {3} -Force",
#else
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(New-AzureStorageContext -ConnectionString \"{5}\" | Get-AzureStorageShare -Name {0} -SnapshotTime \"{1}\").GetRootDirectoryReference().GetDirectoryReference(\"{4}\") | Get-AzureStorageFileContent -Path {2} -Destination {3} -Force",
#endif
                    shareName,
                    shareSnapshot2.SnapshotTime.Value,
                    fileName,
                    fileName,
                    dirName,
                    StorageConnectionString)),
                    string.Format("Download File {0} from share snapshot {1}, {2} should success.", dirName + "\\" + fileName, shareName, shareSnapshot2.SnapshotTime.Value));

                //validate MD5
                CloudFile file2 = shareSnapshot2.GetRootDirectoryReference().GetDirectoryReference(dirName).GetFileReference(fileName);
                file2.FetchAttributes();
                Test.Assert(file2.Properties.ContentMD5 == FileUtil.GetFileContentMD5(fileName), "Expected MD5: {0}, real MD5: {1}", file2.Properties.ContentMD5, FileUtil.GetFileContentMD5(fileName));
            }
            finally
            {
                try
                {
                    fileUtil.DeleteFileShareIfExists(shareName);
                }
                catch (Exception e)
                {
                    Test.Warn("Unexpected exception when cleanup file share {0}: {1}", shareName, e);
                }
            }
        }

        private void UploadAndDownloadFileInternal(CloudFile sourceFile, string md5Checksum, Action<string> getContentAction, Func<string> getDestination = null, bool assertNoError = true)
        {
            var destination = getDestination == null ? Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName()) : getDestination();

            try
            {
                Test.Info("Download source file {0} to destination {1}.", sourceFile.Uri.OriginalString, destination);
                getContentAction(destination);
                var result = CommandAgent.Invoke();

                if (assertNoError)
                {
                    CommandAgent.AssertNoError();
                    if (lang == Language.NodeJS)
                    {
                        result.AssertObjectCollection(obj => result.AssertCloudFile(obj, CloudFileUtil.GetFullPath(sourceFile)));
                    }
                    else
                    {
                        result.AssertNoResult();
                    }

                    if (File.Exists(destination))
                    {
                        string destinationMD5 = FileUtil.GetFileContentMD5(destination);
                        Test.Assert(
                            destinationMD5.Equals(md5Checksum, StringComparison.Ordinal),
                            "MD5 checksum of the downloaded file mismatches. Expected: {0}, Actural: {1}.",
                            md5Checksum,
                            destinationMD5);
                    }
                }
            }
            finally
            {
                if (File.Exists(destination))
                {
                    FileUtil.RemoveFile(destination);
                }
            }
        }
    }
}
