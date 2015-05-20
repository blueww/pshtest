using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.File;
using MS.Test.Common.MsTestLib;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    public class ShareStoredAccessPolicyTest : TestBase
    {
        [ClassInitialize]
        public static void ShareStoredAccessPolicyTestInitialize(TestContext context)
        {
            StorageAccount = Utility.ConstructStorageAccountFromConnectionString();
            TestBase.TestClassInitialize(context);
        }

        [ClassCleanup]
        public static void ShareStoredAccessPolicyTestInitialize()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Positive functional test case 8.48
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        public void NewPolicyDifferentNames()
        {
            DateTime? expiryTime = DateTime.Today.AddDays(10);
            DateTime? startTime = DateTime.Today.AddDays(-2);
            string permission = "rwdl";
            string shareName = Utility.GenNameString("share");

            try
            {
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 0), permission, startTime, expiryTime, shareName, false);
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 0), permission, startTime, expiryTime, shareName, false);
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 4), permission, startTime, expiryTime, shareName, false);
                CreateStoredAccessPolicyAndValidate(FileNamingGenerator.GenerateValidASCIIOptionValue(64), permission, startTime, expiryTime, shareName, false);
                foreach (var policyName in FileNamingGenerator.GenerateValidateUnicodeName(40))
                {
                    CreateStoredAccessPolicyAndValidate(policyName, permission, startTime, expiryTime, shareName, false);
                }

                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 4), null, null, null, "$root", false);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Test plan positive functional 8.48
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        public void NewPolicyDifferentValues()
        {
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessFilePolicy>();

            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            Utility.ClearStoredAccessPolicy<CloudFileShare>(share);

            try
            {
                foreach (Utility.RawStoredAccessPolicy rawPolicy in samplePolicies)
                {
                    CreateStoredAccessPolicy(rawPolicy.PolicyName, rawPolicy.Permission, rawPolicy.StartTime, rawPolicy.ExpiryTime, share, false);
                }

                Utility.WaitForPolicyBecomeValid<CloudFileShare>(share, expectedCount: samplePolicies.Count);

                SharedAccessFilePolicies expectedPolicies = new SharedAccessFilePolicies();
                foreach (Utility.RawStoredAccessPolicy rawPolicy in samplePolicies)
                {
                    expectedPolicies.Add(rawPolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(rawPolicy.StartTime, rawPolicy.ExpiryTime, rawPolicy.Permission));
                }

                Utility.ValidateStoredAccessPolicies<SharedAccessFilePolicy>(share.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Test plan negative functional 8.48
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        public void NewPolicyInvalidParameter()
        {
            DateTime? startTime = DateTime.Today.AddDays(1);
            DateTime? expiryTime = DateTime.Today.AddDays(-1);
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            Utility.ClearStoredAccessPolicy<CloudFileShare>(share);

            try
            {
                Test.Assert(!agent.NewAzureStorageShareStoredAccessPolicy("SHARE", Utility.GenNameString("p", 5), null, null, null), "Create stored access policy for invalid container name CONTAINER should fail");
                ExpectedContainErrorMessage("The given share name/prefix 'SHARE' is not a valid name for a file share of Microsoft Azure File Service.");

                Test.Assert(!agent.NewAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p", 5), null, startTime, expiryTime), "Create stored access policy for ExpiryTime earlier than StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!agent.NewAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p", 5), null, startTime, startTime), "Create stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!agent.NewAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p", 5), "x", null, null), "Create stored access policy with invalid permission should fail");
                ExpectedContainErrorMessage("Invalid access permission");

                string longShareName = FileNamingGenerator.GenerateValidASCIIOptionValue(65);
                Test.Assert(!agent.NewAzureStorageShareStoredAccessPolicy(shareName, longShareName, null, null, null), "Create stored access policy with invalid permission should fail");
                ExpectedContainErrorMessage(string.Format(
                    "The given share name/prefix '{0}' is not a valid name for a file share of Microsoft Azure File Service.",
                    longShareName));

                for (int i = 1; i <= 5; i++)
                {
                    agent.NewAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p", i), null, null, null);
                }

                Test.Assert(!agent.NewAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p", 6), null, null, null), "Create more than 5 stored access policies should fail");
                ExpectedContainErrorMessage("Too many '6' shared access policy identifiers provided");

                Utility.ClearStoredAccessPolicy<CloudFileShare>(share);
                string policyName = Utility.GenNameString("p", 5);
                agent.NewAzureStorageShareStoredAccessPolicy(shareName, policyName, null, null, null);
                Test.Assert(!agent.NewAzureStorageShareStoredAccessPolicy(shareName, policyName, null, null, null), "Create policy with the same name should fail.");
                ExpectedContainErrorMessage(string.Format(
                    "Policy '{0}' already exists.", policyName));

                fileUtil.DeleteFileShareIfExists(shareName);
                Test.Assert(!agent.NewAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p", 5), null, null, null), "Create stored access policy against non-existing share should fail");
                ExpectedContainErrorMessage("The specified share does not exist");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Test plan, positive functional 8.49
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        public void GetPolicyVariations()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            Utility.ClearStoredAccessPolicy<CloudFileShare>(share);

            try
            {
                //empty policies
                Test.Assert(agent.GetAzureStorageContainerStoredAccessPolicy(shareName, null),
                    "Get stored access policy in container should succeed");
                Test.Info("Get stored access policy");
                Assert.IsTrue(agent.Output.Count == 0);

                //get all policies
                List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessFilePolicy>();
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                foreach (Utility.RawStoredAccessPolicy samplePolicy in samplePolicies)
                {
                    CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, share, false);
                    SharedAccessFilePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission);
                    comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessFilePolicy>(policy, samplePolicy.PolicyName));
                }

                Test.Assert(agent.GetAzureStorageShareStoredAccessPolicy(shareName, null),
                    "Get stored access policy in share should succeed");
                Test.Info("Get stored access policy");
                agent.OutputValidation(comp);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Test plan, negative functional 8.49
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        public void GetPolicyInvalid()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            Utility.ClearStoredAccessPolicy<CloudFileShare>(share);

            try
            {
                string policyName = "policy";
                Test.Assert(!agent.GetAzureStorageShareStoredAccessPolicy(shareName, policyName),
                    "Get non-existing stored access policy should fail");
                ExpectedContainErrorMessage("Can not find policy");

                string invalidName = FileNamingGenerator.GenerateValidASCIIOptionValue(65);
                Test.Assert(!agent.GetAzureStorageShareStoredAccessPolicy(shareName, invalidName),
                    "Get stored access policy with name length larger than 64 should fail");
                ExpectedContainErrorMessage("Can not find policy");

                Test.Assert(!agent.GetAzureStorageShareStoredAccessPolicy("SHARE", policyName),
                    "Get stored access policy from invalid share name should fail");
                ExpectedContainErrorMessage("The specifed resource name contains invalid characters");
                
                fileUtil.DeleteFileShareIfExists(shareName);
                Test.Assert(!agent.GetAzureStorageContainerStoredAccessPolicy(shareName, policyName),
                    "Get stored access policy from invalid share name should fail");
                ExpectedContainErrorMessage("The specified share does not exist.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Test plan negative 8.50
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(PsTag.File)]
        public void RemovePolicyInvalid()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            Utility.ClearStoredAccessPolicy<CloudFileShare>(share);

            try
            {
                string policyName = "policy";
                Test.Assert(!agent.RemoveAzureStorageShareStoredAccessPolicy(shareName, policyName),
                    "Remove non-existing stored access policy should fail");
                ExpectedContainErrorMessage("Can not find policy");

                string invalidName = FileNamingGenerator.GenerateValidASCIIOptionValue(65);
                Test.Assert(!agent.RemoveAzureStorageShareStoredAccessPolicy(shareName, invalidName),
                    "Remove stored access policy with name length larger than 64 should fail");
                ExpectedContainErrorMessage("Can not find policy");

                Test.Assert(!agent.RemoveAzureStorageShareStoredAccessPolicy("SHARE", policyName),
                    "Remove stored access policy from invalid share name should fail");
                ExpectedContainErrorMessage("The specifed resource name contains invalid characters");

                fileUtil.DeleteFileShareIfExists(shareName);
                Test.Assert(!agent.RemoveAzureStorageShareStoredAccessPolicy(shareName, policyName),
                    "Remove stored access policy from invalid container name should fail");
                ExpectedContainErrorMessage("The specified share does not exist");

            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Test plan positive functional: 8.51
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(PsTag.File)]
        public void SetPolicyDifferentNames()
        {
            DateTime? expiryTime1 = DateTime.Today.AddDays(10);
            DateTime? startTime1 = DateTime.Today.AddDays(-2);
            DateTime? expiryTime2 = DateTime.Today.AddDays(11);
            DateTime? startTime2 = DateTime.Today.AddDays(-1);
            string permission = "rwdl";
            string policyName = Utility.GenNameString("p", 0);
            Utility.RawStoredAccessPolicy policy1 = new Utility.RawStoredAccessPolicy(policyName, startTime1, expiryTime1, permission);
            Utility.RawStoredAccessPolicy policy2 = new Utility.RawStoredAccessPolicy(policyName, startTime2, expiryTime2, permission);
            SetStoredAccessPolicyAndValidate(policy1, policy2);

            policy1.PolicyName = policy2.PolicyName = Utility.GenNameString("p", 4);
            SetStoredAccessPolicyAndValidate(policy1, policy2);

            policy1.PolicyName = policy2.PolicyName = FileNamingGenerator.GenerateValidASCIIOptionValue(64);
            SetStoredAccessPolicyAndValidate(policy1, policy2);
        }

        /// <summary>
        /// Test plan positive functional: 8.51
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(PsTag.File)]
        public void SetPolicyDifferentValues()
        {
            string shareName = Utility.GenNameString("share");
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessFilePolicy>();
            samplePolicies[4].PolicyName = samplePolicies[3].PolicyName = samplePolicies[2].PolicyName = samplePolicies[1].PolicyName = samplePolicies[0].PolicyName;
            samplePolicies[2].ExpiryTime = DateTime.Today.AddDays(3);
            SetStoredAccessPolicyAndValidate(samplePolicies[0], samplePolicies[1], shareName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[1], samplePolicies[2], shareName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[2], samplePolicies[3], shareName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[3], samplePolicies[4], shareName, true, true);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(PsTag.File)]
        public void SetPolicyNoStartTimeNoExpiryTime()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            Utility.ClearStoredAccessPolicy<CloudFileShare>(share);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessFilePolicy>()[0];
            double effectiveTime = 30;

            try
            {
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, share);

                //NoStartTime
                Test.Assert(agent.SetAzureStorageShareStoredAccessPolicy(shareName, samplePolicy.PolicyName, null, null, null, true, false),
                    "Set stored access policy with -NoStartTime should succeed");
                Thread.Sleep(TimeSpan.FromSeconds(effectiveTime));
                SharedAccessFilePolicies expectedPolicies = new SharedAccessFilePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(null, samplePolicy.ExpiryTime, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessFilePolicy>(share.GetPermissions().SharedAccessPolicies, expectedPolicies);
                SharedAccessFilePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(null, samplePolicy.ExpiryTime, samplePolicy.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessFilePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);

                //NoExpiryTime
                Test.Assert(agent.SetAzureStorageShareStoredAccessPolicy(shareName, samplePolicy.PolicyName, null, null, null, false, true),
                    "Set stored access policy with -NoExpiryTime should succeed");
                Thread.Sleep(TimeSpan.FromSeconds(effectiveTime));
                expectedPolicies = new SharedAccessFilePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(null, null, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessFilePolicy>(share.GetPermissions().SharedAccessPolicies, expectedPolicies);
                policy = Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(null, null, samplePolicy.Permission);
                comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessFilePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);

                //both
                Utility.ClearStoredAccessPolicy<CloudFileShare>(share);
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, share);

                Test.Assert(agent.SetAzureStorageContainerStoredAccessPolicy(shareName, samplePolicy.PolicyName, null, null, null, true, true),
                    "Set stored access policy with both -NoStartTime and -NoExpiryTime should succeed");
                Thread.Sleep(TimeSpan.FromSeconds(effectiveTime));
                expectedPolicies = new SharedAccessFilePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(null, null, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessFilePolicy>(share.GetPermissions().SharedAccessPolicies, expectedPolicies);
                policy = Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(null, null, samplePolicy.Permission);
                comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessFilePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Test plan 8.51
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(PsTag.File)]
        public void SetPolicyInvalidParameter()
        {
            DateTime? startTime = DateTime.Today.AddDays(1);
            DateTime? expiryTime = DateTime.Today.AddDays(-1);
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            Utility.ClearStoredAccessPolicy<CloudFileShare>(share);

            try
            {
                Test.Assert(!agent.SetAzureStorageShareStoredAccessPolicy("SHARE", Utility.GenNameString("p", 5), null, null, null), "Set stored acess policy for invalid share name SHARE should fail");
                ExpectedContainErrorMessage("The specifed resource name contains invalid characters.");

                Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessFilePolicy>()[0];
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, share);
                Test.Assert(!agent.SetAzureStorageShareStoredAccessPolicy(shareName, samplePolicy.PolicyName, null, startTime, expiryTime), "Set stored access policy for ExpiryTime earlier than StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!agent.SetAzureStorageShareStoredAccessPolicy(shareName, samplePolicy.PolicyName, null, startTime, startTime), "Set stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!agent.SetAzureStorageShareStoredAccessPolicy(shareName, samplePolicy.PolicyName, "x", null, null), "Set stored access policy with invalid permission should fail");
                ExpectedContainErrorMessage("Invalid access permission");

                string invalidName = FileNamingGenerator.GenerateValidASCIIOptionValue(65);
                Test.Assert(!agent.SetAzureStorageShareStoredAccessPolicy(shareName, invalidName, null, null, null), "Create stored access policy with invalid name length should fail");
                ExpectedContainErrorMessage("Can not find policy");

                Test.Assert(!agent.SetAzureStorageShareStoredAccessPolicy(shareName, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, null, true, false), "Setting both -StartTime and -NoStartTime should fail");
                ExpectedContainErrorMessage("Parameter -StartTime and -NoStartTime are mutually exclusive");

                Test.Assert(!agent.SetAzureStorageShareStoredAccessPolicy(shareName, samplePolicy.PolicyName, samplePolicy.Permission, null, samplePolicy.ExpiryTime, false, true), "Setting both -ExpiryTime and -NoExpiryTime should fail");
                ExpectedContainErrorMessage("Parameter -ExpiryTime and -NoExpiryTime are mutually exclusive");

                fileUtil.DeleteFileShareIfExists(shareName);
                Test.Assert(!agent.SetAzureStorageShareStoredAccessPolicy(shareName, Utility.GenNameString("p", 5), null, null, null), "Set stored access policy against non-existing share should fail");
                ExpectedContainErrorMessage("does not exist");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        private void CreateStoredAccessPolicyAndValidate(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, string shareName = null, bool ifCleanUpShare = true, bool ifCleanUpPolicy = true)
        {
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                //create the policy
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, share, ifCleanUpPolicy);

                //get the policy and validate
                SharedAccessFilePolicies expectedPolicies = new SharedAccessFilePolicies();
                expectedPolicies.Add(policyName, Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(startTime, expiryTime, permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessFilePolicy>(share.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                if (ifCleanUpPolicy)
                {
                    Utility.ClearStoredAccessPolicy<CloudFileShare>(share);
                }
                if (ifCleanUpShare)
                {
                    fileUtil.DeleteFileShareIfExists(shareName);
                }
            }
        }

        private void CreateStoredAccessPolicy(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, CloudFileShare share, bool ifCleanUpPolicy = true)
        {
            if (ifCleanUpPolicy)
            {
                Utility.ClearStoredAccessPolicy<CloudFileShare>(share);
            }

            Test.Assert(agent.NewAzureStorageShareStoredAccessPolicy(share.Name, policyName, permission, startTime, expiryTime),
                "Create stored access policy in share should succeed");
            Test.Info("Created stored access policy:{0}", policyName);
        }

        private void SetStoredAccessPolicyAndValidate(Utility.RawStoredAccessPolicy policy1, Utility.RawStoredAccessPolicy policy2, string shareName = null, bool ifCleanupPolicy = true, bool ifCleanupShare = true)
        {
            if (null == shareName)
            {
                shareName = Utility.GenNameString("share");
            }

            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            if (ifCleanupPolicy)
            {
                Utility.ClearStoredAccessPolicy<CloudFileShare>(share);
            }

            policy2.PolicyName = policy1.PolicyName;

            try
            {
                agent.NewAzureStorageShareStoredAccessPolicy(shareName, policy1.PolicyName, policy1.Permission, policy1.StartTime, policy1.ExpiryTime);
                Test.Assert(agent.SetAzureStorageShareStoredAccessPolicy(shareName, policy2.PolicyName, policy2.Permission, policy2.StartTime, policy2.ExpiryTime),
                "Set stored access policy in container should succeed");
                Test.Info("Set stored access policy:{0}", policy2.PolicyName);

                //get the policy and validate
                SharedAccessFilePolicies expectedPolicies = new SharedAccessFilePolicies();
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

                expectedPolicies.Add(policy2.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(policy2.StartTime, policy2.ExpiryTime, policy2.Permission));
                Utility.WaitForPolicyBecomeValid<CloudFileShare>(share, policy2);
                Utility.ValidateStoredAccessPolicies<SharedAccessFilePolicy>(share.GetPermissions().SharedAccessPolicies, expectedPolicies);

                //validate the output
                SharedAccessFilePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(policy2.StartTime, policy2.ExpiryTime, policy2.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessFilePolicy>(policy, policy2.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                if (ifCleanupShare)
                {
                    fileUtil.DeleteFileShareIfExists(shareName);
                }
            }
        }
    }
}
