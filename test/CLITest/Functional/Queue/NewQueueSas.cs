namespace Management.Storage.ScenarioTest.Functional.Queue
{
    using Management.Storage.ScenarioTest.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Queue.Protocol;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    [TestClass]
    public class NewQueueSas : TestBase
    {
        [ClassInitialize()]
        public static void NewQueueSasClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void NewQueueSasClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// 1.	Generate SAS of a queue with only limited access right(read, write,delete,list)
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewQueueSas)]
        public void NewQueueSasWithPermission()
        {
            //queue read permission
            string queuePermission = "r";
            GenerateSasTokenAndValidate(queuePermission);

            //queue add permission
            queuePermission = "a";
            GenerateSasTokenAndValidate(queuePermission);

            //queue update permission
            queuePermission = "u";
            GenerateSasTokenAndValidate(queuePermission);

            //queue update permission
            queuePermission = "p";
            GenerateSasTokenAndValidate(queuePermission);

            // Permission param is required according to the design, cannot accept string.Empty, so comment this. We may support this in the future.
            //None permission
            //queuePermission = "";
            //GenerateSasTokenAndValidate(queuePermission);

            //Random combination
            queuePermission = Utility.GenRandomCombination(Utility.QueuePermission);
            GenerateSasTokenAndValidate(queuePermission);
        }

        /// <summary>
        /// 2.	Generate SAS of a queue with a limited time period
        /// Wait for the time expiration
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewQueueSas)]
        public void NewQueueSasWithLifeTime()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            double lifeTime = 3; //Minutes
            const double deltaTime = 0.5;
            DateTime startTime = DateTime.Now.AddMinutes(lifeTime);
            DateTime expiryTime = startTime.AddMinutes(lifeTime);

            try
            {
                string queuePermission = Utility.GenRandomCombination(Utility.QueuePermission);
                string sastoken = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, queuePermission, startTime, expiryTime);
                try
                {
                    ValidateSasToken(queue, queuePermission, sastoken);
                    Test.Error(string.Format("Access queue should fail since the start time is {0}, but now is {1}",
                        startTime.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
                queue.Clear();

                Test.Info("Sleep and wait for the sas token start time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));
                ValidateSasToken(queue, queuePermission, sastoken);
                Test.Info("Sleep and wait for sas token expiry time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));

                try
                {
                    ValidateSasToken(queue, queuePermission, sastoken);
                    Test.Error(string.Format("Access queue should fail since the expiry time is {0}, but now is {1}",
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
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// 3.	Generate SAS of a queue by policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewQueueSas)]
        public void NewQueueSasWithPolicy()
        {
            CloudQueue queue = queueUtil.CreateQueue();

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                QueuePermissions permission = new QueuePermissions();
                string policyName = Utility.GenNameString("saspolicy");

                permission.SharedAccessPolicies.Add(policyName, new SharedAccessQueuePolicy
                {
                    SharedAccessExpiryTime = DateTime.Now.Add(sasLifeTime),
                    Permissions = SharedAccessQueuePermissions.Read,
                });

                queue.SetPermissions(permission);

                string sasToken = CommandAgent.GetQueueSasFromCmd(queue.Name, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                ValidateSasToken(queue, "r", sasToken);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// 4.	Generate SAS of a queue of a non-existing policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        public void NewQueueSasWithNotExistPolicy()
        {
            CloudQueue queue = queueUtil.CreateQueue();

            try
            {
                string policyName = Utility.GenNameString("notexistpolicy");

                Test.Assert(!CommandAgent.NewAzureStorageQueueSAS(queue.Name, policyName, string.Empty),
                    "Generate queue sas token with not exist policy should fail");
                ExpectedContainErrorMessage(string.Format("Invalid access policy '{0}'.", policyName));
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// 3.	Generate SAS of a queue with expiry time before start time
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewQueueSas)]
        public void NewQueueSasWithInvalidLifeTime()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            try
            {
                DateTime start = DateTime.UtcNow;
                DateTime end = start.AddHours(1.0);
                Test.Assert(!CommandAgent.NewAzureStorageQueueSAS(queue.Name, string.Empty, "r", end, start),
                        "Generate queue sas token with invalid should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// 4.	Return full uri with SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewQueueSas)]
        public void NewQueueSasWithFullUri()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            try
            {
                string queuePermission = Utility.GenRandomCombination(Utility.QueuePermission);
                string fullUri = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, queuePermission);
                string sasToken = (lang == Language.PowerShell ? fullUri.Substring(fullUri.IndexOf("?")) : fullUri);
                ValidateSasToken(queue, queuePermission, sasToken);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// 1.	Generate SAS of a queue with only limited access right(read,write,delete,list,none)
        ///     Verify access with the non-granted right to this queue is denied
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewQueueSas)]
        public void NewQueueSasWithLimitedPermission()
        {
            CloudQueue queue = queueUtil.CreateQueue();

            try
            {
                //queue read permission
                string queuePermission = "r";
                string limitedPermission = "aup";
                string sastoken = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, queuePermission);
                ValidateLimitedSasPermission(queue, limitedPermission, sastoken);

                //queue add permission
                queuePermission = "a";
                limitedPermission = "rup";
                sastoken = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, queuePermission);
                ValidateLimitedSasPermission(queue, limitedPermission, sastoken);

                //queue update permission
                queuePermission = "u";
                limitedPermission = "rap";
                sastoken = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, queuePermission);
                ValidateLimitedSasPermission(queue, limitedPermission, sastoken);

                //queue process permission
                queuePermission = "p";
                limitedPermission = "rau";
                sastoken = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, queuePermission);
                ValidateLimitedSasPermission(queue, limitedPermission, sastoken);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// 4.	Generate shared access signature of a non-existing queue or a non-existing container
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewQueueSas)]
        public void NewQueueSasWithNotExistQueue()
        {
            string queueName = Utility.GenNameString("queue");
            CommandAgent.GetQueueSasFromCmd(queueName, string.Empty, "r");
        }

        /// <summary>
        /// 1.	Generate SAS of protocal: HttpsOnly, and all available value of permission. 
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        public void NewQueueSas_Httpsonly()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            try
            {
                string sastoken = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, "raup", null, null, false, SharedAccessProtocol.HttpsOnly);

                queueUtil.ValidateQueueAddableWithSasToken(queue, sastoken);

                try
                {
                    queueUtil.ValidateQueueAddableWithSasToken(queue, sastoken, useHttps: false);
                    Test.Error(string.Format("Queue Add with http should fail since the sas is HttpsOnly."));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(306, e.RequestInformation.HttpStatusCode, "Protocal not match error: ");
                }
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }


        /// <summary>
        /// 1.	Generate SAS of IPAddressOrRange: [Not Current IP], and all available value of permission, protocal. 
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        public void NewQueueSas_NotCurrentIP()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            try
            {
                string sastoken = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, "raup", null, null, false, null, "2.3.4.5");

                try
                {
                    queueUtil.ValidateQueueReadableWithSasToken(queue, sastoken);
                    Test.Error(string.Format("Queue read hould fail since the ipAcl is not current IP."));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }


        /// <summary>
        /// 1.	Generate SAS of IPAddressOrRange: [Range include Current IP], and all available value of permission. 
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.NewQueueSas)]
        public void NewQueueSas_IncludeIPRange()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            try
            {
                string sastoken = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, "raup", null, null, false, null, "0.0.0.0-255.255.255.255");
                queueUtil.ValidateQueueUpdateableWithSasToken(queue, sastoken);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// Generate a sas token and validate it.
        /// </summary>
        /// <param name="queuePermission">Queue permission</param>
        internal void GenerateSasTokenAndValidate(string queuePermission)
        {
            CloudQueue queue = queueUtil.CreateQueue();
            try
            {
                string sastoken = CommandAgent.GetQueueSasFromCmd(queue.Name, string.Empty, queuePermission);
                ValidateSasToken(queue, queuePermission, sastoken);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// Validate the sas token 
        /// </summary>
        /// <param name="queue">CloudQueue object</param>
        /// <param name="queuePermission">Queue permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateSasToken(CloudQueue queue, string queuePermission, string sasToken)
        {
            foreach (char permission in queuePermission.ToLower())
            {
                switch (permission)
                {
                    case 'r':
                        queueUtil.ValidateQueueReadableWithSasToken(queue, sasToken);
                        break;
                    case 'a':
                        queueUtil.ValidateQueueAddableWithSasToken(queue, sasToken);
                        break;
                    case 'u':
                        queueUtil.ValidateQueueUpdateableWithSasToken(queue, sasToken);
                        break;
                    case 'p':
                        queueUtil.ValidateQueueProcessableWithSasToken(queue, sasToken);
                        break;
                }
            }
        }

        /// <summary>
        /// Validte the limited permission for sas token 
        /// </summary>
        /// <param name="queue">CloudQueue object</param>
        /// <param name="queuePermission">Limited permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateLimitedSasPermission(CloudQueue queue,
            string limitedPermission, string sasToken)
        {
            foreach (char permission in limitedPermission.ToLower())
            {
                try
                {
                    ValidateSasToken(queue, permission.ToString(), sasToken);
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
