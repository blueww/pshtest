namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    public class GetAzureStorageShareTest : TestBase
    {
        private Random randomProvider = new Random();

        [ClassInitialize]
        public static void GetAzureStorageShareTestInitialize(TestContext context)
        {
            TestBase.TestClassInitialize(context);
        }

        [ClassCleanup]
        public static void GetAzureStorageShareTestCleanup()
        {
            TestBase.TestClassCleanup();
        }

        public override void OnTestSetup()
        {
            this.agent.Clear();
        }

        public override void OnTestCleanUp()
        {
            this.agent.Clear();
        }

        /// <summary>
        /// Positive functional test case 5.3.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetAListOfSharedByPrefixTest()
        {
            string prefix = "testprefix";
            string nonPrefix = "testnonprefix";

            // Remove all shares starting with prefix or nonprefix
            foreach (var share in fileUtil.ListShares(prefix).Concat(fileUtil.ListShares(nonPrefix)))
            {
                share.DeleteIfExists();
            }

            int numberOfSharesMatchingPrefix = this.randomProvider.Next(5, 20);
            int numberOfSharesNotMatchingPrefix = this.randomProvider.Next(5, 10);
            var sharesMatchingPrefixList = BuildShareNamesByPrefix(prefix, numberOfSharesMatchingPrefix).ToList();
            var sharesNotMatchingPrefixList = BuildShareNamesByPrefix(nonPrefix, numberOfSharesNotMatchingPrefix).ToList();
            var allSharesList = sharesMatchingPrefixList.Concat(sharesNotMatchingPrefixList).ToList();
            try
            {
                foreach (var shareName in allSharesList)
                {
                    fileUtil.EnsureFileShareExists(shareName);
                }

                this.agent.GetFileShareByPrefix(prefix);
                var result = this.agent.Invoke();
                result.AssertObjectCollection(obj => result.AssertCloudFileContainer(obj, sharesMatchingPrefixList), sharesMatchingPrefixList.Count);
            }
            finally
            {
                foreach (var shareName in allSharesList)
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
        }

        /// <summary>
        /// Positive functional test case 5.3.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetFileShareUsageTest()
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);

            try
            {
                agent.GetFileShareByName(fileShareName);

                var result = agent.Invoke();

                agent.AssertNoError();
                result.AssertObjectCollection(obj => result.AssertCloudFileContainer(obj, fileShareName), 1);

                fileUtil.CreateFile(fileShare.GetRootDirectoryReference(), cloudFileName);

                agent.GetFileShareByName(fileShareName);

                result = agent.Invoke();
                agent.AssertNoError();
                result.AssertObjectCollection(obj => result.AssertCloudFileContainer(obj, fileShareName, 1), 1);
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// Positive functional test case 5.3.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void ListAllSharesTest()
        {
            var sharesList = BuildShareNamesByPrefix(string.Empty, this.randomProvider.Next(5, 20)).ToList();

            try
            {
                foreach (var shareName in sharesList)
                {
                    fileUtil.EnsureFileShareExists(shareName);
                }

                this.agent.GetFileShareByPrefix(string.Empty);
                var result = this.agent.Invoke();
                result.AssertObjectCollection(obj =>
                {
                    if (sharesList.Count > 0)
                    {
                        result.AssertCloudFileContainer(obj, sharesList, false);
                    }
                }, -1);

                Test.Assert(sharesList.Count == 0, "All shares created should be listed by list all.");
            }
            finally
            {
                foreach (var shareName in sharesList)
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
        }

        /// <summary>
        /// Negative functional test case 5.3.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetNonExistingShareTest()
        {
            string shareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.DeleteFileShareIfExists(shareName);
            this.agent.GetFileShareByName(shareName);
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(
                AssertUtil.ResourceNotFoundFullQualifiedErrorId,
                AssertUtil.ShareNotFoundFullQualifiedErrorId,
                AssertUtil.ShareBeingDeletedFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.3.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void ListFileShareByPrefixAndMatchingNoneTest()
        {
            string prefix = "nonexistingprefix";

            // Remove all shares starting with prefix or nonprefix
            foreach (var share in fileUtil.ListShares(prefix))
            {
                share.DeleteIfExists();
            }

            this.agent.GetFileShareByPrefix(prefix);
            var result = this.agent.Invoke();
            this.agent.AssertNoError();
            result.AssertNoResult();
        }

        /// <summary>
        /// Negative functional test case 5.3.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetShareByNameUsingWildCardTest()
        {
            string shareName = string.Concat("*", CloudFileUtil.GenerateUniqueFileShareName());
            this.agent.GetFileShareByName(shareName);
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.3.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetShareByPrefixUsingWildCardTest()
        {
            string shareName = string.Concat("*", CloudFileUtil.GenerateUniqueFileShareName());
            this.agent.GetFileShareByPrefix(shareName);
            this.agent.Invoke();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.InvalidArgumentFullQualifiedErrorId));
        }

        private static IEnumerable<string> BuildShareNamesByPrefix(string prefix, int numberOfShares)
        {
            for (int i = 0; i < numberOfShares; i++)
            {
                yield return string.Concat(prefix, CloudFileUtil.GenerateUniqueFileShareName());
            }
        }
    }
}
