// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Management.Automation;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    /// <summary>
    /// functional test for Get-AzureStorageBlob
    /// </summary>
    [TestClass]
    public class ContainerStoredAccessPolicy : TestBase
    {
        [ClassInitialize()]
        public static void ContainerStoredAccessPolicyClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void ContainerStoredAccessPolicyClassCleanup()
        {
            TestBase.TestClassCleanup();
        }


        /// <summary>
        /// Create stored access policy with differient policy name: length 1, 5, 64, ASCII, Unicode, test plan positive functional 8.34.1
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
            string permission = "rwdlca";
            string containerName = Utility.GenNameString("container");

            try
            {
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 0), permission, startTime, expiryTime, containerName, false);
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 0), permission, startTime, expiryTime, containerName, false);
                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 4), permission, startTime, expiryTime, containerName, false);
                CreateStoredAccessPolicyAndValidate(FileNamingGenerator.GenerateValidASCIIOptionValue(64), permission, startTime, expiryTime, containerName, false);
                foreach (var policyName in FileNamingGenerator.GenerateValidateUnicodeName(40))
                {
                    CreateStoredAccessPolicyAndValidate(policyName, permission, startTime, expiryTime, containerName, false);
                }

                CreateStoredAccessPolicyAndValidate(Utility.GenNameString("p", 4), null, null, null, "$root", false);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Test plan positive functional 8.34.2, 8.34.3,8.34.2,8.34.4,8.34.5,
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewPolicyDifferentValues()
        {
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>();

            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);

            try
            {
                foreach (Utility.RawStoredAccessPolicy rawPolicy in samplePolicies)
                {
                    CreateStoredAccessPolicy(rawPolicy.PolicyName, rawPolicy.Permission, rawPolicy.StartTime, rawPolicy.ExpiryTime, container, false);
                }

                Utility.WaitForPolicyBecomeValid<CloudBlobContainer>(container, expectedCount: samplePolicies.Count);

                SharedAccessBlobPolicies expectedPolicies = new SharedAccessBlobPolicies();
                foreach (Utility.RawStoredAccessPolicy rawPolicy in samplePolicies)
                {
                    expectedPolicies.Add(rawPolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(rawPolicy.StartTime, rawPolicy.ExpiryTime, rawPolicy.Permission));
                }

                Utility.ValidateStoredAccessPolicies<SharedAccessBlobPolicy>(container.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// Test plan negative functional 8.34.1, 8.34.2,8.34.3,8.34.4,8.34.5,8.34.6,8.34.7,8.34.8,
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
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);

            try
            {
                Test.Assert(!CommandAgent.NewAzureStorageContainerStoredAccessPolicy("$logs", Utility.GenNameString("p", 5), null, null, null), "Create stored access policy $logs container should fail");
                ExpectedContainErrorMessage("The account being accessed does not have sufficient permissions to execute this operation.");

                Test.Assert(!CommandAgent.NewAzureStorageContainerStoredAccessPolicy("CONTAINER", Utility.GenNameString("p", 5), null, null, null), "Create stored access policy for invalid container name CONTAINER should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters.");
                }
                else
                {
                    ExpectedContainErrorMessage("Container name format is incorrect");
                }

                Test.Assert(!CommandAgent.NewAzureStorageContainerStoredAccessPolicy(container.Name, Utility.GenNameString("p", 5), null, startTime, expiryTime), "Create stored access policy for ExpiryTime earlier than StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!CommandAgent.NewAzureStorageContainerStoredAccessPolicy(container.Name, Utility.GenNameString("p", 5), null, startTime, startTime), "Create stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!CommandAgent.NewAzureStorageContainerStoredAccessPolicy(container.Name, Utility.GenNameString("p", 5), "x", null, null), "Create stored access policy with invalid permission should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Invalid access permission");
                }
                else
                {
                    ExpectedContainErrorMessage("Invalid value: x. Options are: r,w,d,l");
                }

                Test.Assert(!CommandAgent.NewAzureStorageContainerStoredAccessPolicy(container.Name, FileNamingGenerator.GenerateValidASCIIOptionValue(65), null, null, null), "Create stored access policy with invalid permission should fail");
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
                    CommandAgent.NewAzureStorageContainerStoredAccessPolicy(container.Name, Utility.GenNameString("p", i), null, null, null);
                }

                Test.Assert(!CommandAgent.NewAzureStorageContainerStoredAccessPolicy(container.Name, Utility.GenNameString("p", 6), null, null, null), "Create moret than 5 stored access policies should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Too many '6' shared access policy identifiers provided");
                }
                else
                {
                    ExpectedContainErrorMessage("A maximum of 5 access policies may be set");
                }

                blobUtil.RemoveContainer(container);
                Test.Assert(!CommandAgent.NewAzureStorageContainerStoredAccessPolicy(container.Name, Utility.GenNameString("p", 5), null, null, null), "Create stored access policy against non-existing container should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("does not exist");
                }
                else
                {
                    ExpectedContainErrorMessage("The specified container does not exist");
                }
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// Test plan, positive functional 8.35.1, 8.35.3
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetPolicyVariations()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);

            try
            {
                //empty policies
                Test.Assert(CommandAgent.GetAzureStorageContainerStoredAccessPolicy(container.Name, null),
                    "Get stored access policy in container should succeed");
                Test.Info("Get stored access policy");
                Assert.IsTrue(CommandAgent.Output.Count == 0);

                //get all policies
                List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>();
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                foreach (Utility.RawStoredAccessPolicy samplePolicy in samplePolicies)
                {
                    CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, container, false);
                    SharedAccessBlobPolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission);
                    comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessBlobPolicy>(policy, samplePolicy.PolicyName));
                }

                Test.Assert(CommandAgent.GetAzureStorageContainerStoredAccessPolicy(container.Name, null),
                    "Get stored access policy in container should succeed");
                Test.Info("Get stored access policy");
                CommandAgent.OutputValidation(comp);
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// Test plan, positive functional 8.35.2
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetPolicyFromRoot()
        {
            CloudBlobContainer container = blobUtil.CreateContainer("$root");
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>()[0];

            try
            {
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, container);

                Test.Assert(CommandAgent.GetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName),
                    "Get stored access policy in container should succeed");
                Test.Info("Get stored access policy:{0}", samplePolicy.PolicyName);

                SharedAccessBlobPolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessBlobPolicy>(policy, samplePolicy.PolicyName));
                CommandAgent.OutputValidation(comp);
            }
            finally
            {
                Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            }
        }

        /// <summary>
        /// Test plan, negative functional 8.35.1, 8.35.2, 8.35.3, 8.35.4
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetPolicyInvalid()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);

            try
            {
                string policyName = "policy";
                Test.Assert(!CommandAgent.GetAzureStorageContainerStoredAccessPolicy(container.Name, policyName),
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
                Test.Assert(!CommandAgent.GetAzureStorageContainerStoredAccessPolicy(container.Name, invalidName),
                    "Get stored access policy with name length larger than 64 should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("The policy {0} doesn't exist", invalidName));
                }

                Test.Assert(!CommandAgent.GetAzureStorageContainerStoredAccessPolicy("CONTAINER", policyName),
                    "Get stored access policy from invalid container name should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters");
                }
                else
                {
                    ExpectedContainErrorMessage("Container name format is incorrect");
                }

                Test.Assert(!CommandAgent.GetAzureStorageContainerStoredAccessPolicy("$logs", policyName),
                    "Get stored access policy from invalid container name should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("The policy {0} doesn't exist", policyName));
                }

                blobUtil.RemoveContainer(container);
                Test.Assert(!CommandAgent.GetAzureStorageContainerStoredAccessPolicy(container.Name, policyName),
                    "Get stored access policy from invalid container name should fail");
                ExpectedContainErrorMessage("The specified container does not exist.");
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// Test plan positive functional 8.36.1
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void RemovePolicyFromRoot()
        {
            CloudBlobContainer container = blobUtil.CreateContainer("$root");
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>()[0];

            try
            {
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, container);

                Test.Assert(CommandAgent.RemoveAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName),
                    "Remove stored access policy in container should succeed");
                Test.Info("Remove stored access policy:{0}", samplePolicy.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudBlobContainer>(container, expectedCount: 0);

                int count = container.GetPermissions().SharedAccessPolicies.Count;
                Test.Assert(count == 0, string.Format("Policy should be removed. Current policy count is {0}", count));
            }
            finally
            {
                Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            }
        }

        /// <summary>
        /// Test plan negative 8.36.1, 8.36.2,8.36.3,8.36.4
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void RemovePolicyInvalid()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);

            try
            {
                string policyName = "policy";
                Test.Assert(!CommandAgent.RemoveAzureStorageContainerStoredAccessPolicy(container.Name, policyName),
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
                Test.Assert(!CommandAgent.RemoveAzureStorageContainerStoredAccessPolicy(container.Name, invalidName),
                    "Remove stored access policy with name length larger than 64 should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("The policy {0} doesn't exist", invalidName));
                }

                Test.Assert(!CommandAgent.RemoveAzureStorageContainerStoredAccessPolicy("CONTAINER", policyName),
                    "Remove stored access policy from invalid container name should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters");
                }
                else
                {
                    ExpectedContainErrorMessage("Container name format is incorrect");
                }

                Test.Assert(!CommandAgent.RemoveAzureStorageContainerStoredAccessPolicy("$logs", policyName),
                    "Remove stored access policy from invalid container name should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("The policy {0} doesn't exist", policyName));
                }

                blobUtil.RemoveContainer(container);
                Test.Assert(!CommandAgent.RemoveAzureStorageContainerStoredAccessPolicy(container.Name, policyName),
                    "Remove stored access policy from invalid container name should fail");
                ExpectedContainErrorMessage("The specified container does not exist");

            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// Test plan positive functional: 8.37.1
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
            string permission = "rwdlac";
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

            SetStoredAccessPolicyAndValidate(policy1, policy2, "$root", true, false);
        }



        /// <summary>
        /// Test plan positive functional: 8.37.2, 8.37.3,8.37.4,8.37.5
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyDifferentValues()
        {
            string containerName = Utility.GenNameString("container");
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>();
            samplePolicies[4].PolicyName = samplePolicies[3].PolicyName = samplePolicies[2].PolicyName = samplePolicies[1].PolicyName = samplePolicies[0].PolicyName;
            samplePolicies[2].ExpiryTime = DateTime.Today.AddDays(3);
            SetStoredAccessPolicyAndValidate(samplePolicies[0], samplePolicies[1], containerName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[1], samplePolicies[2], containerName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[2], samplePolicies[3], containerName, true, false);
            SetStoredAccessPolicyAndValidate(samplePolicies[3], samplePolicies[4], containerName, true, true);
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetPolicyNoStartTimeNoExpiryTime()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>()[0];
            double effectiveTime = 30;

            try
            {
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, container);

                //NoStartTime
                Test.Assert(CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, null, null, null, true, false),
                    "Set stored access policy with -NoStartTime should succeed");
                Thread.Sleep(TimeSpan.FromSeconds(effectiveTime));
                SharedAccessBlobPolicies expectedPolicies = new SharedAccessBlobPolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(null, samplePolicy.ExpiryTime, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessBlobPolicy>(container.GetPermissions().SharedAccessPolicies, expectedPolicies);
                SharedAccessBlobPolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(null, samplePolicy.ExpiryTime, samplePolicy.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessBlobPolicy>(policy, samplePolicy.PolicyName));
                CommandAgent.OutputValidation(comp);

                //NoExpiryTime
                Test.Assert(CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, null, null, null, false, true),
                    "Set stored access policy with -NoExpiryTime should succeed");
                Thread.Sleep(TimeSpan.FromSeconds(effectiveTime)); 
                expectedPolicies = new SharedAccessBlobPolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(null, null, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessBlobPolicy>(container.GetPermissions().SharedAccessPolicies, expectedPolicies);
                policy = Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(null, null, samplePolicy.Permission);
                comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessBlobPolicy>(policy, samplePolicy.PolicyName));
                CommandAgent.OutputValidation(comp);

                //both
                Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, container);

                Test.Assert(CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, null, null, null, true, true),
                    "Set stored access policy with both -NoStartTime and -NoExpiryTime should succeed");
                Thread.Sleep(TimeSpan.FromSeconds(effectiveTime));
                expectedPolicies = new SharedAccessBlobPolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(null, null, samplePolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessBlobPolicy>(container.GetPermissions().SharedAccessPolicies, expectedPolicies);
                policy = Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(null, null, samplePolicy.Permission);
                comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessBlobPolicy>(policy, samplePolicy.PolicyName));
                CommandAgent.OutputValidation(comp);
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// Test plan 8.37.1 -7 negative cases
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
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);

            try
            {
                Test.Assert(!CommandAgent.SetAzureStorageContainerStoredAccessPolicy("$logs", Utility.GenNameString("p", 5), null, null, null), "Set stored acess policy $logs container should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Can not find policy");
                }
                else
                {
                    ExpectedContainErrorMessage("Reason:");
                }

                Test.Assert(!CommandAgent.SetAzureStorageContainerStoredAccessPolicy("CONTAINER", Utility.GenNameString("p", 5), null, null, null), "Set stored acess policy for invalid container name CONTAINER should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The specifed resource name contains invalid characters.");
                }
                else
                {
                    ExpectedContainErrorMessage("Reason:");
                }

                Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>()[0];
                CreateStoredAccessPolicy(samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime, container);
                Test.Assert(!CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, null, startTime, expiryTime), "Set stored access policy for ExpiryTime earlier than StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, null, startTime, startTime), "Set stored access policy for ExpiryTime same as StartTime should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");

                Test.Assert(!CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, "x", null, null), "Set stored access policy with invalid permission should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Invalid access permission");  
                }
                else
                {
                    ExpectedContainErrorMessage("Invalid value: x. Options are: r,w,d,l");
                }

                string invalidName = FileNamingGenerator.GenerateValidASCIIOptionValue(65);
                Test.Assert(!CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, invalidName, null, null, null), "Create stored access policy with invalid name length should fail");
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
                    Test.Assert(!CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, null, true, false), "Setting both -StartTime and -NoStartTime should fail");
                    ExpectedContainErrorMessage("Parameter -StartTime and -NoStartTime are mutually exclusive");
                }
                else
                {
                    Test.Assert(CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, null, true, false), "Setting both -StartTime and -NoStartTime should succeed");
                }

                if (lang == Language.PowerShell)
                {
                    Test.Assert(!CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, samplePolicy.Permission, null, samplePolicy.ExpiryTime, false, true), "Setting both -ExpiryTime and -NoExpiryTime should fail");
                    ExpectedContainErrorMessage("Parameter -ExpiryTime and -NoExpiryTime are mutually exclusive");
                }
                else
                {
                    Test.Assert(CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, samplePolicy.Permission, null, samplePolicy.ExpiryTime, false, true), "Setting both -ExpiryTime and -NoExpiryTime should succeed");
                }

                blobUtil.RemoveContainer(container);
                Test.Assert(!CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, Utility.GenNameString("p", 5), null, null, null), "Set stored access policy against non-existing container should fail");
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("does not exist");
                }
                else
                {
                    ExpectedContainErrorMessage("The specified container does not exist");
                }
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// test plan functional 8.37.7
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void PolicyWithSASStartTimePastToFuture()
        {
            blobUtil.SetupTestContainerAndBlob();

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                string policyName = Utility.GenNameString("saspolicy");
                DateTime? expiryTime = DateTime.Today.AddDays(10);
                DateTime? startTime = DateTime.Today.AddDays(-2);
                string permission = "r";

                //start time is in the past
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, blobUtil.Container, false);
                string sasToken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                blobUtil.ValidateContainerReadableWithSasToken(blobUtil.Container, sasToken);

                //modify start time to future
                startTime = DateTime.Today.AddDays(2);
                CommandAgent.SetAzureStorageContainerStoredAccessPolicy(blobUtil.Container.Name, policyName, null, startTime, null);
                Test.Info("Sleep and wait for sas policy taking effect");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));

                try
                {
                    blobUtil.ValidateContainerReadableWithSasToken(blobUtil.Container, sasToken);
                    Test.Error(string.Format("Access container should fail since the start time is {0}, but now is {1}",
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
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// test plan functional 8.37.7
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void PolicyWithSASStartTimeFutureToPast()
        {
            blobUtil.SetupTestContainerAndBlob();

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                string policyName = Utility.GenNameString("saspolicy");
                DateTime? expiryTime = DateTime.Today.AddDays(10);
                DateTime? startTime = DateTime.Today.AddDays(2);
                string permission = "r";

                //start time is in the future
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, blobUtil.Container, false);
                string sasToken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                try
                {
                    blobUtil.ValidateContainerReadableWithSasToken(blobUtil.Container, sasToken);
                    Test.Error(string.Format("Access container should fail since the start time is {0}, but now is {1}",
                        startTime.Value.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }

                //modify start time to past
                startTime = DateTime.Today.AddDays(-2);
                CommandAgent.SetAzureStorageContainerStoredAccessPolicy(blobUtil.Container.Name, policyName, null, startTime, null);

                Test.Info("Sleep and wait for sas policy taking effect");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));

                blobUtil.ValidateContainerReadableWithSasToken(blobUtil.Container, sasToken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// test plan functional 8.37.7
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void PolicyWithSASRemovePolicy()
        {
            blobUtil.SetupTestContainerAndBlob();

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                string policyName = Utility.GenNameString("saspolicy");
                DateTime? expiryTime = DateTime.Today.AddDays(10);
                DateTime? startTime = DateTime.Today.AddDays(-2);
                string permission = "r";

                //start time is in the past
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, blobUtil.Container, false);
                string sasToken = CommandAgent.GetContainerSasFromCmd(blobUtil.Container.Name, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                blobUtil.ValidateContainerReadableWithSasToken(blobUtil.Container, sasToken);

                //remove the policy
                startTime = DateTime.Today.AddDays(2);
                CommandAgent.RemoveAzureStorageContainerStoredAccessPolicy(blobUtil.Container.Name, policyName);
                Test.Info("Sleep and wait for sas policy taking effect");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));

                try
                {
                    blobUtil.ValidateContainerReadableWithSasToken(blobUtil.Container, sasToken);
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
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        internal void CreateStoredAccessPolicyAndValidate(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, string containerName = null, bool ifCleanUpContainer = true, bool ifCleanUpPolicy = true)
        {
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                //create the policy
                CreateStoredAccessPolicy(policyName, permission, startTime, expiryTime, container, ifCleanUpPolicy);

                //get the policy and validate
                SharedAccessBlobPolicies expectedPolicies = new SharedAccessBlobPolicies();
                expectedPolicies.Add(policyName, Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(startTime, expiryTime, permission));
                
                Utility.RawStoredAccessPolicy policy = new Utility.RawStoredAccessPolicy(policyName, startTime, expiryTime, permission);
                Utility.WaitForPolicyBecomeValid<CloudBlobContainer>(container, policy); 
                Utility.ValidateStoredAccessPolicies<SharedAccessBlobPolicy>(container.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                if (ifCleanUpPolicy)
                {
                    Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
                }
                if (ifCleanUpContainer)
                {
                    blobUtil.RemoveContainer(container);
                }
            }
        }

        internal void CreateStoredAccessPolicy(string policyName, string permission, DateTime? startTime, DateTime? expiryTime, CloudBlobContainer container, bool ifCleanUpPolicy = true)
        {
            if (ifCleanUpPolicy)
            {
                Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            }

            Test.Assert(CommandAgent.NewAzureStorageContainerStoredAccessPolicy(container.Name, policyName, permission, startTime, expiryTime),
                "Create stored access policy in container should succeed");
            Test.Info("Created stored access policy:{0}", policyName);
        }

        internal void SetStoredAccessPolicyAndValidate(Utility.RawStoredAccessPolicy policy1, Utility.RawStoredAccessPolicy policy2, string containerName = null, bool ifCleanupPolicy = true, bool ifCleanupContainer = true)
        {
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            if (ifCleanupPolicy)
            {
                Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            }

            policy2.PolicyName = policy1.PolicyName;

            try
            {
                CommandAgent.NewAzureStorageContainerStoredAccessPolicy(container.Name, policy1.PolicyName, policy1.Permission, policy1.StartTime, policy1.ExpiryTime);
                Test.Assert(CommandAgent.SetAzureStorageContainerStoredAccessPolicy(container.Name, policy2.PolicyName, policy2.Permission, policy2.StartTime, policy2.ExpiryTime),
                "Set stored access policy in container should succeed");
                Test.Info("Set stored access policy:{0}", policy2.PolicyName);

                //get the policy and validate
                SharedAccessBlobPolicies expectedPolicies = new SharedAccessBlobPolicies();
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

                expectedPolicies.Add(policy2.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(policy2.StartTime, policy2.ExpiryTime, policy2.Permission));
                Utility.WaitForPolicyBecomeValid<CloudBlobContainer>(container, policy2);
                Utility.ValidateStoredAccessPolicies<SharedAccessBlobPolicy>(container.GetPermissions().SharedAccessPolicies, expectedPolicies);

                //validate the output
                SharedAccessBlobPolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(policy2.StartTime, policy2.ExpiryTime, policy2.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessBlobPolicy>(policy, policy2.PolicyName));
                CommandAgent.OutputValidation(comp);
            }
            finally
            {
                if (ifCleanupContainer)
                {
                    blobUtil.RemoveContainer(container);
                }
            }
        }
    }
}

