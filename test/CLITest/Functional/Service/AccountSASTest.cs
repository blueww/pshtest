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

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void AccountSASWithRandomParameters()
        {
            SharedAccessAccountServices randomService = (SharedAccessAccountServices)TestBase.random.Next(0, 15);
            SharedAccessAccountResourceTypes randomReourceType = (SharedAccessAccountResourceTypes)TestBase.random.Next(0, 7);
            string randomPermission = string.Empty;

            foreach (char p in "racwdlup")
            {
                if (TestBase.random.NextDouble() >= 0.5)
                    randomPermission += p;
            }
            
            SharedAccessProtocol? randomProtocol = null;            
            if (TestBase.random.NextDouble() >= 0.5)
            {
                randomProtocol = SharedAccessProtocol.HttpsOnly;
            }
            else
            {
                randomProtocol = SharedAccessProtocol.HttpsOrHttp;
            }

            string randomIPAddressOrRange = null;            
            if (TestBase.random.NextDouble() >= 0.5)
            {
                randomIPAddressOrRange = "10.12.233.1";
            }
            else
            {
                randomIPAddressOrRange = "12.34.56.234-12.157.57.127";
            }

            DateTime? randomStartTime = DateTime.Now.AddSeconds(-1 * TestBase.random.Next(0, 1000));
            DateTime? randomExpiryTime = DateTime.Now.AddSeconds(TestBase.random.Next(1000, 100000));


            string sasToken = GenerateAndValidateAccountSAS(
                randomService,
                randomReourceType,
                randomPermission, 
                randomProtocol, 
                randomIPAddressOrRange, 
                randomStartTime, 
                randomExpiryTime);
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
