using System;
using System.Threading;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using MS.Test.Common.MsTestLib;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.Service
{
    [TestClass]
    public class AccountSASTest : TestBase
    {
        [ClassInitialize()]
        public static void AccountSASTestClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void AccountSASTestClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void AccountSASWithFullPermission()
        {
            string sasToken = GenerateAndValidateAccountSAS(
                SharedAccessAccountServices.Blob | SharedAccessAccountServices.File | SharedAccessAccountServices.Queue | SharedAccessAccountServices.Table,
                SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Service,
                "racwdlup", null, null, null, null);

            CloudBlobUtil blobUtil = new CloudBlobUtil(StorageAccount);
            blobUtil.SetupTestContainerAndBlob();

            blobUtil.ValidateBlobWriteableWithSasToken(blobUtil.Blob, sasToken);
        }

        private string GenerateAndValidateAccountSAS(
            SharedAccessAccountServices service,
            SharedAccessAccountResourceTypes resourceType,
            string permission,
            SharedAccessProtocol? protocol = null,
            string iPAddressOrRange = null,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            Test.Assert(agent.NewAzureStorageAccountSAS(
                service, resourceType, permission, protocol, iPAddressOrRange, startTime, expiryTime), "Should succeeded in generating an account sas with full permissions, services and resource types");

            AccountSASUtils.ValidateAccountSAS(
                service, resourceType, permission, protocol, iPAddressOrRange, startTime, expiryTime, agent.Output[0][Constants.SASTokenKey].ToString());

            return agent.Output[0][Constants.SASTokenKey].ToString();
        }
    }
}
