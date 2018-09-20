using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using MS.Test.Common.MsTestLib;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    [TestClass]
    public class ShareQuotaTest : TestBase
    {
        private const int MinQuota = 1;
        private const int MaxQuota = 5120;

        [ClassInitialize]
        public static void ShareQuotaTestInitialize(TestContext context)
        {
            TestBase.TestClassInitialize(context);
        }

        [ClassCleanup]
        public static void ShareQuotaTestInitialize()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Positive functional test case 8.58
        /// </summary>
        [TestMethod]         
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        public void SetQuotaValueTest()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                fileUtil.CleanupDirectory(share.GetRootDirectoryReference());
                int quota = random.Next(MinQuota, MaxQuota);

                Test.Assert(CommandAgent.SetAzureStorageShareQuota(shareName, quota),
                    "Set share quota should succeed");
                ValidateShareQuota(share, quota);

                fileUtil.CleanupDirectory(share.GetRootDirectoryReference());
                quota = random.Next(MinQuota, MaxQuota);
                Test.Assert(CommandAgent.SetAzureStorageShareQuota(share, quota),
                    "Set share quota with share instance should succeed");
                ValidateShareQuota(share, quota);

                fileUtil.CleanupDirectory(share.GetRootDirectoryReference());
                Test.Assert(CommandAgent.SetAzureStorageShareQuota(share, quota),
                    "Set share quota with unchanged value should succeed");
                ValidateShareQuota(share, quota);

                fileUtil.CleanupDirectory(share.GetRootDirectoryReference());
                Test.Assert(CommandAgent.SetAzureStorageShareQuota(shareName, MinQuota),
                    "Set share minimum quota to a share should succeed");
                ValidateShareQuota(share, MinQuota);

                fileUtil.CleanupDirectory(share.GetRootDirectoryReference());
                Test.Assert(CommandAgent.SetAzureStorageShareQuota(shareName, MaxQuota),
                    "Set share Maximum quota to a share should succeed");
                ValidateShareQuota(share, MaxQuota);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Positive functional test case 8.58
        /// </summary>
        [TestMethod]          
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(CLITag.File)]
        public void SetQuotaWithDiffShareNameTest()
        {
            string shareName = Utility.GenNameString("");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                fileUtil.CleanupDirectory(share.GetRootDirectoryReference());
                int quota = random.Next(MinQuota, MaxQuota);

                Test.Assert(CommandAgent.SetAzureStorageShareQuota(shareName, quota),
                    "Set quota of share with longest name should succeed");
                ValidateShareQuota(share, quota);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }

            shareName = Utility.GenNameString("", 3);
            share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                fileUtil.CleanupDirectory(share.GetRootDirectoryReference());
                int quota = random.Next(MinQuota, MaxQuota);

                Test.Assert(CommandAgent.SetAzureStorageShareQuota(shareName, quota),
                    "Set quota of share with shortest name should succeed");
                ValidateShareQuota(share, quota);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }
        
        /// <summary>
        /// Negative functional test case 8.58
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void SetQuotaNegativeTest()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                fileUtil.CleanupDirectory(share.GetRootDirectoryReference());

                // Quota less than minimum value
                int quota = random.Next(0 - MaxQuota, MinQuota);
                Test.Assert(!CommandAgent.SetAzureStorageShareQuota(shareName, quota),
                    "Set quota with value of less than 1 to a share should fail.");
                ExpectedContainErrorMessage(string.Format("The argument 'Quota' is smaller than minimum of '1'"));
                
                // Quota greater than maximum value
                quota = random.Next(MaxQuota + 1, int.MaxValue);
                Test.Assert(!CommandAgent.SetAzureStorageShareQuota(shareName, quota),
                    "Set quota with value of greater than 5120 to a share should fail.");
                ExpectedContainErrorMessage(string.Format("The value for one of the HTTP headers is not in the correct format.", quota));

                // Share not exist
                string nonExistShareName = Utility.GenNameString("share");
                fileUtil.DeleteFileShareIfExists(nonExistShareName);
                quota = random.Next(MinQuota, MaxQuota);
                Test.Assert(!CommandAgent.SetAzureStorageShareQuota(nonExistShareName, quota),
                    "Set quota to a non-exist share should fail.");
                ExpectedContainErrorMessage("The specified share does not exist");

                string notValidShareName = "SHARE";
                Test.Assert(!CommandAgent.SetAzureStorageShareQuota(notValidShareName, quota),
                    "Set quota to a not valid share name should fail.");
                ExpectedContainErrorMessage(string.Format("The given share name/prefix '{0}' is not a valid name for a file share of Microsoft Azure File Service", notValidShareName));
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        private void ValidateShareQuota(CloudFileShare share, int quota)
        {
            share.FetchAttributes();

            Test.Assert(share.Properties.Quota == quota, "Share quota should be the same with expected. {0} == {1}", share.Properties.Quota, quota);

            const long TBSize = 1024 * 1024 * 1024 * 1024L;
            const long GBSize = 1024 * 1024 * 1024;

            int sizeInTB = quota / 1024;
            int fileNameIndex = 0;
            string fileNamePrefix = Utility.GenNameString("file");

            while (sizeInTB > 0)
            {
                string fileName = string.Format("{0}_{1}", fileNamePrefix, fileNameIndex);
                fileNameIndex++;

                var file = share.GetRootDirectoryReference().GetFileReference(fileName);
                file.Create(TBSize);
                sizeInTB--;
            }

            int sizeInGB = quota % 1024;

            while (sizeInGB > 0)
            {
                string fileName = string.Format("{0}_{1}", fileNamePrefix, fileNameIndex);
                fileNameIndex++;

                var file = share.GetRootDirectoryReference().GetFileReference(fileName);
                file.Create(GBSize);
                sizeInGB--;
            }

            try
            {
                string fileName = string.Format("{0}_{1}", fileNamePrefix, fileNameIndex);
                var file = share.GetRootDirectoryReference().GetFileReference(fileName);
                file.Create(1024);
            }
            catch (StorageException ex)
            {
                Test.Info("Got an exception when try to create files larger than quota as expected. {0}", ex.Message);
                return;
            }

            Test.Warn(string.Format("Actual quota is larger than expected. The share usage is {0}", share.GetStats().Usage));
        }
    }
}