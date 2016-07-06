using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using System;

namespace Management.Storage.ScenarioTest.Functional.Service
{
    [TestClass]
    public class AccountSASTest : TestBase
    {
        [ClassInitialize()]
        public static void AccountSASTestClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            blobUtil.SetupTestContainerAndBlob();
        }

        [ClassCleanup()]
        public static void AccountSASTestClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void AccountSASWithFullPermission()
        {
            string sasToken = GenerateAndValidateAccountSAS(
                SharedAccessAccountServices.Blob | SharedAccessAccountServices.File | SharedAccessAccountServices.Queue | SharedAccessAccountServices.Table,
                SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Service,
                AccountSASUtils.fullPermission, null, null, null, null);

            blobUtil.ValidateBlobWriteableWithSasToken(blobUtil.Blob, sasToken);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void AccountSASWithRandomParameters()
        {
            SharedAccessAccountServices randomService;
            if (lang == Language.PowerShell)
            {
                randomService = (SharedAccessAccountServices) TestBase.random.Next(0, 15);
            }
            else
            {
                randomService = (SharedAccessAccountServices)TestBase.random.Next(1, 15);
            }

            SharedAccessAccountResourceTypes randomResourceType;
            if (lang == Language.PowerShell)
            {
                randomResourceType = (SharedAccessAccountResourceTypes) TestBase.random.Next(0, 7);
            }
            else
            {
                randomResourceType = (SharedAccessAccountResourceTypes)TestBase.random.Next(1, 7);
            }
            string randomPermission = string.Empty;

            foreach (char p in AccountSASUtils.fullPermission)
            {
                if (TestBase.random.NextDouble() >= 0.5)
                    randomPermission += p;
            }

            if (lang == Language.NodeJS && string.IsNullOrEmpty(randomPermission))
            {
                randomPermission = "r";
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
                randomResourceType,
                randomPermission, 
                randomProtocol, 
                randomIPAddressOrRange, 
                randomStartTime, 
                randomExpiryTime);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void AccountSAS_File_Container_w()
        {
            SharedAccessAccountServices service = SharedAccessAccountServices.File;
            SharedAccessAccountResourceTypes resourceType = SharedAccessAccountResourceTypes.Container;
            string permission = "w";           

            string sasToken = GenerateAndValidateAccountSAS(
                service,
                resourceType,
                permission, null, null, null, null);

            string shareName = "sharetocreatewithsas";

            fileUtil.ValidateShareCreatableWithSasToken(shareName, StorageAccount.Credentials.AccountName, sasToken);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void AccountSAS_NoneService()
        {
            string sasToken = GenerateAndValidateAccountSAS(
                SharedAccessAccountServices.None,
                SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Service,
                AccountSASUtils.fullPermission, null, null, null, null);

            try
            {
                blobUtil.ValidateContainerWriteableWithSasToken(blobUtil.Container, sasToken);
                Test.Error(string.Format("Write container should fail since the SharedAccessAccountServices is none"));
            }
            catch (StorageException e)
            {
                Test.Info(e.Message);
                ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void AccountSAS_HttpsOnly()
        {
            string sasToken = GenerateAndValidateAccountSAS(
                SharedAccessAccountServices.Table,
                SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Service,
                AccountSASUtils.fullPermission, 
                SharedAccessProtocol.HttpsOnly, null, null, null);

            tableUtil.ValidateTableAddableWithSasToken(tableUtil.CreateTable(), sasToken, useHttps: true);

            try
            {
                tableUtil.ValidateTableListWithSasToken(sasToken, useHttps: false);
                Test.Error(string.Format("List Table with http should fail since the sas is HttpsOnly."));
            }
            catch (StorageException e)
            {
                Test.Info(e.Message);
                ExpectEqual(306, e.RequestInformation.HttpStatusCode, "Protocal not match error: ");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void AccountSAS_NotCurrentIP()
        {
            string sasToken = GenerateAndValidateAccountSAS(
                SharedAccessAccountServices.Blob | SharedAccessAccountServices.File,
                SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Service,
                AccountSASUtils.fullPermission, 
                SharedAccessProtocol.HttpsOnly, 
                "1.2.3.4", null, null);

            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            try
            {
                //validate can't write Share
                fileUtil.ValidateShareWriteableWithSasToken(share, sasToken);
                Test.Error(string.Format("Write Share should fail since the ipAcl is not current IP."));
            }
            catch (StorageException e)
            {
                share.Delete();
                Test.Info(e.Message);
                ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
            }

        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void AccountSAS_IncludeIPRange()
        {
            string sasToken = GenerateAndValidateAccountSAS(
                SharedAccessAccountServices.Blob | SharedAccessAccountServices.Queue,
                SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Service,
                AccountSASUtils.fullPermission,
                SharedAccessProtocol.HttpsOnly,
                "0.0.0.0-255.255.255.255", null, null);
            
            //validate can delete blob
            CloudBlob blob = blobUtil.CreateAppendBlob(blobUtil.Container, "tesblob");
            blobUtil.ValidateBlobDeleteableWithSasToken(blob, sasToken);

            //validate can add queue
            CloudQueue queue = queueUtil.CreateQueue();
            queueUtil.ValidateQueueAddableWithSasToken(queue, sasToken);
            queueUtil.RemoveQueue(queue);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void AccountSAS_ExcludeIPRange()
        {
            string sasToken = GenerateAndValidateAccountSAS(
                SharedAccessAccountServices.Queue,
                SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Service,
                AccountSASUtils.fullPermission,
                SharedAccessProtocol.HttpsOnly,
                "0.0.0.0-1.1.1.1", null, null);
            
            CloudQueue queue = queueUtil.CreateQueue();
            try
            {
                //validate can't delete queue
                queueUtil.ValidateQueueRemoveableWithSasToken(queue, sasToken);
                Test.Error(string.Format("Delete queue should fail since the ip range not include current IP."));
            }
            catch (StorageException e)
            {
                queueUtil.RemoveQueue(queue);
                Test.Info(e.Message);
                ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void AccountSAS_File_Container_LimitTime()
        {
            string sasToken = GenerateAndValidateAccountSAS(
                SharedAccessAccountServices.File,
                SharedAccessAccountResourceTypes.Object,
                AccountSASUtils.fullPermission,
                null, null, DateTime.Now.AddMinutes(-5), DateTime.Now.AddMinutes(15));

            fileUtil.ValidateShareDeleteableWithSasToken(fileUtil.EnsureFileShareExists(Utility.GenNameString("share")), sasToken);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void AccountSAS_InvalidParameter()
        {
            SharedAccessAccountServices service = SharedAccessAccountServices.Blob | SharedAccessAccountServices.File | SharedAccessAccountServices.Queue | SharedAccessAccountServices.Table;
            SharedAccessAccountResourceTypes resourceType = SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object | SharedAccessAccountResourceTypes.Service;
            string permission = AccountSASUtils.fullPermission;
            SharedAccessProtocol sasProtocal = SharedAccessProtocol.HttpsOrHttp;
            string iPAddressOrRange = "0.0.0.0-255.255.255.255";
            DateTime startTime = DateTime.Now.AddMinutes(-5);
            DateTime expiryTime = DateTime.Now.AddMinutes(60);

            //invalid permission
            Test.Assert(!CommandAgent.NewAzureStorageAccountSAS(
                service, resourceType, "racwx", sasProtocal, iPAddressOrRange, startTime, expiryTime), "Set stored access policy with invalid permission should fail");
            if (lang == Language.PowerShell)
            {
                ExpectedContainErrorMessage("Invalid access permission");
            }
            else
            {

                ExpectedContainErrorMessage("Given  \"x\" is invalid");
            }

            //repeated permission - success
            GenerateAndValidateAccountSAS(
                service, resourceType, "rracw", sasProtocal, iPAddressOrRange, startTime, expiryTime);

            //invalid IP/IP range
            Test.Assert(!CommandAgent.NewAzureStorageAccountSAS(
                service, resourceType, permission, null, "123.3.4a", null, null), "Set stored access policy with invalid iPAddressOrRange should fail");
            if (lang == Language.PowerShell)
            {
                ExpectedContainErrorMessage("Error when parsing IP address: IP address is invalid.");
            }
            else
            {
                ExpectedContainErrorMessage("Invalid ip range format");
            }
            Test.Assert(!CommandAgent.NewAzureStorageAccountSAS(
                service, resourceType, permission, sasProtocal, "123.4.5.6_125.6.7.8", null, null), "Set stored access policy with invalid iPAddressOrRange should fail");
            if (lang == Language.PowerShell)
            {
                ExpectedContainErrorMessage("Error when parsing IP address: IP address is invalid.");
            }
            else
            {
                ExpectedContainErrorMessage("Invalid ip range format");
            }

            //success: start IP > end IP
            GenerateAndValidateAccountSAS(
                service, resourceType, permission, sasProtocal, "22.22.22.22-11.111.11.11", startTime, expiryTime);

            //Start time > expire Time
            Test.Assert(!CommandAgent.NewAzureStorageAccountSAS(
                service, resourceType, permission, sasProtocal, iPAddressOrRange, DateTime.Now.AddMinutes(5), DateTime.Now.AddMinutes(-5)), "Set stored access policy with invalid Start Time should fail");
            ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");
        }


        private string GenerateAndValidateAccountSAS(
            SharedAccessAccountServices service,
            SharedAccessAccountResourceTypes resourceType,
            string permission,
            SharedAccessProtocol? protocol = null,
            string iPAddressOrRange = null,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            Test.Assert(CommandAgent.NewAzureStorageAccountSAS(
                service, resourceType, permission, protocol, iPAddressOrRange, startTime, expiryTime), "Should succeeded in generating an account sas with full permissions, services and resource types");

            string sasToken;
            if (lang == Language.PowerShell)
            {
                sasToken = CommandAgent.Output[0][Constants.SASTokenKey].ToString();
            }
            else
            {
                sasToken = "?" + CommandAgent.Output[0][Constants.SASTokenKeyNode];
            }
            AccountSASUtils.ValidateAccountSAS(
                service, resourceType, permission, protocol, iPAddressOrRange, startTime, expiryTime, sasToken);

            return sasToken;
        }
    }
}
