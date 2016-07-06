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

    [TestClass]
    public class NewBlobSas : TestBase
    {
        [ClassInitialize()]
        public static void NewBlobSasClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void NewBlobSasClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// 1.	Generate SAS of a Blob with only limited access right(read, write,delete,list)
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewBlobSas)]
        public void NewBlobSasWithPermission()
        {
            //Blob read permission
            string blobPermission = "r";
            GenerateSasTokenAndValidate(blobPermission);

            //Blob write permission
            blobPermission = "w";
            GenerateSasTokenAndValidate(blobPermission);

            //Blob delete permission
            blobPermission = "d";
            GenerateSasTokenAndValidate(blobPermission);

            // TODO: Enable it when xplat supports the permissions
            if (lang == Language.PowerShell)
            {
                //Blob Create permission
                blobPermission = "c";
                GenerateSasTokenAndValidate(blobPermission);

                //Blob append permission
                blobPermission = "a";
                GenerateSasTokenAndValidate(blobPermission);
            }

            // Permission param is required according to the design, cannot accept string.Empty, so comment this. We may support this in the future.
            //None permission
            //blobPermission = "";
            //GenerateSasTokenAndValidate(blobPermission);

            //Random combination
            blobPermission = Utility.GenRandomCombination(Utility.BlobPermission);
            GenerateSasTokenAndValidate(blobPermission);
        }

        /// <summary>
        /// 2.	Generate SAS of a Blob with a limited time period
        /// Wait for the time expiration
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewBlobSas)]
        public void NewBlobSasWithLifeTime()
        {
            blobUtil.SetupTestContainerAndBlob();
            double waitEffectTime = 1; 
            double lifeTime = 5; //Minutes
            const double deltaTime = 0.5;
            DateTime startTime = DateTime.Now.AddMinutes(waitEffectTime);
            DateTime expiryTime = startTime.AddMinutes(lifeTime);

            try
            {
                string blobPermission = Utility.GenRandomCombination(Utility.BlobPermission);
                string sastoken = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission, startTime, expiryTime);
                try
                {
                    ValidateSasToken(blobUtil.Blob, blobPermission, sastoken);
                    Test.Error(string.Format("Access Blob should fail since the start time is {0}, but now is {1}",
                        startTime.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }

                Test.Info("Sleep and wait for the sas token start time");
                Thread.Sleep(TimeSpan.FromMinutes(waitEffectTime + deltaTime));
                ValidateSasToken(blobUtil.Blob, blobPermission, sastoken);
                Test.Info("Sleep and wait for sas token expiry time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));

                try
                {
                    if (!blobPermission.Contains('d'))
                    {
                        // if there is 'd' in Permission, the blob does not exist, we should skip this
                        ValidateSasToken(blobUtil.Blob, blobPermission, sastoken);
                        Test.Error(string.Format("Access Blob should fail since the expiry time is {0}, but now is {1}",
                            expiryTime.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                    }
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
        /// 3.	Generate SAS of a Blob by policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewBlobSas)]
        public void NewBlobSasWithPolicy()
        {
            blobUtil.SetupTestContainerAndBlob();

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                BlobContainerPermissions permission = new BlobContainerPermissions();
                string policyName = Utility.GenNameString("saspolicy");

                permission.SharedAccessPolicies.Add(policyName, new SharedAccessBlobPolicy
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.Add(sasLifeTime)
                });

                blobUtil.Container.SetPermissions(permission);
                string sasToken = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                ValidateSasToken(blobUtil.Blob, "r", sasToken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 4.	Generate SAS of a Blob of a non-existing policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        public void NewBlobSasWithNotExistPolicy()
        {
            blobUtil.SetupTestContainerAndBlob();

            try
            {
                string policyName = Utility.GenNameString("notexistpolicy");

                Test.Assert(!CommandAgent.NewAzureStorageBlobSAS(blobUtil.Container.Name, blobUtil.Blob.Name, policyName, string.Empty),
                    "Generate Blob sas token with not exist policy should fail");
                ExpectedContainErrorMessage(string.Format("Invalid access policy '{0}'.", policyName));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 3.	Generate SAS of a Blob with expiry time before start time
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewBlobSas)]
        public void NewBlobSasWithInvalidLifeTime()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            DateTime start = DateTime.UtcNow;
            DateTime end = start.AddHours(1.0);
            Test.Assert(!CommandAgent.NewAzureStorageBlobSAS(containerName, blobName, string.Empty, string.Empty, end, start),
                    "Generate Blob sas token with invalid should fail");
            ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");
        }

        /// <summary>
        /// 4.	Return full uri with SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewBlobSas)]
        public void NewBlobSasWithFullUri()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string blobPermission = Utility.GenRandomCombination(Utility.BlobPermission);
                string fullUri = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                string sasToken = (lang == Language.PowerShell ? fullUri.Substring(fullUri.IndexOf("?")) : fullUri);
                ValidateSasToken(blobUtil.Blob, blobPermission, sasToken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1.	Generate SAS of a Blob with only limited access right(read,write,delete,list,none)
        ///     Verify access with the non-granted right to this blob is denied
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewBlobSas)]
        public void NewBlobSasWithLimitedPermission()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                //Blob read permission
                string blobPermission = "r";
                string limitedPermission = lang == Language.PowerShell ? "wdac" : "wd";
                string sastoken = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                ValidateLimitedSasPermission(blobUtil.Blob, limitedPermission, sastoken);

                //Blob write permission
                blobPermission = "w";
                limitedPermission = "rd";
                sastoken = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                ValidateLimitedSasPermission(blobUtil.Blob, limitedPermission, sastoken);

                //Blob delete permission
                blobPermission = "d";
                limitedPermission = lang == Language.PowerShell ? "rwac" : "rw";
                sastoken = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                ValidateLimitedSasPermission(blobUtil.Blob, limitedPermission, sastoken);

                if (lang == Language.PowerShell)
                {
                    //Blob add permission
                    blobPermission = "a";
                    limitedPermission = "rdwc";
                    sastoken = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                    ValidateLimitedSasPermission(blobUtil.Blob, limitedPermission, sastoken);

                    //Blob create permission
                    blobPermission = "c";
                    limitedPermission = "rdwa";
                    sastoken = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                    ValidateLimitedSasPermission(blobUtil.Blob, limitedPermission, sastoken);
                }

                //Blob none permission
                //blobPermission = "";
                //limitedPermission = "rwd";
                //sastoken = agent.GetBlobSasFromPsCmd(blobUtil.Blob, string.Empty, blobPermission);
                //ValidateLimitedSasPermission(blobUtil.Blob, limitedPermission, sastoken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 5.	Generate SAS of a blob with name containing ?#%=&-
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewBlobSas)]
        public void NewBlobSasWithSpecialCharacters()
        {
            blobUtil.SetupTestContainerAndBlob();
            string specialBlobName = Utility.GenNameString(TestBase.SpecialChars);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(blobUtil.Container, specialBlobName);
                string permisson = "r";
                string fullUri = CommandAgent.GetBlobSasFromCmd(blob, string.Empty, permisson, null, null, true);
                string sasToken = (lang == Language.PowerShell ? fullUri.Substring(fullUri.IndexOf("?")) : fullUri);
                blobUtil.ValidateBlobReadableWithSasToken(blob, sasToken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 4.	Generate shared access signature of a non-existing blob or a non-existing container
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewBlobSas)]
        public void NewBlobSasWithNotExistBlob()
        {           
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CommandAgent.GetBlobSasFromCmd(containerName, blobName, string.Empty, string.Empty);
        }

        /// <summary>
        /// 1.	Generate SAS of protocal: HttpsorHttp, and all available value of permission. 
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        public void NewBlobSas_HttpsOrHttp()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string fullUri = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, "rwd", null, null, true, SharedAccessProtocol.HttpsOrHttp);
                string sasToken = (lang == Language.PowerShell ? fullUri.Substring(fullUri.IndexOf("?")) : fullUri);

                blobUtil.ValidateBlobReadableWithSasToken(blobUtil.Blob, sasToken, useHttps: false); 
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1.	Generate SAS of IPAddressOrRange: [Range include Current IP], and all available value of permission, protocal (sas URL).
        /// Try to download Copy a blob with the SAS  
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        public void NewBlobSas_IncludeIPRange()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string sastoken = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, "rwd", null, null, false, null, "0.0.0.0-255.255.255.255");

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
        /// <param name="blobPermission">Blob permission</param>
        internal void GenerateSasTokenAndValidate(string blobPermission)
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string sastoken = CommandAgent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                ValidateSasToken(blobUtil.Blob, blobPermission, sastoken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// Validate the sas token 
        /// </summary>
        /// <param name="blob">CloudBlob object</param>
        /// <param name="blobPermission">Blob permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateSasToken(CloudBlob blob, string blobPermission, string sasToken)
        {
            foreach (char permission in blobPermission.ToLower())
            {
                switch (permission)
                {
                    case 'r':
                        blobUtil.ValidateBlobReadableWithSasToken(blob, sasToken);
                        break;
                    case 'w':
                        blobUtil.ValidateBlobWriteableWithSasToken(blob, sasToken);
                        break;
                    case 'd':
                        blobUtil.ValidateBlobDeleteableWithSasToken(blob, sasToken);
                        break;
                    case 'c':
                        blobUtil.ValidateBlobCreateableWithSasToken(blob, sasToken);
                        break;
                    case 'a':
                        blobUtil.ValidateBlobAppendableWithSasToken(blob, sasToken);
                        break;
                }
            }
        }

        /// <summary>
        /// Validte the limited permission for sas token 
        /// </summary>
        /// <param name="blob">CloudBlob object</param>
        /// <param name="BlobPermission">Limited permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateLimitedSasPermission(CloudBlob blob,
            string limitedPermission, string sasToken)
        {
            foreach (char permission in limitedPermission.ToLower())
            {
                try
                {
                    ValidateSasToken(blob, permission.ToString(), sasToken);
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
