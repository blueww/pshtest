namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    public class MountTest : TestBase
    {
        private const int RetryIntervalForSMBValidation = 1000;

        private const int RetryLimitForSMBValidation = 10;

        private static string accountName;

        private static string fileEndpoint;

        private static string accountKey;

        private Random randomProvider = new Random();

        private CloudFileShare fileShare;

        private DirectoryInfo mountedShareRoot;

        [ClassInitialize]
        public static void MountTestInitialize(TestContext context)
        {
            StorageAccount = GetCloudStorageAccountFromConfig();
            accountName = StorageAccount.Credentials.AccountName;
            accountKey = StorageAccount.Credentials.ExportBase64EncodedKey();
            fileEndpoint = StorageAccount.FileEndpoint.DnsSafeHost;
            TestBase.TestClassInitialize(context);
        }

        [ClassCleanup]
        public static void MountTestCleanup()
        {
            TestBase.TestClassCleanup();
        }

        public override void OnTestSetup()
        {
            this.fileShare = fileUtil.EnsureFileShareExists(CloudFileUtil.GenerateUniqueFileShareName());
            this.mountedShareRoot = MountShare(this.fileShare.Name);
        }

        public override void OnTestCleanUp()
        {
            if (this.mountedShareRoot != null)
            {
                DismountShare(this.mountedShareRoot);
            }

            fileUtil.DeleteFileShareIfExists(this.fileShare.Name);
        }

        /// <summary>
        /// Positive functional test case 5.2.7
        /// Test the scenario to mount a share
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        public void CreateShareAndMountTest()
        {
            // Test case passes if successfully finished test init and test
            // clean up.
        }

        /// <summary>
        /// Positive functional test case 5.5.13
        /// Test the scenario to mount a share, create a directory through
        /// psh and validate through SMB protocol.
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        public void CreateDirectoryAndListThroughSMBTest()
        {
            string directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            CommandAgent.NewDirectory(this.fileShare, directoryName);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            SMBValidationRetry(
                () => this.mountedShareRoot.GetDirectories().Select(x => x.Name).Contains(directoryName),
                "list the newly created directory through SMB protocol");
        }

        /// <summary>
        /// Positive functional test case 5.11.1
        /// Test the scenario to mount a share, create a directory through
        /// SMB and validate through PSH.
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        public void CreateDirectoryAndListThroughPSHTest()
        {
            string directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            this.mountedShareRoot.CreateSubdirectory(directoryName);
            CommandAgent.GetFile(this.fileShare);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFileDirectory(directoryName));
        }

        /// <summary>
        /// Positive functional test case 5.9.11
        /// Test the scenario to mount a share, create a file through
        /// psh and validate through SMB protocol.
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        public void CreateFileAndListThroughSMBTest()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            FileUtil.GenerateSmallFile(localFilePath, Utility.GetRandomTestCount(5, 10), true);
            CommandAgent.UploadFile(this.fileShare, localFilePath, fileName);
            CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            SMBValidationRetry(
                () => this.mountedShareRoot.GetFiles().Select(x => x.Name).Contains(fileName),
                "list the newly created directory through SMB protocol");
        }

        /// <summary>
        /// Positive functional test case 5.11.2
        /// Test the scenario to mount a share, create a file through
        /// SMB and validate through PSH.
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        public void CreateFileAndListThroughPSHTest()
        {
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            string localFilePath = Path.Combine(Test.Data.Get("TempDir"), CloudFileUtil.GenerateUniqueFileName());
            byte[] randomContent = new byte[10240];
            this.randomProvider.NextBytes(randomContent);
            using (var stream = File.Create(Path.Combine(this.mountedShareRoot.FullName, fileName)))
            {
                stream.Write(randomContent, 0, randomContent.Length);
            }

            CommandAgent.GetFile(this.fileShare);
            var result = CommandAgent.Invoke();
            CommandAgent.AssertNoError();
            result.AssertObjectCollection(obj => obj.AssertCloudFile(fileName));
        }

        private static DirectoryInfo MountShare(string shareName)
        {
            string mountSharePath = string.Format(CultureInfo.InvariantCulture, @"\\{0}\{1}", fileEndpoint, shareName);
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "net";
            startInfo.Arguments = string.Format(
                CultureInfo.InvariantCulture,
                "use {0} /USER:{1} {2}",
                mountSharePath,
                accountName,
                accountKey);
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            Test.Info("Start to invoke mount command: net {0}", startInfo.Arguments);
            var netUseProcess = Process.Start(startInfo);
            netUseProcess.WaitForExit();
            Test.Info("Mount command exited with code {0}.", netUseProcess.ExitCode);
            if (netUseProcess.ExitCode != 0)
            {
                Test.Error(
                    "Mount command failed:\nStandardOutput:\n{0}\nStandardError:\n{1}",
                    netUseProcess.StandardOutput.ReadToEnd(),
                    netUseProcess.StandardError.ReadToEnd());

                throw new InvalidOperationException("Failed to mount share.");
            }

            return new DirectoryInfo(mountSharePath);
        }

        private static void DismountShare(DirectoryInfo mountedShareRoot)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "net";
            startInfo.Arguments = string.Format(
                CultureInfo.InvariantCulture,
                "use {0} /DELETE",
                mountedShareRoot.FullName);
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            Test.Info("Start to invoke mount command: net {0}", startInfo.Arguments);
            var netUseProcess = Process.Start(startInfo);
            netUseProcess.WaitForExit();
            Test.Info("Mount command exited with code {0}.", netUseProcess.ExitCode);
            if (netUseProcess.ExitCode != 0)
            {
                Test.Error(
                    "Mount command failed:\nStandardOutput:\n{0}\nStandardError:\n{1}",
                    netUseProcess.StandardOutput.ReadToEnd(),
                    netUseProcess.StandardError.ReadToEnd());

                throw new InvalidOperationException("Failed to dismount share.");
            }
        }

        private static void SMBValidationRetry(Func<bool> action, string actionDescription)
        {
            int retryCount = 0;
            while (!action())
            {
                Test.Info("Failed to {0}. Retry Count = {1}, Retry Interval = {2}", actionDescription, retryCount, RetryIntervalForSMBValidation);
                retryCount++;
                if (retryCount >= RetryLimitForSMBValidation)
                {
                    Test.Error("Exceeded retry limit {0} when try to {1}.", RetryLimitForSMBValidation, actionDescription);
                    return;
                }

                Thread.Sleep(RetryIntervalForSMBValidation);
            }

            Test.Info("Succeeded to {0}.", actionDescription);
        }
    }
}
