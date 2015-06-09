using System;
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.File;
using MS.Test.Common.MsTestLib;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    [TestClass]
    public class StoredAccessPolicyWithoutPermissionsTest : TestBase
    {
        static string shareName = Utility.GenNameString("share");
        static CloudFileShare share = null;
        [ClassInitialize]
        public static void ShareStoredAccessPolicyTestInitialize(TestContext context)
        {
            StorageAccount = Utility.ConstructStorageAccountFromConnectionString();
            TestBase.TestClassInitialize(context);
            share = fileUtil.EnsureFileShareExists(shareName);
        }

        [ClassCleanup]
        public static void ShareStoredAccessPolicyTestInitialize()
        {
            TestBase.TestClassCleanup();
            share.Delete();
        }

        public override void OnTestSetup()
        {
            string sasToken = share.GetSharedAccessSignature(new SharedAccessFilePolicy()
            {
                Permissions = SharedAccessFilePermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
            });

            this.agent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sasToken);
        }

        /// <summary>
        /// Positive functional test case 8.48
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        public void ManageStoredAccessPolicy()
        {
            DateTime? expiryTime = DateTime.Today.AddDays(10);
            string permission = "rwdl";

            Test.Assert(!agent.NewAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p"), permission, null, expiryTime),
                "Should fail to new a stored access policy to share with sas token credentials");
            ExpectedContainErrorMessage("The specified share does not exist");

            Test.Assert(!agent.GetAzureStorageShareStoredAccessPolicy(shareName, null),
                "Should fail to get stored access policy on a share with sas token credentials");
            ExpectedContainErrorMessage("The specified share does not exist");

            Test.Assert(!agent.RemoveAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p")),
                "Should fail to remove stored access policy on a share with sas token credentials");
            ExpectedContainErrorMessage("The specified share does not exist");

            Test.Assert(!agent.SetAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p"), permission, null, null),
                "Should fail to set stored access policy on a share with sas token credentials");
            ExpectedContainErrorMessage("The specified share does not exist");
        }
    }
}