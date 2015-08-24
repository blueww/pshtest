namespace Management.Storage.ScenarioTest.Functional.Queue
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Queue.Protocol;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    public class QueueStoredAccessPolicy : TestBase
    {
        [ClassInitialize()]
        public static void QueueStoredAccessPolicyClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void QueueStoredAccessPolicyClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Create stored access policy with differient policy name: length 1, 5, 64, ASCII, Unicode, test plan positive functional 8.42.1
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewPolicyDifferentNames()
        {
            DateTime? expiryTime = DateTime.Today.AddDays(10);
            DateTime? startTime = DateTime.Today.AddDays(-2);
            string permission = "raup";
            string queueName = Utility.GenNameString("queue");

            try
            {
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 0), permission, startTime, expiryTime, queueName, false);
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 0), permission, startTime, expiryTime, queueName, false);
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 4), permission, startTime, expiryTime, queueName, false);
                CreateStoredAccessPolicyAndValidate(FileNamingGenerator.GenerateValidASCIIOptionValue(64), permission, startTime, expiryTime, queueName, false);
                foreach (var policyName in FileNamingGenerator.GenerateValidateUnicodeName(40))
                {
                    CreateStoredAccessPolicyAndValidate(policyName, permission, startTime, expiryTime, queueName, false);
                }
            }
            finally
            {
                queueUtil.RemoveQueue(queueName);
            }
        }

        /// <summary>
        /// Test plan positive functional 8.42.2, 8.42.3,8.42.2,8.42.4,8.42.5,
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewPolicyDifferentValues()
        {
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessQueuePolicy>();

            string queueName = Utility.GenNameString("table");
            CloudQueue queue = queueUtil.CreateQueue(queueName);
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);

            try
            {
                foreach (Utility.RawStoredAccessPolicy rawPolicy in samplePolicies)
                {
                    CreateStoredAccessPolicy(rawPolicy.PolicyName, rawPolicy.Permission, rawPolicy.StartTime, rawPolicy.ExpiryTime, queue, false);
                }

                Utility.WaitForPolicyBecomeValid<CloudQueue>(queue, expectedCount: samplePolicies.Count);

                SharedAccessQueuePolicies expectedPolicies = new SharedAccessQueuePolicies();
                foreach (Utility.RawStoredAccessPolicy rawPolicy in samplePolicies)
                {
                    expectedPolicies.Add(rawPolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(rawPolicy.StartTime, rawPolicy.ExpiryTime, rawPolicy.Permission));
                }

                Utility.ValidateStoredAccessPolicies<SharedAccessQueuePolicy>(queue.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                queueUtil.RemoveQueue(queueName);
            }
        }

        /// <summary>
        /// Test plan negative functional 8.42.1, 8.42.2,8.42.3,8.42.4,8.42.5,8.42.6
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewPolicyInvalidParameter()
        {
            DateTime? startTime = DateTime.Today.AddDays(1);
            DateTime? expiryTime = DateTime.Today.AddDays(-1);
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);

            try
            {
                Test.Assert(!agent.NewAzureStorageQueueStoredAccessPolicy("CONTAINER", Utility.GenNameString("p", 5), null, null, null), "Create stored acess policy for invalid queue name CONTAINER should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters.");
                }
                else
                {
                    ExpectedContainErrorMessage("Queue name format is incorrect");
                }

                Test.Assert(!agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, Utility.GenNameString("p", 5), null, startTime, expiryTime), "Create stored access policy for ExpiryTime earlier than StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, Utility.GenNameString("p", 5), null, startTime, startTime), "Create stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, Utility.GenNameString("p", 5), "x", null, null), "Create stored access policy with invalid permission should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Invalid access permission");
                }
                else
                {
                    ExpectedContainErrorMessage("Invalid value: x. Options are: r,a,u,p");
                }

                Test.Assert(!agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, FileNamingGenerator.GenerateValidASCIIOptionValue(65), null, null, null), "Create stored access policy with invalid name length should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Valid names should be 1 through 64 characters long.");
                }
                else
                {
                    ExpectedContainErrorMessage("Reason: Signed identifier ID cannot be empty or over 64 characters in length");
                }

                for (int i = 1; i <= 5; i++)
                {
                    agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, Utility.GenNameString("p", i), null, null, null);
                }

                Test.Assert(!agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, Utility.GenNameString("p", 6), null, null, null), "Create more than 5 stored access policies should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Too many '6' shared access policy identifiers provided");
                }
                else
                {
                    ExpectedContainErrorMessage("A maximum of 5 access policies may be set");
                }

                string queueName = Utility.GenNameString("queue");
                queueUtil.RemoveQueue(queueName);
                Test.Assert(!agent.NewAzureStorageQueueStoredAccessPolicy(queueName, Utility.GenNameString("p", 5), null, null, null), "Create stored access policy against non-existing container should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("does not exist");
                }
                else
                {
                    ExpectedContainErrorMessage("The specified queue does not exist");
                }
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// Test plan, positive functional 8.43.1, 8.43.2
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetPolicyVariations()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);

            try
            {
                //empty policies
                Test.Assert(agent.GetAzureStorageQueueStoredAccessPolicy(queue.Name, null),
                    "Get stored access policy in queue should succeed");
                Test.Info("Get stored access policy");
                Assert.IsTrue(agent.Output.Count == 0);

                //get all policies
                List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessQueuePolicy>();
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                foreach (Utility.RawStoredAccessPolicy samplePolicy in samplePolicies)
                {
                    CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, queue, false);
                    SharedAccessQueuePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission);
                    comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessQueuePolicy>(policy, samplePolicy.PolicyName));
                }

                Test.Assert(agent.GetAzureStorageQueueStoredAccessPolicy(queue.Name, null),
                    "Get stored access policy in table should succeed");
                Test.Info("Get stored access policy");
                agent.OutputValidation(comp);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// Test plan, negative functional 8.43.1, 8.43.2, 8.43.3, 8.43.4
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetPolicyInvalid()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);

            try
            {
                string policyName = "policy";
                Test.Assert(!agent.GetAzureStorageQueueStoredAccessPolicy(queue.Name, policyName),
                    "Get non-existing stored access policy should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("The policy {0} doesn't exist", policyName));
                }

                string invalidName = FileNamingGenerator.GenerateValidASCIIOptionValue(65);
                Test.Assert(!agent.GetAzureStorageQueueStoredAccessPolicy(queue.Name, invalidName),
                    "Get stored access policy with name length larger than 64 should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("The policy {0} doesn't exist", invalidName));
                }

                Test.Assert(!agent.GetAzureStorageQueueStoredAccessPolicy("CONTAINER", policyName),
                    "Get stored access policy from invalid queue name should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters");
                }
                else
                {
                    ExpectedContainErrorMessage("Queue name format is incorrect");
                }

                string queueName = Utility.GenNameString("queue");
                queueUtil.RemoveQueue(queueName);
                Test.Assert(!agent.GetAzureStorageQueueStoredAccessPolicy(queueName, policyName),
                    "Get stored access policy from invalid queue name should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The specified queue does not exist.");
                }
                else
                {
                    ExpectedContainErrorMessage("The specified queue does not exist.");
                }
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// Test plan negative 8.44.1, 8.44.2,8.44.3,8.44.4
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void RemovePolicyInvalid()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);

            try
            {
                string policyName = "policy";
                Test.Assert(!agent.RemoveAzureStorageQueueStoredAccessPolicy(queue.Name, policyName),
                    "Remove non-existing stored access policy should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("The policy {0} doesn't exist", policyName));
                }

                string invalidName = FileNamingGenerator.GenerateValidASCIIOptionValue(65);
                Test.Assert(!agent.RemoveAzureStorageQueueStoredAccessPolicy(queue.Name, invalidName),
                    "Remove stored access policy with name length larger than 64 should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("The policy {0} doesn't exist", invalidName));
                }

                Test.Assert(!agent.RemoveAzureStorageQueueStoredAccessPolicy("CONTAINER", policyName),
                    "Remove stored access policy from invalid queue name should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters");
                }
                else
                {
                    ExpectedContainErrorMessage("Queue name format is incorrect");
                }

                string queueName = Utility.GenNameString("queue");
                queueUtil.RemoveQueue(queueName);
                Test.Assert(!agent.RemoveAzureStorageQueueStoredAccessPolicy(queueName, policyName),
                    "Remove stored access policy from invalid table name should fail");
                ExpectedContainErrorMessage("The specified queue does not exist");
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// Test plan positive functional: 8.45.1
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyDifferentNames()
        {
            DateTime? expiryTime1 = DateTime.Today.AddDays(10);
            DateTime? startTime1 = DateTime.Today.AddDays(-2);
            DateTime? expiryTime2 = DateTime.Today.AddDays(11);
            DateTime? startTime2 = DateTime.Today.AddDays(-1);
            string permission = "raup";
            string policyName = Utility.GenNameString("p", 0);
            Utility.RawStoredAccessPolicy policy1 = new Utility.RawStoredAccessPolicy(policyName, startTime1, expiryTime1, permission);
            Utility.RawStoredAccessPolicy policy2 = new Utility.RawStoredAccessPolicy(policyName, startTime2, expiryTime2, permission);
            SetStoredAccessPolicyAndValidate(policy1, policy2);

            policy1.PolicyName = policy2.PolicyName = Utility.GenNameString("p", 4);
            SetStoredAccessPolicyAndValidate(policy1, policy2);

            policy1.PolicyName = policy2.PolicyName = FileNamingGenerator.GenerateValidASCIIOptionValue(64);
            SetStoredAccessPolicyAndValidate(policy1, policy2);

            foreach (var samplePolicyName in FileNamingGenerator.GenerateValidateUnicodeName(40))
            {
                policy1.PolicyName = policy2.PolicyName = samplePolicyName;
                SetStoredAccessPolicyAndValidate(policy1, policy2);
            }
        }

        /// <summary>
        /// Test plan positive functional: 8.45.2, 8.45.3,8.45.4,8.45.5, 8.45.6
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyDifferentValues()
        {
            string queueName = Utility.GenNameString("queue");
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessQueuePolicy>();
            samplePolicies[4].PolicyName = samplePolicies[3].PolicyName = samplePolicies[2].PolicyName = samplePolicies[1].PolicyName = samplePolicies[0].PolicyName;
            samplePolicies[2].ExpiryTime = DateTime.Today.AddDays(3);
            SetStoredAccessPolicyAndValidate(samplePolicies[0], samplePolicies[1], queueName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[1], samplePolicies[2], queueName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[2], samplePolicies[3], queueName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[3], samplePolicies[4], queueName, true, true);
        }

        /// <summary>
        /// Test plan positive functional: 8.41.6
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyNoStartTimeNoExpiryTime()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessQueuePolicy>()[0];
            double effectiveTime = 30;

            try
            {
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, queue);

                //NoStartTime
                Test.Assert(agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, null, null, null, true, false),
                    "Set stored access policy with -NoStartTime should succeed");
                Thread.Sleep(TimeSpan.FromSeconds(effectiveTime));
                SharedAccessQueuePolicies expectedPolicies = new SharedAccessQueuePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(null, samplePolicy.ExpiryTime, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessQueuePolicy>(queue.GetPermissions().SharedAccessPolicies, expectedPolicies);
                SharedAccessQueuePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(null, samplePolicy.ExpiryTime, samplePolicy.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessQueuePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);

                //NoExpiryTime
                Test.Assert(agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, null, null, null, false, true),
                    "Set stored access policy with -NoExpiryTime should succeed");
                Thread.Sleep(TimeSpan.FromSeconds(effectiveTime));
                expectedPolicies = new SharedAccessQueuePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(null, null, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessQueuePolicy>(queue.GetPermissions().SharedAccessPolicies, expectedPolicies);
                policy = Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(null, null, samplePolicy.Permission);
                comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessQueuePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);

                //both
                Utility.ClearStoredAccessPolicy<CloudQueue>(queue);
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, queue);

                Test.Assert(agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, null, null, null, true, true),
                    "Set stored access policy with both -NoStartTime and -NoExpiryTime should succeed");
                Thread.Sleep(TimeSpan.FromSeconds(effectiveTime));
                expectedPolicies = new SharedAccessQueuePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(null, null, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessQueuePolicy>(queue.GetPermissions().SharedAccessPolicies, expectedPolicies);
                policy = Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(null, null, samplePolicy.Permission);
                comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessQueuePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// Test plan 8.45.1 -5 negative cases
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyInvalidParameter()
        {
            DateTime? startTime = DateTime.Today.AddDays(1);
            DateTime? expiryTime = DateTime.Today.AddDays(-1);
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);

            try
            {
                Test.Assert(!agent.SetAzureStorageQueueStoredAccessPolicy("CONTAINER", Utility.GenNameString("p", 5), null, null, null), "Set stored acess policy for invalid queue name CONTAINER should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters.");
                }
                else
                {
                    ExpectedContainErrorMessage("Queue name format is incorrect");
                }

                Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessQueuePolicy>()[0];
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, queue);
                Test.Assert(!agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, null, startTime, expiryTime), "Set stored access policy for ExpiryTime earlier than StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, null, startTime, startTime), "Set stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, "x", null, null), "Set stored access policy with invalid permission should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Invalid access permission");
                }
                else
                {
                    ExpectedContainErrorMessage("Invalid value: x. Options are: r,a,u,p");
                }

                string invalidName = FileNamingGenerator.GenerateValidASCIIOptionValue(65);
                Test.Assert(!agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, invalidName, null, null, null), "Create stored access policy with invalid name length should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("The policy {0} doesn't exist", invalidName));
                }

                if (lang == Language.PowerShell)
                {
                    Test.Assert(!agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, null, true, false), "Setting both -StartTime and -NoStartTime should fail");
                    ExpectedContainErrorMessage("Parameter -StartTime and -NoStartTime are mutually exclusive");
                }
                else
                {
                    Test.Assert(agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, null, true, false), "Setting both -StartTime and -NoStartTime should succeed");
                }

                if (lang == Language.PowerShell)
                {
                    Test.Assert(!agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, samplePolicy.Permission, null, samplePolicy.ExpiryTime, false, true), "Setting both -ExpiryTime and -NoExpiryTime should fail");
                    ExpectedContainErrorMessage("Parameter -ExpiryTime and -NoExpiryTime are mutually exclusive");
                }
                else
                {
                    Test.Assert(agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, samplePolicy.Permission, null, samplePolicy.ExpiryTime, false, true), "Setting both -ExpiryTime and -NoExpiryTime should succeed");
                }

                queueUtil.RemoveQueue(queue);
                Test.Assert(!agent.SetAzureStorageTableStoredAccessPolicy(queue.Name, Utility.GenNameString("p", 5), null, null, null), "Set stored access policy against non-existing queue should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("does not exist");
                }
                else
                {
                    ExpectedContainErrorMessage("Reason:");
                }
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// test plan functional 8.41.7
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void PolicyWithSASPermission()
        {
            TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
            string policyName = Utility.GenNameString("saspolicy");
            DateTime? expiryTime = DateTime.Today.AddDays(10);
            DateTime? startTime = DateTime.Today.AddDays(-2);
            string permission = "raup";
            CloudQueue queue = queueUtil.CreateQueue();

            try
            {
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, queue, false);
                string sasToken = agent.GetQueueSasFromCmd(queue.Name, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                queueUtil.ValidateQueueAddableWithSasToken(queue, sasToken);


                //remove the Add permission
                permission = "r";
                agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, policyName, permission, null, null);
                Test.Info("Sleep and wait for sas policy taking effect");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));

                try
                {
                    queueUtil.ValidateQueueAddableWithSasToken(queue, sasToken);
                    Test.Error("Add in queue should fail since Add permission is removed");
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 404, "(404) Not Found");
                }

                //add back the Add permission
                permission = "ra";
                agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, policyName, permission, null, null);
                Test.Info("Sleep and wait for sas policy taking effect");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                queueUtil.ValidateQueueAddableWithSasToken(queue, sasToken);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        internal void CreateStoredAccessPolicyAndValidate(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, string queueName = null, bool ifCleanUpQueue = true, bool ifCleanUpPolicy = true)
        {
            CloudQueue queue = queueUtil.CreateQueue(queueName);

            try
            {
                //create the policy
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, queue, ifCleanUpPolicy);

                //get the policy and validate
                SharedAccessQueuePolicies expectedPolicies = new SharedAccessQueuePolicies();
                expectedPolicies.Add(policyName, Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(startTime, expiryTime, permission));

                Utility.RawStoredAccessPolicy policy = new Utility.RawStoredAccessPolicy(policyName, startTime, expiryTime, permission);
                Utility.WaitForPolicyBecomeValid<CloudQueue>(queue, policy); 
                Utility.ValidateStoredAccessPolicies<SharedAccessQueuePolicy>(queue.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                if (ifCleanUpPolicy)
                {
                    Utility.ClearStoredAccessPolicy<CloudQueue>(queue);
                }
                if (ifCleanUpQueue)
                {
                    queueUtil.RemoveQueue(queue);
                }
            }
        }

        internal void CreateStoredAccessPolicy(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, CloudQueue queue, bool ifCleanUpPolicy = true)
        {
            if (ifCleanUpPolicy)
            {
                Utility.ClearStoredAccessPolicy<CloudQueue>(queue);
            }

            Test.Assert(agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, policyName, permission, startTime, expiryTime),
                "Create stored access policy in queue should succeed");
            Test.Info("Created stored access policy:{0}", policyName);
        }

        internal void SetStoredAccessPolicyAndValidate(Utility.RawStoredAccessPolicy policy1, Utility.RawStoredAccessPolicy policy2, string queueName = null, bool ifCleanupPolicy = true, bool ifCleanupQueue = true)
        {
            CloudQueue queue = queueUtil.CreateQueue(queueName);
            if (ifCleanupPolicy)
            {
                Utility.ClearStoredAccessPolicy<CloudQueue>(queue);
            }

            policy2.PolicyName = policy1.PolicyName;

            try
            {
                agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, policy1.PolicyName, policy1.Permission, policy1.StartTime, policy1.ExpiryTime);
                Test.Assert(agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, policy2.PolicyName, policy2.Permission, policy2.StartTime, policy2.ExpiryTime),
                "Set stored access policy in queue should succeed");
                Test.Info("Set stored access policy:{0}", policy2.PolicyName);

                //get the policy and validate
                SharedAccessQueuePolicies expectedPolicies = new SharedAccessQueuePolicies();
                if (policy2.StartTime == null)
                {
                    policy2.StartTime = policy1.StartTime;
                }
                if (policy2.ExpiryTime == null)
                {
                    policy2.ExpiryTime = policy1.ExpiryTime;
                }
                if (policy2.Permission == null)
                {
                    policy2.Permission = policy1.Permission;
                }

                expectedPolicies.Add(policy2.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(policy2.StartTime, policy2.ExpiryTime, policy2.Permission));
                Utility.WaitForPolicyBecomeValid<CloudQueue>(queue, policy2);
                Utility.ValidateStoredAccessPolicies<SharedAccessQueuePolicy>(queue.GetPermissions().SharedAccessPolicies, expectedPolicies);

                //validate the output
                SharedAccessQueuePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(policy2.StartTime, policy2.ExpiryTime, policy2.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessQueuePolicy>(policy, policy2.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                if (ifCleanupQueue)
                {
                    queueUtil.RemoveQueue(queue);
                }
            }
        }

    }
}
