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

namespace Management.Storage.ScenarioTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.Azure.Commands.Common.Authentication.Models;
    using Microsoft.Azure.Management.Storage;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Management;
    using Microsoft.WindowsAzure.Management.Storage;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using SRPModel = Microsoft.Azure.Management.Storage.Models;
    using Microsoft.Azure.Commands.Management.Storage.Models;
    using System.Collections;

    /// <summary>
    /// this class contains all the account parameter settings for Node.js commands
    /// </summary>
    [TestClass]
    public class WormTest : TestBase
    {
        private static ManagementClient managementClient;
        protected static AccountUtils accountUtils;
        protected static string resourceGroupName = string.Empty;
        protected static string accountName = string.Empty;
        private static ResourceManagerWrapper resourceManager;
        private static string resourceLocation;

        private const string allowedLocation = Constants.Location.WestUS;

        #region Additional test attributes

        [ClassInitialize()]
        public static void StorageAccountTestInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            if (isResourceMode)
            {
                NodeJSAgent.AgentConfig.UseEnvVar = false;

                AzureEnvironment environment = Utility.GetTargetEnvironment();
                managementClient = new ManagementClient(Utility.GetCertificateCloudCredential(),
                        environment.GetEndpointAsUri(AzureEnvironment.Endpoint.ServiceManagement));

                accountUtils = new AccountUtils(lang, isResourceMode);

                accountName = accountUtils.GenerateAccountName();
                
                resourceLocation = isMooncake ? Constants.MCLocation.ChinaEast : allowedLocation;
                resourceManager = new ResourceManagerWrapper();
                resourceGroupName = accountUtils.GenerateResourceGroupName();
                resourceManager.CreateResourceGroup(resourceGroupName, resourceLocation);

                var parameters = new SRPModel.StorageAccountCreateParameters(new SRPModel.Sku(SRPModel.SkuName.StandardLRS), SRPModel.Kind.StorageV2,
                    isMooncake ? Constants.MCLocation.ChinaEast : Constants.Location.EastUS2Stage);
                accountUtils.SRPStorageClient.StorageAccounts.CreateAsync(resourceGroupName, accountName, parameters, CancellationToken.None).Wait();

                //resourceGroupName = "weitest";
                //accountName = "weitesttemp";
            }
        }

        [ClassCleanup()]
        public static void StorageAccountTestCleanup()
        {
            if (isResourceMode)
            {
                try
                {
                    accountUtils.SRPStorageClient.StorageAccounts.DeleteAsync(resourceGroupName, accountName, CancellationToken.None).Wait();
                    resourceManager.DeleteResourceGroup(resourceGroupName);
                }
                catch (Exception ex)
                {
                    Test.Info(string.Format("SRP cleanup exception: {0}", ex));
                }
            }

            TestBase.TestClassCleanup();
        }

        #endregion

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(Tag.Function_SRP)]
        public void CreateContainer_allParameter()
        {
            if (isResourceMode)
            {
                string containerName = Utility.GenNameString("container");
                //Hashtable[] metadata = StorageAccountTest.GetUnicodeTags();
                Hashtable metadata = new Hashtable(3);
                metadata.Add("key1", "value1");
                metadata.Add("key2", "value2");
                metadata.Add("key3", "value3");

                Test.Assert(CommandAgent.NewAzureRmStorageContainer(resourceGroupName, accountName, containerName, metadata, PublicAccess: PSPublicAccess.Container), "Create Container should success.");
                PSContainer con = GetContainer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, metadata, PSPublicAccess.Container);

                CommandAgent.RemoveAzureRmStorageContainer(resourceGroupName, accountName, containerName);

            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(Tag.Function_SRP)]
        public void UpdateContainer_allParameter()
        {
            if (isResourceMode)
            {
                string containerName = Utility.GenNameString("container");

                Hashtable metadata = new Hashtable(3);
                metadata.Add("key1", "value1");
                metadata.Add("key2", "value2");
                metadata.Add("key3", "value3");

                //Creat container
                Test.Assert(CommandAgent.NewAzureRmStorageContainer(resourceGroupName, accountName, containerName), "Create Container should success."); 
                PSContainer con = GetContainer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName);

                //Set Container
                Test.Assert(CommandAgent.UpdateAzureRmStorageContainer(resourceGroupName, accountName, containerName, metadata, PSPublicAccess.Blob), "Update Container should success.");
                con = GetContainer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, metadata, PSPublicAccess.Blob);


                //Set Container
                Test.Assert(CommandAgent.UpdateAzureRmStorageContainer(resourceGroupName, accountName, containerName, new Hashtable(), PSPublicAccess.None), "Update Container should success.");
                con = GetContainer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName);

                //Remove Container
                Test.Assert(CommandAgent.RemoveAzureRmStorageContainer(resourceGroupName, accountName, containerName), "Remove Container should success.");

            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(Tag.Function_SRP)]
        public void GetContainer()
        {
            if (isResourceMode)
            {
                string containerName = Utility.GenNameString("container");

                Hashtable metadata = new Hashtable(2);
                metadata.Add("key1", "value1");
                metadata.Add("key3", "value3");

                //Creat container
                Test.Assert(CommandAgent.NewAzureRmStorageContainer(resourceGroupName, accountName, containerName, metadata, PublicAccess: PSPublicAccess.Blob), "Create Container should success.");
                PSContainer con = GetContainer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, metadata, PSPublicAccess.Blob);

                //get Container
                con = GetContainerFromServer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, metadata, PSPublicAccess.Blob);

                //Remove Container
                Test.Assert(CommandAgent.RemoveAzureRmStorageContainer(resourceGroupName, accountName, containerName), "Remove Container should success.");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(Tag.Function_SRP)]
        public void ListContainer()
        {
            if (isResourceMode)
            {
                string containerName = Utility.GenNameString("container");


                PSContainer[] cons1 = ListContainersFromServer();

                //Creat container
                Test.Assert(CommandAgent.NewAzureRmStorageContainer(resourceGroupName, accountName, containerName, PublicAccess: PSPublicAccess.Container), "Create Container should success.");
                PSContainer con = GetContainer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, publicaccess: PSPublicAccess.Container);

                //get Container
                PSContainer[] cons2 = ListContainersFromServer();
                Test.AssertEqual(cons1.Length + 1, cons2.Length);

                //Remove Container
                Test.Assert(CommandAgent.RemoveAzureRmStorageContainer(resourceGroupName, accountName, containerName), "Remove Container should success.");


                //get Container
                cons2 = ListContainersFromServer();
                Test.AssertEqual(cons1.Length, cons2.Length);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(Tag.Function_SRP)]
        public void LegalHoldTest()
        {
            if (isResourceMode)
            {
                string containerName = Utility.GenNameString("container");

                string[] legalhold = new string[] { "tag1", "tag2", "tag3", "tag4" };
                string[] legalhold2Remove = new string[] { "tag2", "tag3" };
                string[] legalholdLeft = new string[] { "tag1", "tag4" };

                //Creat container
                Test.Assert(CommandAgent.NewAzureRmStorageContainer(resourceGroupName, accountName, containerName, PublicAccess: PSPublicAccess.Container), "Create Container should success.");
                PSContainer con = GetContainer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, publicaccess: PSPublicAccess.Container);

                //Add Container LegalHold
                Test.Assert(CommandAgent.AddAzureRmStorageContainerLegalHold(resourceGroupName, accountName, containerName, legalhold), "Add Container LegalHold should success.");
                PSLegalHold outputLegalhold = GetLegalholdFromOutput();
                CompareLegalhold(legalhold, outputLegalhold);

                //Remove Container LegalHold
                Test.Assert(CommandAgent.RemoveAzureRmStorageContainerLegalHold(resourceGroupName, accountName, containerName, legalhold2Remove), "Remove Container LegalHold should success.");
                outputLegalhold = GetLegalholdFromOutput();
                CompareLegalhold(legalholdLeft, outputLegalhold);

                //Remove Container fail when legalhold exist
                Test.Assert(!CommandAgent.RemoveAzureRmStorageContainer(resourceGroupName, accountName, containerName), "Remove Container with legalhold should fail.");
                ExpectedContainErrorMessage(string.Format("The storage account {0} container {1} is protected from deletion due to LegalHold.", accountName, containerName));

                //Remove Container LegalHold
                Test.Assert(CommandAgent.RemoveAzureRmStorageContainerLegalHold(resourceGroupName, accountName, containerName, legalholdLeft), "Remove Container LegalHold should success.");
                outputLegalhold = GetLegalholdFromOutput();
                CompareLegalhold(null, outputLegalhold);

                //Remove container
                Test.Assert(CommandAgent.RemoveAzureRmStorageContainer(resourceGroupName, accountName, containerName), "Remove Container should success.");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(Tag.Function_SRP)]
        public void ImmutabilityPolicy_SetGetDelete()
        {
            if (isResourceMode)
            {
                string containerName = Utility.GenNameString("container");

                int immutabilityPeriod = 3;

                //Creat container
                Test.Assert(CommandAgent.NewAzureRmStorageContainer(resourceGroupName, accountName, containerName, PublicAccess: PSPublicAccess.Blob), "Create Container should success.");
                PSContainer con = GetContainer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, publicaccess: PSPublicAccess.Blob);

                //Get ImmutabilityPolicy
                Test.Assert(CommandAgent.GetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName), "Get Container ImmutabilityPolicy should success.");
                PSImmutabilityPolicy immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, 0, "Unlocked");

                //Set ImmutabilityPolicy
                Test.Assert(CommandAgent.SetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName, immutabilityPeriod), "Set Container ImmutabilityPolicy should success.");
                immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod, "Unlocked");

                //Get ImmutabilityPolicy
                Test.Assert(CommandAgent.GetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName), "Get Container ImmutabilityPolicy should success.");
                immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod, "Unlocked");

                //Remove ImmutabilityPolicy
                Test.Assert(CommandAgent.RemoveAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName, immuPolicy.Etag), "Remove Container ImmutabilityPolicy should success.");
                Test.Assert(CommandAgent.GetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName), "Get Container ImmutabilityPolicy should success.");
                immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, 0, "Unlocked");

                //Remove Container
                Test.Assert(CommandAgent.RemoveAzureRmStorageContainer(resourceGroupName, accountName, containerName), "Remove Container should success.");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(Tag.Function_SRP)]
        public void ImmutabilityPolicy_LockExtend()
        {
            if (isResourceMode)
            {
                string containerName = Utility.GenNameString("container");

                int immutabilityPeriod = 5;

                //Creat container
                Test.Assert(CommandAgent.NewAzureRmStorageContainer(resourceGroupName, accountName, containerName, PublicAccess: PSPublicAccess.None), "Create Container should success.");
                PSContainer con = GetContainer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, publicaccess: PSPublicAccess.None);

                //Set ImmutabilityPolicy
                Test.Assert(CommandAgent.SetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName, immutabilityPeriod), "Set Container ImmutabilityPolicy should success.");
                PSImmutabilityPolicy immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod, "Unlocked");

                //Lock ImmutabilityPolicy
                Test.Assert(CommandAgent.LockAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName, immuPolicy.Etag), "Lock Container ImmutabilityPolicy should success.");
                immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod, "Locked");

                //Get ImmutabilityPolicy
                Test.Assert(CommandAgent.GetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName), "Get Container ImmutabilityPolicy should success.");
                immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod, "Locked");

                //Set ImmutabilityPolicy fail
                Test.Assert(!CommandAgent.SetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName, 2, Etag: immuPolicy.Etag), "Set Container ImmutabilityPolicy should fail.");
                ExpectedContainErrorMessage(string.Format("Operation not allowed on immutability policy with current state."));

                //Extend ImmutabilityPolicy
                immutabilityPeriod = 10;
                Test.Assert(CommandAgent.SetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName, immutabilityPeriod, extendPolicy: true, Etag: immuPolicy.Etag), "Extend Container ImmutabilityPolicy should success.");
                immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod, "Locked");

                //Get ImmutabilityPolicy
                Test.Assert(CommandAgent.GetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName), "Get Container ImmutabilityPolicy should success.");
                immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod, "Locked");

                //Remove ImmutabilityPolicy fail
                Test.Assert(!CommandAgent.RemoveAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName, immuPolicy.Etag), "Remove Container ImmutabilityPolicy should fail.");
                ExpectedContainErrorMessage(string.Format("Operation not allowed on immutability policy with current state."));

                //Remove Container
                Test.Assert(CommandAgent.RemoveAzureRmStorageContainer(resourceGroupName, accountName, containerName), "Remove Container should success.");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(Tag.Function_SRP)]
        public void GetContainer_withLegalHoldAndImmutabilityPolicy()
        {
            if (isResourceMode)
            {
                string containerName = Utility.GenNameString("container");

                //Creat container
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(
                    string.Format("Get-AzureRMStorageAccount -ResourceGroupName {0} -Name {1} | New-AzureRmStorageContainer -ContainerName {2}", 
                    resourceGroupName, 
                    accountName, 
                    containerName)),
                    string.Format("Create container for storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                PSContainer con = GetContainerFromServer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, publicaccess: PSPublicAccess.None);

                //Set ImmutabilityPolicy
                int immutabilityPeriod = 5;
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(
                    string.Format("Get-AzureRMStorageAccount -ResourceGroupName {0} -Name {1} | Set-AzureRmStorageContainerImmutabilityPolicy -ContainerName {2} -ImmutabilityPeriod {3}", 
                    resourceGroupName, 
                    accountName, 
                    containerName,
                    immutabilityPeriod)),
                    string.Format("Set Container ImmutabilityPolicy should success."));
                Test.Assert(CommandAgent.GetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName), "Get Container ImmutabilityPolicy should success.");
                PSImmutabilityPolicy immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod, "Unlocked");

                //Lock ImmutabilityPolicy
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(
                    string.Format("Get-AzureRmStorageContainer -ResourceGroupName {0} -StorageAccountName {1} -Name {2} | Lock-AzureRmStorageContainerImmutabilityPolicy -Etag '{3}' -Force",
                    resourceGroupName,
                    accountName,
                    containerName,
                    immuPolicy.Etag)),
                    string.Format("Lock Container ImmutabilityPolicy should success."));
                Test.Assert(CommandAgent.GetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName), "Get Container ImmutabilityPolicy should success.");
                immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod, "Locked");
               
                //Extend ImmutabilityPolicy
                int immutabilityPeriod2 = 10;
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(
                    string.Format("Get-AzureRmStorageContainerImmutabilityPolicy -ResourceGroupName {0} -StorageAccountName {1} -ContainerName {2} | Set-AzureRmStorageContainerImmutabilityPolicy -ImmutabilityPeriod {3} -ExtendPolicy",
                    resourceGroupName,
                    accountName,
                    containerName, 
                    immutabilityPeriod2)),
                    string.Format("Extend Container ImmutabilityPolicy should success."));
                Test.Assert(CommandAgent.GetAzureRmStorageContainerImmutabilityPolicy(resourceGroupName, accountName, containerName), "Get Container ImmutabilityPolicy should success.");
                immuPolicy = GetImmutabilityPolicyFromOutput();
                ValidateImmutabilityPolicy(immuPolicy, immutabilityPeriod2, "Locked");

                //Add Container LegalHold
                string[] legalhold = new string[] { "legalholdtag1", "legalholdtag2" };
                Test.Assert(CommandAgent.AddAzureRmStorageContainerLegalHold(resourceGroupName, accountName, containerName, legalhold), "Add Container LegalHold should success.");
                PSLegalHold outputLegalhold = GetLegalholdFromOutput();
                CompareLegalhold(legalhold, outputLegalhold);

                //get Container and validate legalhold, ImmutabilityPolicy
                con = GetContainerFromServer(containerName);
                ValidateContainer(con, resourceGroupName, accountName, containerName, publicaccess: PSPublicAccess.None);
                Test.Assert(con.HasImmutabilityPolicy.Value, "Should has HasImmutabilityPolicy = true");
                Test.Assert(con.HasLegalHold.Value, "Should has HasLegalHold = true");

                Test.AssertEqual(immutabilityPeriod2, con.ImmutabilityPolicy.ImmutabilityPeriodSinceCreationInDays);
                Test.AssertEqual("Locked", con.ImmutabilityPolicy.State);
                Test.AssertEqual(3, con.ImmutabilityPolicy.UpdateHistory.Length);
                Test.AssertEqual("put", con.ImmutabilityPolicy.UpdateHistory[0].Update);
                Test.AssertEqual(immutabilityPeriod, con.ImmutabilityPolicy.UpdateHistory[0].ImmutabilityPeriodSinceCreationInDays.Value);
                Test.AssertEqual("lock", con.ImmutabilityPolicy.UpdateHistory[1].Update);
                Test.AssertEqual(immutabilityPeriod, con.ImmutabilityPolicy.UpdateHistory[1].ImmutabilityPeriodSinceCreationInDays.Value);
                Test.AssertEqual("extend", con.ImmutabilityPolicy.UpdateHistory[2].Update);
                Test.AssertEqual(immutabilityPeriod2, con.ImmutabilityPolicy.UpdateHistory[2].ImmutabilityPeriodSinceCreationInDays.Value);

                Test.AssertEqual(legalhold.Length, con.LegalHold.Tags.Length);
                if (legalhold.Length == con.LegalHold.Tags.Length)
                {
                    for (int i = 0; i < legalhold.Length; i++)
                    {
                        Test.AssertEqual(legalhold[i], con.LegalHold.Tags[i].Tag);
                    }
                }

                //Remove Legalhold and Container
                //Test.Assert(CommandAgent.RemoveAzureRmStorageContainerLegalHold(resourceGroupName, accountName, containerName, legalhold), "Remove Container LegalHold should success.");
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(
                    string.Format("Get-AzureRMStorageAccount -ResourceGroupName {0} -Name {1} | Remove-AzureRmStorageContainerLegalHold -Name {2} -Tag {3}",
                    resourceGroupName,
                    accountName,
                    containerName,
                    legalhold[0] + "," + legalhold[1])),
                    string.Format("Remove Container LegalHold should success."));
                //outputLegalhold = GetLegalholdFromOutput();
                //CompareLegalhold(null, outputLegalhold);


                //Test.Assert(CommandAgent.RemoveAzureRmStorageContainer(resourceGroupName, accountName, containerName), "Remove Container should success.");
                Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(
                    string.Format("Get-AzureRmStorageContainer -ResourceGroupName {0} -StorageAccountName {1} -Name {2} | Remove-AzureRmStorageContainer -Force",
                    resourceGroupName,
                    accountName,
                    containerName)),
                    string.Format("Remove Container should success."));
            }
        }

        #region Help Functions

        private PSContainer GetContainer(string containerName, string accountNameToGet = null)
        {
            if (random.Next() % 2 == 0)
                return GetContainerFromServer(containerName, accountNameToGet);
            else
                return GetContainerFromOutput();
        }

        private PSContainer[] ListContainers(string accountNameToGet = null)
        {
            if (random.Next() % 2 == 0)
                return ListContainersFromServer(accountNameToGet);
            else
                return ListContainersFromOutput();
        }


        //Get a list of containers from output
        private PSContainer[] ListContainersFromOutput()
        {
            try
            {
                List<PSContainer> containers = new List<PSContainer>();

                for (int i=0; i< CommandAgent.Output.Count; i++)
                {
                    containers.Add(CommandAgent.Output[i]["_baseObject"] as PSContainer);
                }

                return containers.ToArray();

                //return CommandAgent.Output[0]["_baseObject"] as PSContainer[];
            }
            catch (Exception e)
            {
                Test.Warn("Get PSContainer from output failed: " + e.Message);
            }
            return null;
        }


        // Run Get-AzureRmStorageContainer and return result
        private PSContainer[] ListContainersFromServer(string accountNameToGet = null)
        {
            if (accountNameToGet == null)
                accountNameToGet = accountName;
            Test.Assert(CommandAgent.GetAzureRmStorageContainer(resourceGroupName, accountNameToGet),
                       string.Format("List containers of storage account {0} in the resource group {1} should success.", accountNameToGet, resourceGroupName));

            return ListContainersFromOutput();
        }

        //Get a container from output
        private PSContainer GetContainerFromOutput()
        {
            try
            {
                return CommandAgent.Output[0]["_baseObject"] as PSContainer;
            }
            catch (Exception e)
            {
                Test.Warn("Get PSContainer from output failed: " + e.Message);
            }
            return null;
        }


        // Run Get-AzureRmStorageContainer and return result
        private PSContainer GetContainerFromServer(string containerName, string accountNameToGet = null)
        {
            if (accountNameToGet == null)
                accountNameToGet = accountName;
            Test.Assert(CommandAgent.GetAzureRmStorageContainer(resourceGroupName, accountNameToGet, containerName),
                       string.Format("Get container {2} of storage account {0} in the resource group {1} should success.", accountNameToGet, resourceGroupName, containerName));

            return GetContainerFromOutput();
        }

        public static void ValidateContainer(PSContainer container, string resourceGroupName, string accoutName, string containerName, Hashtable metadata = null, PSPublicAccess? publicaccess = PSPublicAccess.None)
        {
            Test.AssertEqual(containerName, container.Name);
            Test.AssertEqual(resourceGroupName, container.ResourceGroupName);
            Test.AssertEqual(accountName, container.StorageAccountName);
            Test.AssertEqual(publicaccess.ToString(), container.PublicAccess == null ? PSPublicAccess.None.ToString() : container.PublicAccess.ToString());
            accountUtils.ValidateTags(metadata == null ? null : new Hashtable[] { metadata }, container.Metadata);
        }


        private PSLegalHold GetLegalholdFromOutput()
        {
            try
            {
                return CommandAgent.Output[0]["_baseObject"] as PSLegalHold;
            }
            catch (Exception e)
            {
                Test.Warn("Get Legalhold from output failed: " + e.Message);
            }
            return null;
        }

        public static void CompareLegalhold (string[] expect, PSLegalHold real)
        {
            if (expect == null || expect.Length == 0)
            {
                Test.Assert(!real.HasLegalHold.Value, "There should be no legal hold.");
                Test.Assert((real == null || real.Tags.Length == 0), "There should be no legal hold.");
                return;
            }

            Test.Assert(real.HasLegalHold.Value, "There should be some legal hold.");
            Test.AssertEqual(expect.Length, real.Tags.Length);

            for (int i =0; i < expect.Length; i++)
            {
                Test.AssertEqual(expect[i], real.Tags[i]);
            }
        }

        public static PSImmutabilityPolicy GetImmutabilityPolicyFromOutput()
        {
            try
            {
                return CommandAgent.Output[0]["_baseObject"] as PSImmutabilityPolicy;
            }
            catch (Exception e)
            {
                Test.Warn("Get Legalhold from output failed: " + e.Message);
            }
            return null;
        }

        public static void ValidateImmutabilityPolicy(PSImmutabilityPolicy policy, int ImmutabilityPeriodSinceCreationInDays, string state = null)
        {
            Test.AssertEqual(ImmutabilityPeriodSinceCreationInDays, policy.ImmutabilityPeriodSinceCreationInDays);
            if (state != null)
            {
                Test.AssertEqual(state, policy.State);
            }
        }

        #endregion
    }
}

