using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using StorageFile = Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.File;
using MS.Test.Common.MsTestLib;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    [TestClass]
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
                Test.Assert(agent.GetAzureStorageShareStoredAccessPolicy(shareName, null),
                    "Get stored access policy in share should succeed");
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

        /// <summary>
        /// test plan functional 8.51
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(PsTag.File)]
        public void PolicyWithSASStartTimePastToFuture()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            string fileName = Utility.GenNameString("file");
            fileUtil.CreateFile(share, fileName);

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                string policyName = Utility.GenNameString("saspolicy");
                DateTime? expiryTime = DateTime.Today.AddDays(10);
                DateTime? startTime = DateTime.Today.AddDays(-2);
                string permission = "r";

                //start time is in the past
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, share, false);
                string sasToken = agent.GetAzureStorageShareSasFromCmd(shareName, policyName);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                fileUtil.ValidateShareReadableWithSasToken(share, fileName, sasToken);

                //modify start time to future
                startTime = DateTime.Today.AddDays(2);
                agent.SetAzureStorageShareStoredAccessPolicy(shareName, policyName, null, startTime, null);
                Test.Info("Sleep and wait for sas policy taking effect");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));

                try
                {
                    fileUtil.ValidateShareReadableWithSasToken(share, fileName, sasToken);
                    Test.Error(string.Format("Access share should fail since the start time is {0}, but now is {1}",
                        startTime.Value.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// test plan functional 8.51
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(PsTag.File)]
        public void PolicyWithSASStartTimeFutureToPast()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            string fileName = Utility.GenNameString("file");
            fileUtil.CreateFile(share, fileName);

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                string policyName = Utility.GenNameString("saspolicy");
                DateTime? expiryTime = DateTime.Today.AddDays(10);
                DateTime? startTime = DateTime.Today.AddDays(2);
                string permission = "r";

                //start time is in the future
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, share, false);
                string sasToken = agent.GetAzureStorageShareSasFromCmd(shareName, policyName);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                try
                {
                    fileUtil.ValidateShareReadableWithSasToken(share, fileName, sasToken);
                    Test.Error(string.Format("Access share should fail since the start time is {0}, but now is {1}",
                        startTime.Value.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }

                //modify start time to past
                startTime = DateTime.Today.AddDays(-2);
                agent.SetAzureStorageShareStoredAccessPolicy(shareName, policyName, null, startTime, null);

                fileUtil.ValidateShareReadableWithSasToken(share, fileName, sasToken);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// test plan functional 8.51
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(PsTag.File)]
        public void PolicyWithSASRemovePolicy()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);
            string fileName = Utility.GenNameString("file");
            fileUtil.CreateFile(share, fileName);

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                string policyName = Utility.GenNameString("saspolicy");
                DateTime? expiryTime = DateTime.Today.AddDays(10);
                DateTime? startTime = DateTime.Today.AddDays(-2);
                string permission = "r";

                //start time is in the past
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, share, false);
                string sasToken = agent.GetAzureStorageShareSasFromCmd(shareName, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                fileUtil.ValidateShareReadableWithSasToken(share, fileName, sasToken);

                //remove the policy
                startTime = DateTime.Today.AddDays(2);
                agent.RemoveAzureStorageShareStoredAccessPolicy(shareName, policyName);
                Test.Info("Sleep and wait for sas policy taking effect");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));

                try
                {
                    fileUtil.ValidateShareReadableWithSasToken(share, fileName, sasToken);
                    Test.Error(string.Format("Access container should fail since policy {0} is removed", policyName));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a share with only limited access right(read, write,delete,list)
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewShareSasWithPermission()
        {
            //Share read permission
            string sharePermission = "r";
            GenerateSasTokenAndValidate(sharePermission);

            //Share write permission
            sharePermission = "w";
            GenerateSasTokenAndValidate(sharePermission);

            //Share delete permission
            sharePermission = "d";
            GenerateSasTokenAndValidate(sharePermission);

            //Share list permission
            sharePermission = "l";
            GenerateSasTokenAndValidate(sharePermission);

            //Random combination
            sharePermission = Utility.GenRandomCombination(Utility.SharePermission);
            GenerateSasTokenAndValidate(sharePermission);
        }

        /// <summary>
        /// Generate SAS of a share with a limited time period
        /// Wait for the time expiration
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewShareSasWithLifeTime()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            double lifeTime = 3; //Minutes
            const double deltaTime = 0.5;
            DateTime startTime = DateTime.Now.AddMinutes(lifeTime);
            DateTime expiryTime = startTime.AddMinutes(lifeTime);

            try
            {
                string sharePermissions = Utility.GenRandomCombination(Utility.SharePermission);
                string sastoken = agent.GetAzureStorageShareSasFromCmd(shareName, string.Empty, sharePermissions, startTime, expiryTime);
                try
                {
                    ValidateSasToken(share, sharePermissions, sastoken);
                    Test.Error(string.Format("Access share should fail since the start time is {0}, but now is {1}",
                        startTime.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }

                Test.Info("Sleep and wait for the sas token start time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));
                ValidateSasToken(share, sharePermissions, sastoken);
                Test.Info("Sleep and wait for sas token expiry time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));

                try
                {
                    ValidateSasToken(share, sharePermissions, sastoken);
                    Test.Error(string.Format("Access share should fail since the expiry time is {0}, but now is {1}",
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
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a share by policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewShareSasWithPolicy()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                FileSharePermissions permission = new FileSharePermissions();
                string policyName = Util.FileNamingGenerator.GenerateValidateASCIIName(64);

                permission.SharedAccessPolicies.Add(policyName, new SharedAccessFilePolicy
                {
                    SharedAccessExpiryTime = DateTime.Now.Add(sasLifeTime),
                    Permissions = SharedAccessFilePermissions.Read,
                });

                share.SetPermissions(permission);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                string sasToken = agent.GetAzureStorageShareSasFromCmd(shareName, policyName);
                ValidateSasToken(share, "r", sasToken);

                permission.SharedAccessPolicies[policyName] = new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read,
                };
                Test.Info("Sleep and wait for sas policy taking effect");

                Thread.Sleep(30000);

                sasToken = agent.GetAzureStorageShareSasFromCmd(shareName, policyName, null, null, DateTime.Now.Add(sasLifeTime));
                ValidateSasToken(share, "r", sasToken);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a share of a non-existing policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewShareSasWithNonExistPolicy()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string policyName = Utility.GenNameString("nonexistpolicy");

                Test.Assert(!agent.NewAzureStorageShareSAS(shareName, policyName, string.Empty),
                    "Generate share sas token with non-exist policy should fail");
                ExpectedEqualErrorMessage(string.Format("Invalid access policy '{0}'.", policyName));
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a share with expiry time before start time
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewShareSasWithInvalidLifeTime()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                DateTime start = DateTime.UtcNow;
                DateTime end = start.AddHours(1.0);
                Test.Assert(!agent.NewAzureStorageShareSAS(shareName, string.Empty, "l", end, start),
                        "Generate share sas token with invalid should fail");
                ExpectedStartsWithErrorMessage("The expiry time of the specified access policy should be greater than start time");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Return full uri with SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewShareSasWithFullUri()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string sharePermissions = Utility.GenRandomCombination(Utility.SharePermission);
                string fullUri = agent.GetAzureStorageShareSasFromCmd(shareName, string.Empty, sharePermissions);
                string sasToken = (lang == Language.PowerShell ? fullUri.Substring(fullUri.IndexOf("?")) : fullUri);
                ValidateSasToken(share, sharePermissions, sasToken);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a share with only limited access right(read,write,delete,list,none)
        ///     Verify access with the non-granted right to this file is denied
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewShareSasWithLimitedPermission()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                //Share read permission
                string sharePermission = "r";
                string limitedPermission = "wdl";
                string sastoken = agent.GetAzureStorageShareSasFromCmd(shareName, string.Empty, sharePermission);
                ValidateLimitedSasPermission(share, limitedPermission, sastoken);

                //Share write permission
                sharePermission = "w";
                limitedPermission = "rdl";
                sastoken = agent.GetAzureStorageShareSasFromCmd(shareName, string.Empty, sharePermission);
                ValidateLimitedSasPermission(share, limitedPermission, sastoken);

                //Share delete permission
                sharePermission = "d";
                limitedPermission = "rwl";
                sastoken = agent.GetAzureStorageShareSasFromCmd(shareName, string.Empty, sharePermission);
                ValidateLimitedSasPermission(share, limitedPermission, sastoken);

                //Share list permission
                sharePermission = "l";
                limitedPermission = "rwd";
                sastoken = agent.GetAzureStorageShareSasFromCmd(shareName, string.Empty, sharePermission);
                ValidateLimitedSasPermission(share, limitedPermission, sastoken);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a share with only limited access right(read,write,delete,list,none)
        ///     Verify access with the non-granted right to this file is denied
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewShareSasNegativeTest()
        {
            DateTime startTime = DateTime.Now.AddMinutes(-1);
            DateTime expiryTime = startTime.AddMinutes(30);
            string sharePermission = "r";
            string policyName = Utility.GenNameString("p");

            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                Utility.ClearStoredAccessPolicy<CloudFileShare>(share);
                Test.Assert(agent.NewAzureStorageShareStoredAccessPolicy(shareName, policyName, sharePermission, startTime, expiryTime),
                    "Create stored access policy to a share should succeed");
                Utility.WaitForPolicyBecomeValid<CloudFileShare>(share, new Utility.RawStoredAccessPolicy(policyName, startTime, expiryTime, sharePermission));

                Test.Assert(!agent.NewAzureStorageShareSAS(shareName, policyName, startTime:startTime),
                    "Create sas with Policy and start time should failed.");
                ExpectedContainErrorMessage("This start time field must be omitted if it has been specified in an associated stored access policy.");

                Test.Assert(!agent.NewAzureStorageShareSAS(shareName, policyName, sharePermission),
                    "Create sas with Policy and permission should failed.");
                ExpectedContainErrorMessage("Parameter set cannot be resolved using the specified named parameters");

                Test.Assert(!agent.NewAzureStorageShareSAS(shareName, policyName, expiryTime:expiryTime),
                    "Create sas with Policy and expiry time should failed.");
                ExpectedContainErrorMessage("This expiry time field must be omitted if it has been specified in an associated stored access policy.");

                fileUtil.DeleteFileShareIfExists(shareName); 
                Test.Assert(agent.NewAzureStorageShareSAS(shareName, null, sharePermission, startTime, expiryTime),
                        "Create sas on a non-exist share without policy should succeed.");

                Test.Assert(agent.NewAzureStorageShareSAS(shareName, policyName),
                        "Create sas on a non-exist share with policy should fail.");
                ExpectedContainErrorMessage("The specified share does not exist.");

            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }            
        }

        /// <summary>
        /// Generate SAS of a file with only limited access right(read, write,delete)
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewFileSasWithPermission()
        {
            //File read permission
            string permission = "r";
            GenerateFileSasTokenAndValidate(permission);

            //File write permission
            permission = "w";
            GenerateFileSasTokenAndValidate(permission);

            //File delete permission
            permission = "d";
            GenerateFileSasTokenAndValidate(permission);
            
            //Random combination
            permission = Utility.GenRandomCombination(Utility.FilePermission);
            GenerateSasTokenAndValidate(permission);
        }

        /// <summary>
        /// Generate SAS of a file with a limited time period
        /// Wait for the time expiration
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewFileSasWithLifeTime()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            double lifeTime = 3; //Minutes
            const double deltaTime = 0.5;
            DateTime startTime = DateTime.Now.AddMinutes(lifeTime);
            DateTime expiryTime = startTime.AddMinutes(lifeTime);

            try
            {
                string fileName = Utility.GenNameString("file");
                var file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);
                string permissions = Utility.GenRandomCombination(Utility.FilePermission);
                string sastoken = agent.GetAzureStorageFileSasFromCmd(shareName, fileName, string.Empty, permissions, startTime, expiryTime);
                try
                {
                    ValidateFileSasToken(file, permissions, sastoken);
                    Test.Error(string.Format("Access file should fail since the start time is {0}, but now is {1}",
                        startTime.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }

                Test.Info("Sleep and wait for the sas token start time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));
                ValidateFileSasToken(file, permissions, sastoken);
                Test.Info("Sleep and wait for sas token expiry time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));

                try
                {
                    ValidateFileSasToken(file, permissions, sastoken);
                    Test.Error(string.Format("Access file should fail since the expiry time is {0}, but now is {1}",
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
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a file by policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewFileSasWithPolicy()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("file");
                var file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                FileSharePermissions permission = new FileSharePermissions();
                string policyName = Util.FileNamingGenerator.GenerateValidateASCIIName(64);

                permission.SharedAccessPolicies.Add(policyName, new SharedAccessFilePolicy
                {
                    SharedAccessExpiryTime = DateTime.Now.Add(sasLifeTime),
                    Permissions = SharedAccessFilePermissions.Read,
                });

                share.SetPermissions(permission);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                string sasToken = agent.GetAzureStorageFileSasFromCmd(shareName, fileName, policyName);
                ValidateFileSasToken(file, "r", sasToken);

                permission.SharedAccessPolicies[policyName] = new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read,
                };
                Test.Info("Sleep and wait for sas policy taking effect");

                Thread.Sleep(30000);

                sasToken = agent.GetAzureStorageFileSasFromCmd(shareName, fileName, policyName, null, null, DateTime.Now.Add(sasLifeTime));
                ValidateFileSasToken(file, "r", sasToken);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a file of a non-existing policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewFileSasWithNonExistPolicy()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("file");
                var file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                string policyName = Utility.GenNameString("nonexistpolicy");

                Test.Assert(!agent.NewAzureStorageFileSAS(shareName, fileName, policyName, string.Empty),
                    "Generate file sas token with non-exist policy should fail");
                ExpectedEqualErrorMessage(string.Format("Invalid access policy '{0}'.", policyName));
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a file with expiry time before start time
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewFileSasWithInvalidLifeTime()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("file");
                var file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                DateTime start = DateTime.UtcNow;
                DateTime end = start.AddHours(1.0);
                Test.Assert(!agent.NewAzureStorageFileSAS(shareName, fileName, string.Empty, "l", end, start),
                        "Generate file sas token with invalid should fail");
                ExpectedStartsWithErrorMessage("The expiry time of the specified access policy should be greater than start time");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Return full uri with SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewFileSasWithFullUri()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("file");
                var file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                string permissions = Utility.GenRandomCombination(Utility.FilePermission);
                string fullUri = agent.GetAzureStorageFileSasFromCmd(shareName, fileName, string.Empty, permissions);
                string sasToken = (lang == Language.PowerShell ? fullUri.Substring(fullUri.IndexOf("?")) : fullUri);
                ValidateFileSasToken(file, permissions, sasToken);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a file with only limited access right(read,write,delete,list,none)
        ///     Verify access with the non-granted right to this file is denied
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewFileSasWithLimitedPermission()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("file");
                var file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                //File read permission
                string permissions = "r";
                string limitedPermission = "wd";
                string sastoken = agent.GetAzureStorageFileSasFromCmd(shareName, fileName, string.Empty, permissions);
                ValidateFileLimitedSasPermission(file, limitedPermission, sastoken);

                //File write permission
                permissions = "w";
                limitedPermission = "rd";
                sastoken = agent.GetAzureStorageFileSasFromCmd(shareName, fileName, string.Empty, permissions);
                ValidateLimitedSasPermission(share, limitedPermission, sastoken);

                //File delete permission
                permissions = "d";
                limitedPermission = "rw";
                sastoken = agent.GetAzureStorageFileSasFromCmd(shareName, fileName, string.Empty, permissions);
                ValidateLimitedSasPermission(share, limitedPermission, sastoken);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Generate SAS of a file with only limited access right(read,write,delete,none)
        ///     Verify access with the non-granted right to this file is denied
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.File)]
        public void NewFileSasNegativeTest()
        {
            DateTime startTime = DateTime.Now.AddMinutes(-1);
            DateTime expiryTime = startTime.AddMinutes(30);
            string sharePermission = "r";
            string policyName = Utility.GenNameString("p");

            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                Utility.ClearStoredAccessPolicy<CloudFileShare>(share);
                Test.Assert(agent.NewAzureStorageShareStoredAccessPolicy(shareName, policyName, sharePermission, startTime, expiryTime),
                    "Create stored access policy to a share should succeed");
                Utility.WaitForPolicyBecomeValid<CloudFileShare>(share, new Utility.RawStoredAccessPolicy(policyName, startTime, expiryTime, sharePermission));

                string fileName = Utility.GenNameString("file");
                var file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                Test.Assert(!agent.NewAzureStorageFileSAS(shareName, fileName, policyName, startTime: startTime),
                    "Create sas with Policy and start time should failed.");
                ExpectedContainErrorMessage("This start time field must be omitted if it has been specified in an associated stored access policy.");

                Test.Assert(!agent.NewAzureStorageFileSAS(shareName, fileName, policyName, sharePermission),
                    "Create sas with Policy and permission should failed.");
                ExpectedContainErrorMessage("Parameter set cannot be resolved using the specified named parameters");

                Test.Assert(!agent.NewAzureStorageFileSAS(shareName, fileName, policyName, expiryTime: expiryTime),
                    "Create sas with Policy and expiry time should failed.");
                ExpectedContainErrorMessage("This expiry time field must be omitted if it has been specified in an associated stored access policy.");

                fileUtil.DeleteFileIfExists(share, fileName);
                Test.Assert(agent.NewAzureStorageFileSAS(shareName, fileName, null, sharePermission, startTime, expiryTime),
                        "Create sas on a non-exist file without policy should succeed.");

                Test.Assert(agent.NewAzureStorageFileSAS(shareName, fileName, policyName),
                        "Create sas on a non-exist file with policy should fail.");
                ExpectedContainErrorMessage("The specified file does not exist.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        private void GenerateFileSasTokenAndValidate(string permission)
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("file");
                var file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);
                string sastoken = agent.GetAzureStorageFileSasFromCmd(shareName, fileName, string.Empty, permission);
                ValidateFileSasToken(file, permission, sastoken);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }
        

        private void GenerateSasTokenAndValidate(string sharePermission)
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string sastoken = agent.GetAzureStorageShareSasFromCmd(shareName, string.Empty, sharePermission);
                ValidateSasToken(share, sharePermission, sastoken);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }
        
        private void ValidateFileSasToken(StorageFile.CloudFile file, string permissions, string sasToken)
        {
            foreach (char permission in permissions.ToLower())
            {
                switch (permission)
                {
                    case 'r':
                        fileUtil.ValidateFileReadableWithSasToken(file, sasToken);
                        break;
                    case 'w':
                        fileUtil.ValidateFileWriteableWithSasToken(file, sasToken);
                        break;
                    case 'd':
                        fileUtil.ValidateFileDeleteableWithSasToken(file, sasToken);
                        break;
                }
            }
        }

        private void ValidateSasToken(CloudFileShare share, string sharePermissions, string sasToken)
        {
            foreach (char permission in sharePermissions.ToLower())
            {
                switch (permission)
                {
                    case 'r':
                        fileUtil.ValidateShareReadableWithSasToken(share, Utility.GenNameString("file"), sasToken);
                        break;
                    case 'w':
                        fileUtil.ValidateShareWriteableWithSasToken(share, sasToken);
                        break;
                    case 'd':
                        fileUtil.ValidateShareDeleteableWithSasToken(share, sasToken);
                        break;
                    case 'l':
                        fileUtil.ValidateShareListableWithSasToken(share, sasToken);
                        break;
                }
            }
        }

        private void ValidateFileLimitedSasPermission(StorageFile.CloudFile file,
            string limitedPermission, string sasToken)
        {
            try
            {
                ValidateFileSasToken(file, limitedPermission, sasToken);
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

        private void ValidateLimitedSasPermission(CloudFileShare share,
            string limitedPermission, string sasToken)
        {
            try
            {
                ValidateSasToken(share, limitedPermission, sasToken);
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
