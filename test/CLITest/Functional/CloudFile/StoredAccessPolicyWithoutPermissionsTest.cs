﻿using System;
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

            if (lang == Language.PowerShell)
            {
                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sasToken);
            }
            else
            {
                CommandAgent.SetStorageContextWithSASTokenInConnectionString(StorageAccount, sasToken);
            }
        }

        /// <summary>
        /// Positive functional test case 8.48
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void ManageStoredAccessPolicy()
        {
            DateTime? expiryTime = DateTime.Today.AddDays(10);
            string permission = "rwdl";

            string errorMsg = "This request is not authorized to perform this operation.";

            Test.Assert(!CommandAgent.NewAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p"), permission, null, expiryTime),
                "Should fail to new a stored access policy to share with sas token credentials");
            ExpectedContainErrorMessage(errorMsg);

            Test.Assert(!CommandAgent.GetAzureStorageShareStoredAccessPolicy(shareName, null),
                "Should fail to get stored access policy on a share with sas token credentials");
            ExpectedContainErrorMessage(errorMsg);

            Test.Assert(!CommandAgent.RemoveAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p")),
                "Should fail to remove stored access policy on a share with sas token credentials");
            ExpectedContainErrorMessage(errorMsg);

            Test.Assert(!CommandAgent.SetAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p"), permission, null, null),
                "Should fail to set stored access policy on a share with sas token credentials");
            ExpectedContainErrorMessage(errorMsg);
        }
    }
}