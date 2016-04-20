namespace Management.Storage.ScenarioTest.Functional.Blob
{
    using Management.Storage.ScenarioTest.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Functional test case for new-azurestoragecontainersas
    /// </summary>
    [TestClass]
    public class NewContainerSas : TestBase
    {
        [ClassInitialize()]
        public static void NewContainerSasClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void NewContainerSasClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// 1.	Generate SAS of a container with only limited access right(read, write,delete,list)
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewContainerSas)]
        public void NewContainerSasWithPermission()
        {
            //Container read permission
            string containerPermission = "r";
            GenerateSasTokenAndValid(containerPermission);

            //Container write permission
            containerPermission = "w";
            GenerateSasTokenAndValid(containerPermission);
                
            //Container delete permission
            containerPermission = "d";
            GenerateSasTokenAndValid(containerPermission);

            //Container list permission
            containerPermission = "l";
            GenerateSasTokenAndValid(containerPermission);

            // TODO: Enable it when xplat supports the permissions
            if (lang == Language.PowerShell)
            {
                //Container create permission
                containerPermission = "c";
                GenerateSasTokenAndValid(containerPermission);

                //Container append permission
                containerPermission = "a";
                GenerateSasTokenAndValid(containerPermission);
            }

            // Permission param is required according to the design, cannot accept string.Empty, so comment this. We may support this in the future.
            //None permission
            //containerPermission = "";
            //GenerateSasTokenAndValid(containerPermission);

            //Random combination
            containerPermission = Utility.GenRandomCombination(Utility.ContainerPermission);
            GenerateSasTokenAndValid(containerPermission);
        }

        /// <summary>
        /// 2.	Generate SAS of a container with a limited time period
        /// Wait for the time expiration
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewContainerSas)]
        public void NewContainerSasWithLifeTime()
        { 
            blobUtil.SetupTestContainerAndBlob();
            double lifeTime = 3; //Minutes
            const double deltaTime = 0.5;
            DateTime startTime = DateTime.Now.AddMinutes(lifeTime);
            DateTime expiryTime = startTime.AddMinutes(lifeTime);

            try
            {
                string containerPermission = Utility.GenRandomCombination(Utility.ContainerPermission);
                string sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission, startTime, expiryTime);
                try
                {
                    ValidateSasToken(blobUtil.Container, containerPermission, sastoken);
                    Test.Error(string.Format("Access container should fail since the start time is {0}, but now is {1}",
                        startTime.ToUniversalTime().ToString(), DateTime.UtcNow.ToString())); 
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }

                Test.Info("Sleep and wait for the sas token start time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));
                ValidateSasToken(blobUtil.Container, containerPermission, sastoken);
                Test.Info("Sleep and wait for sas token expiry time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));

                try
                {
                    ValidateSasToken(blobUtil.Container, containerPermission, sastoken);
                    Test.Error(string.Format("Access container should fail since the expiry time is {0}, but now is {1}",
                        expiryTime.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 3.	Generate SAS of a container by policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewContainerSas)]
        public void NewContainerSasWithPolicy()
        {
            blobUtil.SetupTestContainerAndBlob();

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                BlobContainerPermissions permission = new BlobContainerPermissions();
                string policyName = Utility.GenNameString("saspolicy");

                permission.SharedAccessPolicies.Add(policyName, new SharedAccessBlobPolicy
                {
                    SharedAccessExpiryTime = DateTime.Now.Add(sasLifeTime),
                    Permissions = SharedAccessBlobPermissions.Read,
                });
                
                blobUtil.Container.SetPermissions(permission);
                string sasToken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                ValidateSasToken(blobUtil.Container, "r", sasToken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 4.	Generate SAS of a container of a non-existing policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        public void NewContainerSasWithNotExistPolicy()
        {
            blobUtil.SetupTestContainerAndBlob();

            try
            {
                string policyName = Utility.GenNameString("notexistpolicy");

                Test.Assert(!CommandAgent.NewAzureStorageContainerSAS(blobUtil.Container.Name, policyName, string.Empty),
                    "Generate container sas token with not exist policy should fail");
                ExpectedContainErrorMessage(string.Format("Invalid access policy '{0}'.", policyName));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 3.	Generate SAS of a container with expiry time before start time
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewContainerSas)]
        public void NewContainerSasWithInvalidLifeTime()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                DateTime start = DateTime.UtcNow;
                DateTime end = start.AddHours(1.0);
                Test.Assert(!CommandAgent.NewAzureStorageContainerSAS(blobUtil.Container.Name, string.Empty, "l", end, start),
                        "Generate container sas token with invalid should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 4.	Return full uri with SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewContainerSas)]
        public void NewContainerSasWithFullUri()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string containerPermission = Utility.GenRandomCombination(Utility.ContainerPermission);
                string fullUri = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission);
                string sasToken = (lang == Language.PowerShell ? fullUri.Substring(fullUri.IndexOf("?")) : fullUri);
                ValidateSasToken(blobUtil.Container, containerPermission, sasToken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1.	Generate SAS of a container with only limited access right(read,write,delete,list,none)
        ///     Verify access with the non-granted right to this blob is denied
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewContainerSas)]
        public void NewContainerSasWithLimitedPermission()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                //Container read permission
                string containerPermission = "r";
                string limitedPermission = lang == Language.PowerShell ? "wdlac" : "wdl";
                string sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission);
                ValidateLimitedSasPermission(blobUtil.Container, limitedPermission, sastoken);

                //Container write permission
                containerPermission = "w";
                limitedPermission = "rdl";
                sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission);
                ValidateLimitedSasPermission(blobUtil.Container, limitedPermission, sastoken);

                //Container delete permission
                containerPermission = "d";
                limitedPermission = lang == Language.PowerShell ? "rwlac" : "rwl";
                sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission);
                ValidateLimitedSasPermission(blobUtil.Container, limitedPermission, sastoken);

                //Container list permission
                containerPermission = "l";
                limitedPermission = lang == Language.PowerShell ? "rwdac" : "rwd";
                sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission);
                ValidateLimitedSasPermission(blobUtil.Container, limitedPermission, sastoken);

                // TODO: Enable it when xplat supports the permissions
                if (lang == Language.PowerShell)
                {
                    //Container add permission
                    containerPermission = "a";
                    limitedPermission = "rwdlc";
                    sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission);
                    ValidateLimitedSasPermission(blobUtil.Container, limitedPermission, sastoken);

                    //Container create permission
                    containerPermission = "c";
                    limitedPermission = "rwdla";
                    sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission);
                    ValidateLimitedSasPermission(blobUtil.Container, limitedPermission, sastoken);
                }

                //Container none permission
                //containerPermission = "";
                //limitedPermission = "rdwl";
                //sastoken = agent.GetContainerSasFromPsCmd(blobUtil.Container.Name, string.Empty, containerPermission);
                //ValidateLimitedSasPermission(blobUtil.Container, limitedPermission, sastoken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1.	Generate SAS of protocal: HttpsOnly, and all available value of permission. 
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        public void NewContainerSas_Httpsonly()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string containerPermission = "rwdl";
                string sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission, null, null, false, SharedAccessProtocol.HttpsOnly);

                blobUtil.ValidateBlobWriteableWithSasToken(blobUtil.Blob, sastoken);

                try
                {
                    blobUtil.ValidateBlobWriteableWithSasToken(blobUtil.Blob, sastoken, useHttps: false);
                    Test.Error(string.Format("Write blob with http should fail since the sas is HttpsOnly."));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(306, e.RequestInformation.HttpStatusCode, "Protocal not match error: ");
                }              
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1.	Generate SAS of IPAddressOrRange: [Not Current IP], and all available value of permission, protocal. 
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        public void NewContainerSas_NotCurrentIP()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string containerPermission = "rwdl";
                string sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission, null, null, false, null, "1.1.1.1");

                try
                {
                    blobUtil.ValidateBlobWriteableWithSasToken(blobUtil.Blob, sastoken);
                    Test.Error(string.Format("Write blob with should fail since the ipAcl is not current IP."));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1.	Generate SAS of IPAddressOrRange: [Range include Current IP], and all available value of permission. 
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewContainerSas)]
        public void NewContainerSas_CurrentIPRange()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string containerPermission = "rwdl";
                string fullUri = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission, null, null, true, null, "0.0.0.0-255.255.255.255");
                string sastoken = (lang == Language.PowerShell ? fullUri.Substring(fullUri.IndexOf("?")) : fullUri);

                blobUtil.ValidateBlobWriteableWithSasToken(blobUtil.Blob, sastoken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// Generate a sas token and validate it.
        /// </summary>
        /// <param name="containerPermission">Container permission</param>
        internal void GenerateSasTokenAndValid(string containerPermission)
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string sastoken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, string.Empty, containerPermission);
                ValidateSasToken(blobUtil.Container, containerPermission, sastoken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// Validate the sas token 
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="containerPermission">container permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateSasToken(CloudBlobContainer container, string containerPermission, string sasToken)
        {
            foreach (char permission in containerPermission.ToLower())
            {
                switch (permission)
                {
                    case 'r':
                        blobUtil.ValidateContainerReadableWithSasToken(container, sasToken);
                        break;
                    case 'w':
                        blobUtil.ValidateContainerWriteableWithSasToken(container, sasToken);
                        break;
                    case 'd':
                        blobUtil.ValidateContainerDeleteableWithSasToken(container, sasToken);
                        break;
                    case 'l':
                        blobUtil.ValidateContainerListableWithSasToken(container, sasToken);
                        break;
                    case 'c':
                        blobUtil.ValidateContainerCreateableWithSasToken(container, sasToken);
                        break;
                    case 'a':
                        blobUtil.ValidateContainerAppendableWithSasToken(container, sasToken);
                        break;
                }
            }
        }

        /// <summary>
        /// Validte the limited permission for sas token 
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="containerPermission">Limited permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateLimitedSasPermission(CloudBlobContainer container,
            string limitedPermission, string sasToken)
        {
            foreach (char permission in limitedPermission.ToLower())
            {
                try
                {
                    ValidateSasToken(container, permission.ToString(), sasToken);
                    Test.Error("sastoken '{0}' should not contain the permission {1}", sasToken, permission.ToString());
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    if (403 == e.RequestInformation.HttpStatusCode)
                    {
                        Test.Info("Limited permission sas token should not access storage objects. {0}", e.RequestInformation.HttpStatusMessage);
                    }
                    else
                    {
                        Test.Error("Limited permission sas token should return 403, but actually it's {0} {1}",
                            e.RequestInformation.HttpStatusCode, e.RequestInformation.HttpStatusMessage);
                    }
                }
            }
        }
    }
}
