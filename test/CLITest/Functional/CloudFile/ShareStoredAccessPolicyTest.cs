using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        internal void CreateStoredAccessPolicyAndValidate(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, string shareName = null, bool ifCleanUpShare = true, bool ifCleanUpPolicy = true)
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

        internal void CreateStoredAccessPolicy(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, CloudFileShare share, bool ifCleanUpPolicy = true)
        {
            if (ifCleanUpPolicy)
            {
                Utility.ClearStoredAccessPolicy<CloudFileShare>(share);
            }

            Test.Assert(agent.NewAzureStorageShareStoredAccessPolicy(share.Name, policyName, permission, startTime, expiryTime),
                "Create stored access policy in share should succeed");
            Test.Info("Created stored access policy:{0}", policyName);
        }
    }
}
