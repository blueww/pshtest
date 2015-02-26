namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    public class NewAzureStorageFileShareTest : TestBase
    {
        private Random randomProvider = new Random();

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

        public override void OnTestCleanUp()
        {
            this.agent.Dispose();
        }

        /// <summary>
        /// Positive functional test case 5.2.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void PipelineMultipleShareNamesTest()
        {
            // TODO: Generate more random names for file shares after the
            // naming rules is settled down.
            int numberOfShares = this.randomProvider.Next(2, 33);
            string[] names = Enumerable.Range(0, numberOfShares)
                    .Select(i => CloudFileUtil.GenerateUniqueFileShareName()).ToArray();

            var client = StorageAccount.CreateCloudFileClient();

            // Ensure all file shares are not exists
            for (int i = 0; i < names.Length; i++)
            {
                while (client.GetShareReference(names[i]).Exists())
                {
                    names[i] = CloudFileUtil.GenerateUniqueFileShareName();
                }
            }

            try
            {
                this.agent.NewFileShareFromPipeline();
                var result = this.agent.Invoke(names);

                this.agent.AssertNoError();
                result.AssertObjectCollection(obj => result.AssertCloudFileContainer(obj, new List<string>(names)), numberOfShares);
            }
            finally
            {
                foreach (string fileShareName in names)
                {
                    fileUtil.DeleteFileShareIfExists(fileShareName);
                }
            }
        }

        /// <summary>
        /// Positive functional test case 5.2.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareWhichHasBeenDeletedAndGCed()
        {
            const int CreateShareInterval = 10000;
            const int CreateShareRetryLimit = 10;

            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);

            // Delete the share first.
            fileShare.Delete();

            Stopwatch watch = Stopwatch.StartNew();
            // Try to create the share
            try
            {
                for (int i = 0; i < CreateShareRetryLimit; i++)
                {
                    Thread.Sleep(CreateShareInterval);
                    Test.Info("Try to create a share which has just been deleted. RetryCount = {0}", i);
                    this.agent.NewFileShare(fileShareName);
                    var result = this.agent.Invoke();
                    if (!this.agent.HadErrors)
                    {
                        Test.Info("Successfully created the file share at round {0}.", i);
                        return;
                    }

                    this.agent.AssertErrors(errorRecord => errorRecord.AssertError(AssertUtil.ShareBeingDeletedFullQualifiedErrorId));
                    this.agent.Clear();
                }

                Test.Error("Failed to create the file share within the given retry count {0}. Total time passed is {1}", CreateShareRetryLimit, watch.Elapsed);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// Positive functional test case 5.2.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareWith63CharactersTest()
        {
            this.CreateShareInternal(
                () => FileNamingGenerator.GenerateValidShareName(63),
                (results, shareName) =>
                {
                    this.agent.AssertNoError();
                    results.AssertObjectCollection(obj => results.AssertCloudFileContainer(obj, shareName));
                });
        }

        /// <summary>
        /// Positive functional test case 5.2.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareWith3CharactersTest()
        {
            this.CreateShareInternal(
                () => FileNamingGenerator.GenerateValidShareName(3),
                (results, shareName) =>
                {
                    this.agent.AssertNoError();
                    results.AssertObjectCollection(obj => results.AssertCloudFileContainer(obj, shareName));
                });
        }

        /// <summary>
        /// Negative functional test case 5.2.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareWithInvalidCharactersTest_DoubleDash()
        {
            int length = this.randomProvider.Next(10, 60);
            this.CreateShareInternal(
                () => FileNamingGenerator.GenerateInvalidShareName_DoubleDash(length),
                (results, shareName) =>
                {
                    this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
                },
                false);
        }

        /// <summary>
        /// Negative functional test case 5.2.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateShareWithInvalidCharactersTest_StartsWithDash()
        {
            int length = this.randomProvider.Next(10, 60);
            this.CreateShareInternal(
                () => FileNamingGenerator.GenerateInvalidShareName_StartsWithDash(length),
                (results, shareName) =>
                {
                    this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
                },
                false);
        }

        /// <summary>
        /// Negative functional test case 5.2.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareWithInvalidCharactersTest_EndsWithDash()
        {
            int length = this.randomProvider.Next(10, 60);
            this.CreateShareInternal(
                () => FileNamingGenerator.GenerateInvalidShareName_EndsWithDash(length),
                (results, shareName) =>
                {
                    this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
                },
                false);
        }

        /// <summary>
        /// Negative functional test case 5.2.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareWithInvalidCharactersTest_TooShort()
        {
            this.CreateShareInternal(
                () => FileNamingGenerator.GenerateValidShareName(2),
                (results, shareName) =>
                {
                    this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
                },
                false);
        }

        /// <summary>
        /// Negative functional test case 5.2.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateAlreadyExistsShareTest()
        {
            string shareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.EnsureFileShareExists(shareName);
            this.CreateShareInternal(
                () => shareName,
                (results, fileShareName) =>
                {
                    this.agent.AssertErrors(err => err.AssertError(AssertUtil.ShareAlreadyExistsFullQualifiedErrorId));
                },
                false);
        }

        /// <summary>
        /// Negative functional test case 5.2.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateAListOfSharesWhileSomeOfThemAlreadyExistsTest()
        {
            int numberOfSharesToBeCreated = this.randomProvider.Next(5, 20);
            List<string> shareNames = Enumerable.Range(0, numberOfSharesToBeCreated).Select(x => CloudFileUtil.GenerateUniqueFileShareName()).ToList();
            int numberOfSharesAlreadyExists = this.randomProvider.Next(1, numberOfSharesToBeCreated - 1);
            List<string> existingShareNames = shareNames.RandomlySelect(numberOfSharesAlreadyExists, this.randomProvider).ToList();
            foreach (string shareName in shareNames)
            {
                if (fileUtil.FileShareExists(shareName) && (!existingShareNames.Contains(shareName)))
                {
                    existingShareNames.Add(shareName);
                }
            }

            try
            {
                foreach (string shareName in existingShareNames)
                {
                    fileUtil.EnsureFileShareExists(shareName);
                }

                this.agent.NewFileShareFromPipeline();
                var result = this.agent.Invoke(shareNames);
                this.agent.AssertErrors(
                    err => err.AssertError(AssertUtil.ShareAlreadyExistsFullQualifiedErrorId),
                    numberOfSharesAlreadyExists);

                foreach (string shareName in shareNames)
                {
                    fileUtil.AssertFileShareExists(shareName, "File share should exist after created.");
                }
            }
            finally
            {
                foreach (string shareName in shareNames)
                {
                    fileUtil.DeleteFileShareIfExists(shareName);
                }
            }
        }

        /// <summary>
        /// Negative functional test case 5.2.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareWithInvalidCharactersTest_UpperCase()
        {
            int length = this.randomProvider.Next(10, 60);
            this.CreateShareInternal(
                () => FileNamingGenerator.GenerateInvalidShareName_UpperCase(length),
                (results, shareName) =>
                {
                    this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
                },
                false);
        }

        /// <summary>
        /// Negative functional test case 5.2.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareWithInvalidCharactersTest_TooLong()
        {
            this.CreateShareInternal(
                () => FileNamingGenerator.GenerateValidShareName(64),
                (results, shareName) =>
                {
                    this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
                },
                false);
        }

        /// <summary>
        /// Negative functional test case 5.2.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareWhichHasJustBeenDeletd()
        {
            string shareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.EnsureFileShareExists(shareName);
            fileUtil.DeleteFileShareIfExists(shareName);
            this.CreateShareInternal(
                () => shareName,
                (results, fileShareName) =>
                {
                    this.agent.AssertErrors(err => err.AssertError(AssertUtil.ShareBeingDeletedFullQualifiedErrorId));
                },
                false);
        }

        /// <summary>
        /// Negative functional test case 5.2.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void CreateShareOnOldTestAcountWhichDoesNotSupportFileService()
        {
            string shareName = CloudFileUtil.GenerateUniqueFileShareName();
            object contextObject = this.agent.CreateStorageContextObject(Test.Data.Get("Pre42StorageConnectionString"));
            this.agent.NewFileShare(shareName, contextObject);
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.NameResolutionFailureFullQualifiedErrorId,
                AssertUtil.ProtocolErrorFullQualifiedErrorId));
        }

        private void CreateShareInternal(Func<string> shareNameProvider, Action<IExecutionResult, string> assertAction, bool validateNotExists = true)
        {
            string shareName = shareNameProvider();

            if (validateNotExists)
            {
                while (fileUtil.Client.GetShareReference(shareName).Exists())
                {
                    shareName = shareNameProvider();
                }
            }

            try
            {
                this.agent.NewFileShare(shareName);
                assertAction(this.agent.Invoke(), shareName);
            }
            finally
            {
                if (validateNotExists)
                {
                    fileUtil.DeleteFileShareIfExists(shareName);
                }
            }
        }
    }
}
