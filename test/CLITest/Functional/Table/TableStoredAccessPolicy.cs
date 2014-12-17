namespace Management.Storage.ScenarioTest.Functional.Table
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    public class TableStoredAccessPolicy : TestBase
    {
        [ClassInitialize()]
        public static void TableStoredAccessPolicyClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void TableStoredAccessPolicyClassCleanup()
        {
            TestBase.TestClassCleanup();
        }


        /// <summary>
        /// Create stored access policy with differient policy name: length 1, 5, 64, ASCII, Unicode, test plan positive functional 8.38.1
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewPolicyDifferentNames()
        {
            DateTime? expiryTime = DateTime.Today.AddDays(10);
            DateTime? startTime = DateTime.Today.AddDays(-2);
            string permission = "audq";
            string tableName = Utility.GenNameString("table");

            try
            {
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 0), permission, startTime, expiryTime, tableName, false);
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 0), permission, startTime, expiryTime, tableName, false);
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 4), permission, startTime, expiryTime, tableName, false);
                CreateStoredAccessPolicyAndValidate(FileNamingGenerator.GenerateValidateASCIIName(64), permission, startTime, expiryTime, tableName, false);
                foreach (var policyName in FileNamingGenerator.GenerateValidateUnicodeName(40))
                {
                    CreateStoredAccessPolicyAndValidate(policyName, permission, startTime, expiryTime, tableName, false);
                }
            }
            finally
            {
                tableUtil.RemoveTable(tableName);
            }
        }

        /// <summary>
        /// Test plan positive functional 8.38.2, 8.38.3,8.38.2,8.38.4,8.38.5,
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewPolicyDifferentValues()
        {
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessTablePolicy>();

            string tableName = Utility.GenNameString("table");
            CloudTable table = tableUtil.CreateTable(tableName);
            Utility.ClearStoredAccessPolicy<CloudTable>(table);

            try
            {
                foreach (Utility.RawStoredAccessPolicy rawPolicy in samplePolicies)
                {
                    CreateStoredAccessPolicy(rawPolicy.PolicyName, rawPolicy.Permission, rawPolicy.StartTime, rawPolicy.ExpiryTime, table, false);
                }

                SharedAccessTablePolicies expectedPolicies = new SharedAccessTablePolicies();
                foreach (Utility.RawStoredAccessPolicy rawPolicy in samplePolicies)
                {
                    expectedPolicies.Add(rawPolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(rawPolicy.StartTime, rawPolicy.ExpiryTime, rawPolicy.Permission));
                }

                Utility.ValidateStoredAccessPolicies<SharedAccessTablePolicy>(table.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                tableUtil.RemoveTable(tableName);
            }
        }

        /// <summary>
        /// Test plan negative functional 8.38.1, 8.38.2,8.38.3,8.38.4,8.38.5,8.38.6
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewPolicyInvalidParameter()
        {
            DateTime? startTime = DateTime.Today.AddDays(1);
            DateTime? expiryTime = DateTime.Today.AddDays(-1);
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);

            try
            {                
                Test.Assert(!agent.NewAzureStorageTableStoredAccessPolicy("CONTAINER", Utility.GenNameString("p", 5), null, null, null), "Create stored acess policy for invalid table name CONTAINER should fail");
                ExpectedContainErrorMessage("The table specified does not exist.");

                Test.Assert(!agent.NewAzureStorageTableStoredAccessPolicy(table.Name, Utility.GenNameString("p", 5), null, startTime, expiryTime), "Create stored access policy for ExpiryTime earlier than StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time.");

                Test.Assert(!agent.NewAzureStorageTableStoredAccessPolicy(table.Name, Utility.GenNameString("p", 5), null, startTime, startTime), "Create stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time.");

                Test.Assert(!agent.NewAzureStorageTableStoredAccessPolicy(table.Name, Utility.GenNameString("p", 5), "x", null, null), "Create stored access policy with invalid permission should fail");
                ExpectedContainErrorMessage("Invalid access permission");

                Test.Assert(!agent.NewAzureStorageTableStoredAccessPolicy(table.Name, FileNamingGenerator.GenerateValidateASCIIName(65), null, null, null), "Create stored access policy with invalid name length should fail");
                ExpectedContainErrorMessage("Valid names should be 1 through 64 characters long.");

                for (int i = 1; i <= 5; i++)
                {
                    agent.NewAzureStorageTableStoredAccessPolicy(table.Name, Utility.GenNameString("p", i), null, null, null);
                }

                Test.Assert(!agent.NewAzureStorageTableStoredAccessPolicy(table.Name, Utility.GenNameString("p", 6), null, null, null), "Create more than 5 stored access policies should fail");
                ExpectedContainErrorMessage("Too many '6' shared access policy identifiers provided");

                tableUtil.RemoveTable(table);
                Test.Assert(!agent.NewAzureStorageTableStoredAccessPolicy(table.Name, Utility.GenNameString("p", 5), null, null, null), "Create stored access policy against non-existing container should fail");
                ExpectedContainErrorMessage("does not exist");
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Test plan, positive functional 8.39.1, 8.39.2
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetPolicyVariations()
        {
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);

            try
            {
                //empty policies
                Test.Assert(agent.GetAzureStorageTableStoredAccessPolicy(table.Name, null),
                    "Get stored access policy in table should succeed");
                Test.Info("Get stored access policy");
                Assert.IsTrue(agent.Output.Count == 0);

                //get all policies
                List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessTablePolicy>();
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                foreach (Utility.RawStoredAccessPolicy samplePolicy in samplePolicies)
                {
                    CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, table, false);
                    SharedAccessTablePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission);
                    comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessTablePolicy>(policy, samplePolicy.PolicyName));
                }

                Test.Assert(agent.GetAzureStorageTableStoredAccessPolicy(table.Name, null),
                    "Get stored access policy in table should succeed");
                Test.Info("Get stored access policy");
                agent.OutputValidation(comp);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Test plan, negative functional 8.39.1, 8.39.2, 8.39.3, 8.39.4
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetPolicyInvalid()
        {
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);

            try
            {
                Test.Assert(!agent.GetAzureStorageTableStoredAccessPolicy(table.Name, "policy"),
                    "Get non-existing stored access policy should fail");
                ExpectedContainErrorMessage("Can not find policy");

                Test.Assert(!agent.GetAzureStorageTableStoredAccessPolicy(table.Name, FileNamingGenerator.GenerateValidateASCIIName(65)),
                    "Get stored access policy with name length larger than 64 should fail");
                ExpectedContainErrorMessage("Can not find policy");

                Test.Assert(!agent.GetAzureStorageTableStoredAccessPolicy("CONTAINER", "policy"),
                    "Get stored access policy from invalid table name should fail");
                ExpectedContainErrorMessage("The table specified does not exist.");

                tableUtil.RemoveTable(table);
                Test.Assert(!agent.GetAzureStorageTableStoredAccessPolicy(table.Name, "policy"),
                    "Get stored access policy from invalid table name should fail");
                ExpectedContainErrorMessage("The table specified does not exist.");
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Test plan negative 8.40.1, 8.40.2,8.40.3,8.40.4
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void RemovePolicyInvalid()
        {
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);

            try
            {
                Test.Assert(!agent.RemoveAzureStorageTableStoredAccessPolicy(table.Name, "policy"),
                    "Remove non-existing stored access policy should fail");
                ExpectedContainErrorMessage("Can not find policy");

                Test.Assert(!agent.RemoveAzureStorageTableStoredAccessPolicy(table.Name, FileNamingGenerator.GenerateValidateASCIIName(65)),
                    "Remove stored access policy with name length larger than 64 should fail");
                ExpectedContainErrorMessage("Can not find policy");

                Test.Assert(!agent.RemoveAzureStorageTableStoredAccessPolicy("CONTAINER", "policy"),
                    "Remove stored access policy from invalid table name should fail");
                ExpectedContainErrorMessage("The table specified does not exist.");

                tableUtil.RemoveTable(table);
                Test.Assert(!agent.RemoveAzureStorageTableStoredAccessPolicy(table.Name, "policy"),
                    "Remove stored access policy from invalid table name should fail");
                ExpectedContainErrorMessage("The table specified does not exist.");
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Test plan positive functional: 8.41.1
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyDifferentNames()
        {
            DateTime? expiryTime1 = DateTime.Today.AddDays(10);
            DateTime? startTime1 = DateTime.Today.AddDays(-2);
            DateTime? expiryTime2 = DateTime.Today.AddDays(11);
            DateTime? startTime2 = DateTime.Today.AddDays(-1);
            string permission = "raud";
            string policyName = Utility.GenNameString("p", 0);
            Utility.RawStoredAccessPolicy policy1 = new Utility.RawStoredAccessPolicy(policyName, startTime1, expiryTime1, permission);
            Utility.RawStoredAccessPolicy policy2 = new Utility.RawStoredAccessPolicy(policyName, startTime2, expiryTime2, permission);
            SetStoredAccessPolicyAndValidate(policy1, policy2);

            policy1.PolicyName = policy2.PolicyName = Utility.GenNameString("p", 4);
            SetStoredAccessPolicyAndValidate(policy1, policy2);

            policy1.PolicyName = policy2.PolicyName = FileNamingGenerator.GenerateValidateASCIIName(64);
            SetStoredAccessPolicyAndValidate(policy1, policy2);

            foreach (var samplePolicyName in FileNamingGenerator.GenerateValidateUnicodeName(40))
            {
                policy1.PolicyName = policy2.PolicyName = samplePolicyName;
                SetStoredAccessPolicyAndValidate(policy1, policy2);
            }
        }



        /// <summary>
        /// Test plan positive functional: 8.41.2, 8.41.3,8.41.4,8.41.5
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyDifferentValues()
        {
            string tableName = Utility.GenNameString("table");
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessTablePolicy>();
            samplePolicies[4].PolicyName = samplePolicies[3].PolicyName = samplePolicies[2].PolicyName = samplePolicies[1].PolicyName = samplePolicies[0].PolicyName;
            samplePolicies[2].ExpiryTime = DateTime.Today.AddDays(3);
            SetStoredAccessPolicyAndValidate(samplePolicies[0], samplePolicies[1], tableName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[1], samplePolicies[2], tableName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[2], samplePolicies[3], tableName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[3], samplePolicies[4], tableName, true, true);
        }

        /// <summary>
        /// Test plan positive functional: 8.41.6
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyNoStartTimeNoExpiryTime()
        {
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessTablePolicy>()[0];

            try
            {
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, table);

                //NoStartTime
                Test.Assert(agent.SetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, null, null, null, true, false),
                    "Set stored access policy with -NoStartTime should succeed");

                SharedAccessTablePolicies expectedPolicies = new SharedAccessTablePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(null, samplePolicy.ExpiryTime, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessTablePolicy>(table.GetPermissions().SharedAccessPolicies, expectedPolicies);
                SharedAccessTablePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(null, samplePolicy.ExpiryTime, samplePolicy.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessTablePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);

                //NoExpiryTime
                Test.Assert(agent.SetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, null, null, null, false, true),
                    "Set stored access policy with -NoStartTime should succeed");
                expectedPolicies = new SharedAccessTablePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(null, null, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessTablePolicy>(table.GetPermissions().SharedAccessPolicies, expectedPolicies);
                policy = Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(null, null, samplePolicy.Permission);
                comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessTablePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);

                //both
                Utility.ClearStoredAccessPolicy<CloudTable>(table);
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, table);

                Test.Assert(agent.SetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, null, null, null, true, true),
                    "Set stored access policy with -NoStartTime should succeed");
                expectedPolicies = new SharedAccessTablePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(null, null, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessTablePolicy>(table.GetPermissions().SharedAccessPolicies, expectedPolicies);
                policy = Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(null, null, samplePolicy.Permission);
                comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessTablePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Test plan 8.41.1 -5 negative cases
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyInvalidParameter()
        {
            DateTime? startTime = DateTime.Today.AddDays(1);
            DateTime? expiryTime = DateTime.Today.AddDays(-1);
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);

            try
            {
                Test.Assert(!agent.SetAzureStorageTableStoredAccessPolicy("CONTAINER", Utility.GenNameString("p", 5), null, null, null), "Set stored acess policy for invalid table name CONTAINER should fail");
                ExpectedContainErrorMessage("The table specified does not exist.");

                Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessTablePolicy>()[0];
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, table);
                Test.Assert(!agent.SetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, null, startTime, expiryTime), "Set stored access policy for ExpiryTime earlier than StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time.");

                Test.Assert(!agent.SetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, null, startTime, startTime), "Set stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time.");

                Test.Assert(!agent.SetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, "x", null, null), "Set stored access policy with invalid permission should fail");
                ExpectedContainErrorMessage("Invalid access permission");

                Test.Assert(!agent.SetAzureStorageTableStoredAccessPolicy(table.Name, FileNamingGenerator.GenerateValidateASCIIName(65), null, null, null), "Create stored access policy with invalid name length should fail");
                ExpectedContainErrorMessage("Can not find policy");

                Test.Assert(!agent.SetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, null, true, false), "Set stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("Parameter -StartTime and -NoStartTime are mutually exclusive");

                Test.Assert(!agent.SetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, samplePolicy.Permission, null, samplePolicy.ExpiryTime, false, true), "Set stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("Parameter -ExpiryTime and -NoExpiryTime are mutually exclusive");

                tableUtil.RemoveTable(table);
                Test.Assert(!agent.SetAzureStorageTableStoredAccessPolicy(table.Name, Utility.GenNameString("p", 5), null, null, null), "Set stored access policy against non-existing table should fail");
                ExpectedContainErrorMessage("does not exist");
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// test plan functional 8.41.7
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void PolicyWithSASExpiryTimeFutureToPast()
        {
            TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
            string policyName = Utility.GenNameString("saspolicy");
            DateTime? expiryTime = DateTime.Today.AddDays(10);
            DateTime? startTime = DateTime.Today.AddDays(-2);
            string permission = "q";
            CloudTable table = tableUtil.CreateTable();

            try
            {           
                //expiry time is future
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, table, false);
                string sasToken = agent.GetTableSasFromCmd(table.Name, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                tableUtil.ValidateTableQueryableWithSasToken(table, sasToken);

                //modify exipiry time to past
                expiryTime = DateTime.Today.AddDays(-2);
                agent.SetAzureStorageTableStoredAccessPolicy(table.Name, policyName, null, null, expiryTime);
                Test.Info("Sleep and wait for sas policy taking effect");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));

                try
                {
                    tableUtil.ValidateTableQueryableWithSasToken(table, sasToken);
                    Test.Error(string.Format("Access table should fail since the expiry time is {0}, but now is {1}",
                        expiryTime.Value.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// test plan functional 8.41.7
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void PolicyWithSASExpiryTimePastToFuture()
        {
            TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
            string policyName = Utility.GenNameString("saspolicy");
            DateTime? expiryTime = DateTime.Today.AddDays(-2);
            DateTime? startTime = DateTime.Today.AddDays(-10);
            string permission = "q";
            CloudTable table = tableUtil.CreateTable();

            try
            {             
                //expiry time is in the past
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, table, false);
                string sasToken = agent.GetTableSasFromCmd(table.Name, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                try
                {
                    tableUtil.ValidateTableQueryableWithSasToken(table, sasToken);
                    Test.Error(string.Format("Access table should fail since the expiry time is {0}, but now is {1}",
                        expiryTime.Value.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }

                //modify expiry time to future
                expiryTime = DateTime.Today.AddDays(10);
                agent.SetAzureStorageTableStoredAccessPolicy(table.Name, policyName, null, null, expiryTime);
                Test.Info("Sleep and wait for sas policy taking effect");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                tableUtil.ValidateTableQueryableWithSasToken(table, sasToken);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        internal void CreateStoredAccessPolicyAndValidate(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, string tableName = null, bool ifCleanUpTable = true, bool ifCleanUpPolicy = true)
        {
            CloudTable table = tableUtil.CreateTable(tableName);

            try
            {
                //create the policy
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, table, ifCleanUpPolicy);

                //get the policy and validate
                SharedAccessTablePolicies expectedPolicies = new SharedAccessTablePolicies();
                expectedPolicies.Add(policyName, Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(startTime, expiryTime, permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessTablePolicy>(table.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                if (ifCleanUpPolicy)
                {
                    Utility.ClearStoredAccessPolicy<CloudTable>(table);
                }
                if (ifCleanUpTable)
                {
                    tableUtil.RemoveTable(table);
                }
            }
        }

        internal void CreateStoredAccessPolicy(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, CloudTable table, bool ifCleanUpPolicy = true)
        {
            if (ifCleanUpPolicy)
            {
                Utility.ClearStoredAccessPolicy<CloudTable>(table);
            }

            Test.Assert(agent.NewAzureStorageTableStoredAccessPolicy(table.Name, policyName, permission, startTime, expiryTime),
                "Create stored access policy in table should succeed");
            Test.Info("Created stored access policy:{0}", policyName);
        }

        internal void SetStoredAccessPolicyAndValidate(Utility.RawStoredAccessPolicy policy1, Utility.RawStoredAccessPolicy policy2, string tableName = null, bool ifCleanupPolicy = true, bool ifCleanupTable = true)
        {
            CloudTable table = tableUtil.CreateTable(tableName);
            if (ifCleanupPolicy)
            {
                Utility.ClearStoredAccessPolicy<CloudTable>(table);
            }

            policy2.PolicyName = policy1.PolicyName;

            try
            {
                agent.NewAzureStorageTableStoredAccessPolicy(table.Name, policy1.PolicyName, policy1.Permission, policy1.StartTime, policy1.ExpiryTime);
                Test.Assert(agent.SetAzureStorageTableStoredAccessPolicy(table.Name, policy2.PolicyName, policy2.Permission, policy2.StartTime, policy2.ExpiryTime),
                "Set stored access policy in table should succeed");
                Test.Info("Set stored access policy:{0}", policy2.PolicyName);

                //get the policy and validate
                SharedAccessTablePolicies expectedPolicies = new SharedAccessTablePolicies();
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

                expectedPolicies.Add(policy2.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(policy2.StartTime, policy2.ExpiryTime, policy2.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessTablePolicy>(table.GetPermissions().SharedAccessPolicies, expectedPolicies);

                //validate the output
                SharedAccessTablePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(policy2.StartTime, policy2.ExpiryTime, policy2.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessTablePolicy>(policy, policy2.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                if (ifCleanupTable)
                {
                    tableUtil.RemoveTable(table);
                }
            }
        }
    }
}
