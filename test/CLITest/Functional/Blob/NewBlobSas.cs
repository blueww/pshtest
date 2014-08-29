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
        public void NewBlobSasWithLifeTime()
        {
            blobUtil.SetupTestContainerAndBlob();
            double lifeTime = 3; //Minutes
            const double deltaTime = 0.5;
            DateTime startTime = DateTime.Now.AddMinutes(lifeTime);
            DateTime expiryTime = startTime.AddMinutes(lifeTime);

            try
            {
                string blobPermission = Utility.GenRandomCombination(Utility.BlobPermission);
                string sastoken = agent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission, startTime, expiryTime);
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
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));
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
                });

                blobUtil.Container.SetPermissions(permission);
                string sasToken = agent.GetBlobSasFromCmd(blobUtil.Blob, policyName, string.Empty);
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

                Test.Assert(!agent.NewAzureStorageBlobSAS(blobUtil.Container.Name, blobUtil.Blob.Name, policyName, string.Empty),
                    "Generate Blob sas token with not exist policy should fail");
                ExpectedEqualErrorMessage(string.Format("Invalid access policy '{0}'.", policyName));
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
        public void NewBlobSasWithInvalidLifeTime()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            DateTime start = DateTime.UtcNow;
            DateTime end = start.AddHours(1.0);
            Test.Assert(!agent.NewAzureStorageBlobSAS(containerName, blobName, string.Empty, string.Empty, end, start),
                    "Generate Blob sas token with invalid should fail");
            ExpectedStartsWithErrorMessage("The expiry time of the specified access policy should be greater than start time.");
        }

        /// <summary>
        /// 4.	Return full uri with SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewBlobSas)]
        public void NewBlobSasWithFullUri()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string blobPermission = Utility.GenRandomCombination(Utility.BlobPermission);
                string fullUri = agent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                string sasToken = fullUri.Substring(fullUri.IndexOf("?"));
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
        public void NewBlobSasWithLimitedPermission()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                //Blob read permission
                string blobPermission = "r";
                string limitedPermission = "wd";
                string sastoken = agent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                ValidateLimitedSasPermission(blobUtil.Blob, limitedPermission, sastoken);

                //Blob write permission
                blobPermission = "w";
                limitedPermission = "rd";
                sastoken = agent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                ValidateLimitedSasPermission(blobUtil.Blob, limitedPermission, sastoken);

                //Blob delete permission
                blobPermission = "d";
                limitedPermission = "rw";
                sastoken = agent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
                ValidateLimitedSasPermission(blobUtil.Blob, limitedPermission, sastoken);

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
        public void NewBlobSasWithSpecialCharacters()
        {
            blobUtil.SetupTestContainerAndBlob();
            string specialBlobName = Utility.GenNameString(TestBase.SpecialChars);

            try
            {
                ICloudBlob blob = blobUtil.CreateRandomBlob(blobUtil.Container, specialBlobName);
                string permisson = "r";
                string fullUri = agent.GetBlobSasFromCmd(blob, string.Empty, permisson, null, null, true);
                string sasToken = fullUri.Substring(fullUri.IndexOf("?"));
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
        public void NewBlobSasWithNotExistBlob()
        {           
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            agent.GetBlobSasFromCmd(containerName, blobName, string.Empty, string.Empty);
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
                string sastoken = agent.GetBlobSasFromCmd(blobUtil.Blob, string.Empty, blobPermission);
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
        /// <param name="blob">ICloudBlob object</param>
        /// <param name="blobPermission">Blob permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateSasToken(ICloudBlob blob, string blobPermission, string sasToken)
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
                }
            }
        }

        /// <summary>
        /// Validte the limited permission for sas token 
        /// </summary>
        /// <param name="blob">ICloudBlob object</param>
        /// <param name="BlobPermission">Limited permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateLimitedSasPermission(ICloudBlob blob,
            string limitedPermission, string sasToken)
        {
            try
            {
                ValidateSasToken(blob, limitedPermission, sasToken);
                Test.Error("sastoken '{0}' should not contain the permission {1}", limitedPermission);
            }
            catch (StorageException e)
            {
                Test.Info(e.Message);
                if (403 == e.RequestInformation.HttpStatusCode || 404 == e.RequestInformation.HttpStatusCode)
                {
                    Test.Info("Limited permission sas token should not access storage objects. {0}", e.RequestInformation.HttpStatusMessage);
                }
                else
                {
                    Test.Error("Limited permission sas token should return 403 or 404, but actually it's {0} {1}",
                        e.RequestInformation.HttpStatusCode, e.RequestInformation.HttpStatusMessage);
                }
            }
        }
    }
}
