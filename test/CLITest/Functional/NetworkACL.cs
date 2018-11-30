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
#if !DOTNET5_4
    using Microsoft.WindowsAzure.Management;
    using Microsoft.WindowsAzure.Management.Storage;
#endif
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using SRPModel = Microsoft.Azure.Management.Storage.Models;
    using Microsoft.Azure.Commands.Management.Storage.Models;

    /// <summary>
    /// this class contains all the account parameter settings for Node.js commands
    /// </summary>
    [TestClass]
    public class NetworkACLTest : TestBase
    {
#if !DOTNET5_4
        private static ManagementClient managementClient;
#endif
        protected static AccountUtils accountUtils;
        protected static string resourceGroupName = string.Empty;
        protected static string accountName = string.Empty;
        private static ResourceManagerWrapper resourceManager;
        private static string resourceLocation;

        //Created with cmdlet: New-AzureRmVirtualNetwork -ResourceGroupName $group -Location $location -AddressPrefix 10.0.0.0/24 -Name $vnetName
        //Get-AzureRmVirtualNetwork -ResourceGroupName $group -Name $vnetName | Add-AzureRmVirtualNetworkSubnetConfig -Name "subnet1" -AddressPrefix "10.0.0.16/28" -ServiceTunnel "Microsoft.Storage" | Set-AzureRmVirtualNetwork
        private static string[] allowedNetworkRules;

        private const int maxRuleCount = 100;
        private const string allowedLocation = Constants.Location.EastUS2EUAP;
#region Additional test attributes

        [ClassInitialize()]
        public static void StorageAccountTestInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            if (isResourceMode)
            {
#if !DOTNET5_4
                NodeJSAgent.AgentConfig.UseEnvVar = false;
#endif

                AzureEnvironment environment = Utility.GetTargetEnvironment();

                accountUtils = new AccountUtils(lang, isResourceMode);

                accountName = accountUtils.GenerateAccountName();

                //resourceLocation = isMooncake ? Constants.MCLocation.ChinaEast : Constants.Location.EastAsia;
                resourceLocation = isMooncake ? Constants.MCLocation.ChinaEast : allowedLocation;
                resourceManager = new ResourceManagerWrapper();
                resourceGroupName = accountUtils.GenerateResourceGroupName();
                resourceManager.CreateResourceGroup(resourceGroupName, resourceLocation);

                var parameters = new SRPModel.StorageAccountCreateParameters(new SRPModel.Sku(SRPModel.SkuName.StandardGRS), SRPModel.Kind.Storage,
                    isMooncake ? Constants.MCLocation.ChinaEast : allowedLocation);
                accountUtils.SRPStorageClient.StorageAccounts.CreateAsync(resourceGroupName, accountName, parameters, CancellationToken.None).Wait();

                allowedNetworkRules = Test.Data.Get("allowedNetworkRules").Split(new char[] { ',', '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
        [TestCategory(Tag.Function_SRP)]
        public void UpdateNetworkAcl_AllParameters()
        {
            if (isResourceMode)
            {
                UpdatetNetworkACLCheckResult(PSNetWorkRuleBypassEnum.AzureServices, 
                    PSNetWorkRuleDefaultActionEnum.Allow, 
                    GetIpRules(GetRandomIpRules(random.Next(0, maxRuleCount + 1))), 
                    GetNetworkRules(GetRandomNetworkRules(random.Next(0, allowedNetworkRules.Length + 1))));
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void UpdateNetworkAcl_SingleParameters_Bypass()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = GetNetworkAclFromServer();

                acl = UpdatetNetworkACLCheckResult(PSNetWorkRuleBypassEnum.Metrics | PSNetWorkRuleBypassEnum.Logging | PSNetWorkRuleBypassEnum.AzureServices, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(PSNetWorkRuleBypassEnum.None, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(PSNetWorkRuleBypassEnum.None | PSNetWorkRuleBypassEnum.Metrics, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(PSNetWorkRuleBypassEnum.AzureServices | PSNetWorkRuleBypassEnum.Logging, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(PSNetWorkRuleBypassEnum.Metrics, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(PSNetWorkRuleBypassEnum.Logging, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(PSNetWorkRuleBypassEnum.Logging | PSNetWorkRuleBypassEnum.Metrics, originalAcl: acl);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void UpdateNetworkAcl_SingleParameters_DefaultAction()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = GetNetworkAclFromServer();

                acl = UpdatetNetworkACLCheckResult(defaultAction: PSNetWorkRuleDefaultActionEnum.Deny, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(defaultAction: PSNetWorkRuleDefaultActionEnum.Allow, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(defaultAction: PSNetWorkRuleDefaultActionEnum.Deny, originalAcl: acl);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void UpdateNetworkAcl_SingleParameters_IpRules()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = GetNetworkAclFromServer();
                PSIpRule[] ipRules = GetIpRules(GetRandomIpRules(random.Next(1, maxRuleCount)));

                acl = UpdatetNetworkACLCheckResult(ipRules: ipRules, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(ipRules: new PSIpRule[] { }, originalAcl: acl);
                acl = UpdatetNetworkACLCheckResult(ipRules: ipRules, originalAcl: acl);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void UpdateNetworkAcl_SingleParameters_NetworkRules()
        {
            if (isResourceMode)
            {
                if (allowedNetworkRules.Length > 0)
                {
                    PSNetworkRuleSet acl = GetNetworkAclFromServer();
                    PSVirtualNetworkRule[] networkRules = GetNetworkRules(GetRandomNetworkRules(random.Next(1, allowedNetworkRules.Length)));

                    acl = UpdatetNetworkACLCheckResult(networkRules: networkRules, originalAcl: acl);
                    acl = UpdatetNetworkACLCheckResult(networkRules: new PSVirtualNetworkRule[] { }, originalAcl: acl);
                    acl = UpdatetNetworkACLCheckResult(networkRules: networkRules, originalAcl: acl);
                }
                else
                {
                    Test.Warn("Can't run this case, as there's not enough allowedNetworkRules to run this case. at least need 1.");
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void UpdateNetworkAcl_NoParameters()
        {
            if (isResourceMode)
            {
                Test.Assert(!CommandAgent.UpdateSRPAzureStorageAccountNetworkAcl(resourceGroupName, accountName),
                           string.Format("Update NetworkACL with no parameter on storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage("Request must specify an account NetworkRule property to update.");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void GetNetworkAcl_NewAccount()
        {
            if (isResourceMode)
            {
                ReGenerateAccount();
                PSNetworkRuleSet acl = GetNetworkAclFromServer();
                ValidateNetworkACL(acl, PSNetWorkRuleBypassEnum.AzureServices, PSNetWorkRuleDefaultActionEnum.Allow, new PSIpRule[] { }, new PSVirtualNetworkRule[] { });
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void GetNetworkAcl_PipeLine()
        {
            if (isResourceMode)
            {
                UpdatetNetworkACLCheckResult(null, null,
                    GetIpRules(GetRandomIpRules(random.Next(1, 4))),
                    GetNetworkRules(GetRandomNetworkRules(random.Next(1, allowedNetworkRules.Length > 3 ? 4 : allowedNetworkRules.Length + 1))));
                PSNetworkRuleSet acl = GetNetworkAclFromOutput();

                string accountName2 = accountUtils.GenerateAccountName();

                try
                {
                    //Create a second account
                    var parameters = new SRPModel.StorageAccountCreateParameters(new SRPModel.Sku(SRPModel.SkuName.StandardGRS), SRPModel.Kind.Storage,
                        isMooncake ? Constants.MCLocation.ChinaEast : allowedLocation);
                    accountUtils.SRPStorageClient.StorageAccounts.CreateAsync(resourceGroupName, accountName2, parameters, CancellationToken.None).Wait();

                    //Add Rules
                    // Add Unary Operators "," in the begin of the script, see more detail in http://stackoverflow.com/questions/29973212/pipe-complete-array-objects-instead-of-array-items-one-at-a-time       
                    Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(Get-AzureRMStorageAccountNetworkRuleSet -ResourceGroupName {0} -Name {1}).IpRules | Add-AzureRMStorageAccountNetworkRule -ResourceGroupName {0} -Name {2}", resourceGroupName, accountName, accountName2)),
                        string.Format("Add IPRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                    PSNetworkRuleSet acl2 = GetNetworkAclFromServer(accountName2);
                    ValidateIpRules(acl.IpRules, acl2.IpRules);

                    if (allowedNetworkRules.Length > 0)
                    {
                        Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(Get-AzureRMStorageAccountNetworkRuleSet -ResourceGroupName {0} -Name {1}).VirtualNetworkRules | Add-AzureRMStorageAccountNetworkRule -ResourceGroupName {0} -Name {2}", resourceGroupName, accountName, accountName2)),
                            string.Format("Add VirtualNetworkRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                        acl2 = GetNetworkAclFromServer(accountName2);
                        ValidateNetworkRules(acl.VirtualNetworkRules, acl2.VirtualNetworkRules);
                    }

                    //Remove Rules
                    Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(Get-AzureRMStorageAccountNetworkRuleSet -ResourceGroupName {0} -Name {1}).IpRules | Remove-AzureRMStorageAccountNetworkRule -ResourceGroupName {0} -Name {2}", resourceGroupName, accountName, accountName2)),
                        string.Format("Remove IPRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                    acl2 = GetNetworkAclFromServer(accountName2);
                    ValidateIpRules(new PSIpRule[] { }, acl2.IpRules);


                    if (allowedNetworkRules.Length > 0)
                    {
                        Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(Get-AzureRMStorageAccountNetworkRuleSet -ResourceGroupName {0} -Name {1}).VirtualNetworkRules | Remove-AzureRMStorageAccountNetworkRule -ResourceGroupName {0} -Name {2}", resourceGroupName, accountName, accountName2)),
                        string.Format("Remove VirtualNetworkRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                        acl2 = GetNetworkAclFromServer(accountName2);
                        ValidateNetworkRules(new PSVirtualNetworkRule[] { }, acl2.VirtualNetworkRules);
                    }

                    //Add Rules by Update ACL
                    Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(Get-AzureRMStorageAccountNetworkRuleSet -ResourceGroupName {0} -Name {1}).IpRules | Update-AzureRMStorageAccountNetworkRuleSet -ResourceGroupName {0} -Name {2}", resourceGroupName, accountName, accountName2)),
                        string.Format("Update IPRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                    acl2 = GetNetworkAclFromServer(accountName2);
                    ValidateIpRules(acl.IpRules, acl2.IpRules);

                    Test.Assert((CommandAgent as PowerShellAgent).InvokePSScript(string.Format(",(Get-AzureRMStorageAccountNetworkRuleSet -ResourceGroupName {0} -Name {1}).VirtualNetworkRules | Update-AzureRMStorageAccountNetworkRuleSet -ResourceGroupName {0} -Name {2}", resourceGroupName, accountName, accountName2)),
                        string.Format("Update VirtualNetworkRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                    acl2 = GetNetworkAclFromServer(accountName2);
                    ValidateNetworkRules(acl.VirtualNetworkRules, acl2.VirtualNetworkRules);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    //delete the second account
                    accountUtils.SRPStorageClient.StorageAccounts.DeleteAsync(resourceGroupName, accountName2, CancellationToken.None).Wait();
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void Add_Remove_SingleNetworkAclRule()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = ResetNetworkACLCheckResult();

                //IP Rule
                acl = AddNetworkACLRulesCheckResult(ipAddressRange: new string[] { "20.10.0.0/16" }, originalAcl: acl);
                acl = AddNetworkACLRulesCheckResult(ipRules: GetIpRules(new string[] { "20.11.0.0/16" }), originalAcl: acl);
                acl = RemoveNetworkACLRulesCheckResult(ipRules: GetIpRules(new string[] { "20.11.0.0/16" }), originalAcl: acl);
                acl = RemoveNetworkACLRulesCheckResult(ipAddressRange: new string[] { "20.10.0.0/16" }, originalAcl: acl);

                if (allowedNetworkRules.Length >= 2)
                {
                    //network Rule
                    acl = AddNetworkACLRulesCheckResult(virtualNetworkResourceId: new string[] { allowedNetworkRules[0] }, originalAcl: acl);
                    acl = AddNetworkACLRulesCheckResult(networkRules: GetNetworkRules(new string[] { allowedNetworkRules[1] }), originalAcl: acl);
                    acl = RemoveNetworkACLRulesCheckResult(virtualNetworkResourceId: new string[] { allowedNetworkRules[0] }, originalAcl: acl);
                    acl = RemoveNetworkACLRulesCheckResult(networkRules: GetNetworkRules(new string[] { allowedNetworkRules[1] }), originalAcl: acl);
                }
                else
                {
                    Test.Warn("Can't run this case for networkRule, as there's not enough allowedNetworkRules to run this case. at least need 2.");
                }
            }
        }


        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void Add_Remove_MultipleNetworkAclRule_IPRule()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = ResetNetworkACLCheckResult();

                int addIpRuleCount1 = random.Next(1, maxRuleCount);
                int addIpRuleCount2 = random.Next(1, maxRuleCount + 1 - addIpRuleCount1);
                string[] ipRuleToAdd1 = GetRandomIpRules(addIpRuleCount1);
                string[] ipRuleToAdd2 = GetRandomIpRules(addIpRuleCount2, ipRuleToAdd1);
                string[] ipRuleToRemove1 = GetRandomRulesToRemove(ipRuleToAdd1);
                string[] ipRuleToRemove2 = GetRandomRulesToRemove(ipRuleToAdd2);

                //Add Rules
                acl = AddNetworkACLRulesCheckResult(ipAddressRange: ipRuleToAdd1, originalAcl: acl);
                acl = AddNetworkACLRulesCheckResult(ipRules: GetIpRules(ipRuleToAdd2), originalAcl: acl);

                //Remove Rules
                acl = RemoveNetworkACLRulesCheckResult(ipRules: GetIpRules(ipRuleToRemove1), originalAcl: acl);
                acl = RemoveNetworkACLRulesCheckResult(ipAddressRange: ipRuleToRemove2, originalAcl: acl);
            }
        }


        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void Add_Remove_MultipleNetworkAclRule_NetworkRule()
        {
            if (isResourceMode)
            {
                if (allowedNetworkRules.Length >= 2)
                {
                    PSNetworkRuleSet acl = ResetNetworkACLCheckResult();

                    int addnetworkRuleCount1 = random.Next(1, allowedNetworkRules.Length);
                    int addnetworkRuleCount2 = random.Next(1, allowedNetworkRules.Length + 1 - addnetworkRuleCount1);
                    string[] networkRuleToAdd1 = GetRandomNetworkRules(addnetworkRuleCount1);
                    string[] networkRuleToAdd2 = GetRandomNetworkRules(addnetworkRuleCount2, networkRuleToAdd1);
                    string[] networkRuleToRemove1 = GetRandomRulesToRemove(networkRuleToAdd1);
                    string[] networkRuleToRemove2 = GetRandomRulesToRemove(networkRuleToAdd2);

                    //Add Rules
                    acl = AddNetworkACLRulesCheckResult(virtualNetworkResourceId: networkRuleToAdd1, originalAcl: acl);
                    acl = AddNetworkACLRulesCheckResult(networkRules: GetNetworkRules(networkRuleToAdd2), originalAcl: acl);

                    //Remove Rules
                    acl = RemoveNetworkACLRulesCheckResult(virtualNetworkResourceId: networkRuleToRemove1, originalAcl: acl);
                    acl = RemoveNetworkACLRulesCheckResult(networkRules: GetNetworkRules(networkRuleToRemove2), originalAcl: acl);
                }
                else
                {
                    Test.Warn("Can't run this case, as there's not enough allowedNetworkRules to run this case. at least need 2.");
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void Add_Remove_EmptyNetworkAclRule()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = ResetNetworkACLCheckResult();
                string emptyError = "because it is an empty array";

                string[] emptyRule = new string[] { };
                PSIpRule[] emptyIpRule = new PSIpRule[] { };
                PSVirtualNetworkRule[] emptyNetworkRule = new PSVirtualNetworkRule[] { };

                //Add IP Rules
                Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, emptyRule, isIPRule: true),
                           string.Format("Add Empty IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(emptyError);
                Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, emptyIpRule),
                           string.Format("Add Empty IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(emptyError);

                //Add Network Rules
                Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, emptyRule, isIPRule: false),
                           string.Format("Add Empty NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(emptyError);
                Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, emptyNetworkRule),
                           string.Format("Add Empty NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(emptyError);

                //Remove IP Rules
                Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, emptyRule, isIPRule: true),
                           string.Format("Remove Empty IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(emptyError);
                Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, emptyIpRule),
                           string.Format("Remove Empty IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(emptyError);

                //Add Network Rules
                Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, emptyRule, isIPRule: false),
                           string.Format("Remove Empty NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(emptyError);
                Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, emptyNetworkRule),
                           string.Format("Remove Empty NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(emptyError);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void Add_Duplicated_NetworkAclRule()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = ResetNetworkACLCheckResult();
                string dupErrorIpRule = "Values for request parameters are invalid: networkAcls.ipRules[*].value(unique).";
                string dupErrorNetworkRule = "Values for request parameters are invalid: networkAcls.virtualNetworkRules[*].id(unique).";

                //Add IP Rules
                string[] ipRules = GetRandomIpRules(random.Next(1, maxRuleCount-1));
                List<string> ipRuleList = new List<string>(ipRules);
                ipRuleList.Add(ipRules[random.Next(0, ipRules.Length)]);
                string[] ipRulesDup = ipRuleList.ToArray();

                Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, ipRulesDup, isIPRule: true),
                           string.Format("Add duplicated IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(dupErrorIpRule);
                Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, GetIpRules(ipRulesDup)),
                           string.Format("Add duplicated IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(dupErrorIpRule);

                if (allowedNetworkRules.Length > 0)
                {
                    //Add Network Rules
                    string[] networkRules = GetRandomNetworkRules(random.Next(1, allowedNetworkRules.Length));
                    List<string> networkRuleList = new List<string>(networkRules);
                    networkRuleList.Add(networkRules[random.Next(0, networkRules.Length)]);
                    string[] networkRulesDup = networkRuleList.ToArray();

                    Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, networkRulesDup, isIPRule: false),
                               string.Format("Add duplicated NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(dupErrorNetworkRule);
                    Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, GetNetworkRules(networkRulesDup)),
                               string.Format("Add duplicated NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(dupErrorNetworkRule);
                }
                else
                {
                    Test.Warn("Can't run this case for networkRule, as there's not enough allowedNetworkRules to run this case. at least need 1.");
                }

            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void Add_Exsiting_NetworkAclRule()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = ResetNetworkACLCheckResult();
                string ExistingErrorIpRule = "networkAcls.ipRules[*].value(unique).";
                string ExistingErrorNetworkRule = "networkAcls.virtualNetworkRules[*].id(unique).";

                string[] ipRules = GetRandomIpRules(random.Next(1, maxRuleCount - 1));
                string[] ipRulesExisting = GetRandomRulesToRemove(ipRules);

                //Add IP Rules
                AddNetworkACLRulesCheckResult(ipAddressRange: ipRules);

                //Add Existing IP Rules
                Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, ipRulesExisting, isIPRule: true),
                           string.Format("Add Existing IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(ExistingErrorIpRule);
                Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, GetIpRules(ipRulesExisting)),
                           string.Format("Add Existing IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(ExistingErrorIpRule);

                if (allowedNetworkRules.Length > 0)
                {
                    string[] networkRules = GetRandomNetworkRules(random.Next(1, allowedNetworkRules.Length));
                    string[] networkRulesExisting = GetRandomRulesToRemove(networkRules);

                    //Add Network Rules
                    AddNetworkACLRulesCheckResult(virtualNetworkResourceId: networkRules);

                    //Add Existing Network Rules
                    Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, networkRulesExisting, isIPRule: false),
                               string.Format("Add Existing NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(ExistingErrorNetworkRule);
                    Test.Assert(!CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, GetNetworkRules(networkRulesExisting)),
                               string.Format("Add Existing NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(ExistingErrorNetworkRule);
                }
                else
                {
                    Test.Warn("Can't run this case for networkRule, as there's not enough allowedNetworkRules to run this case. at least need 1.");
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void Remove_Duplicated_NetworkAclRule()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = ResetNetworkACLCheckResult();
                string dupErrorIpRule = "Can't remove IpRule with specific IPAddressOrRange since not exist";
                string dupErrorNetworkRule = "Can't remove VirtualNetworkRule with specific ResourceId since not exist";

                string[] ipRules = GetRandomIpRules(random.Next(1, maxRuleCount - 1));
                List<string> ipRuleList = new List<string>(ipRules);
                ipRuleList.Add(ipRules[random.Next(0, ipRules.Length)]);
                string[] ipRulesDup = ipRuleList.ToArray();

                //Add IP Rules
                AddNetworkACLRulesCheckResult(ipAddressRange: ipRules);

                //Remove Ip Rules
                Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, ipRulesDup, isIPRule: true),
                           string.Format("Remove duplicated IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(dupErrorIpRule);
                Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, GetIpRules(ipRulesDup)),
                           string.Format("Remove duplicated IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(dupErrorIpRule);


                if (allowedNetworkRules.Length > 0)
                {
                    string[] networkRules = GetRandomNetworkRules(random.Next(1, allowedNetworkRules.Length));
                    List<string> networkRuleList = new List<string>(networkRules);
                    networkRuleList.Add(networkRules[random.Next(0, networkRules.Length)]);
                    string[] networkRulesDup = networkRuleList.ToArray();

                    //Add Network Rules
                    AddNetworkACLRulesCheckResult(virtualNetworkResourceId: networkRules);

                    //Remove Network Rules
                    Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, networkRulesDup, isIPRule: false),
                               string.Format("Remove duplicated NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(dupErrorNetworkRule);
                    Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, GetNetworkRules(networkRulesDup)),
                               string.Format("Remove duplicated NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(dupErrorNetworkRule);
                }
                else
                {
                    Test.Warn("Can't run this case for networkRule, as there's not enough allowedNetworkRules to run this case. at least need 1.");
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void Remove_NotExsiting_NetworkAclRule()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = ResetNetworkACLCheckResult();
                string notExistingErrorIpRule = "Can't remove IpRule with specific IPAddressOrRange since not exist";
                string notExistingErrorNetworkRule = "Can't remove VirtualNetworkRule with specific ResourceId since not exist";

                string[] ipRules = GetRandomIpRules(random.Next(1, maxRuleCount - 1));
                List<string> ipRuleList = new List<string>(GetRandomRulesToRemove(ipRules, allowEmpty: true));
                ipRuleList.Add("255.255.0.0/16");//the ip rule from GetRandomIpRules() always has the 2nd block in 0-99, so 2nd block as 255 not exist
                string[] ipRuleNotExisting = ipRuleList.ToArray();

                //Add IP Rules
                AddNetworkACLRulesCheckResult(ipAddressRange: ipRules);

                //Remove Not Existing IP Rules
                Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, ipRuleNotExisting, isIPRule: true),
                           string.Format("Remove NotExisting IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(notExistingErrorIpRule);
                Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, GetIpRules(ipRuleNotExisting)),
                           string.Format("Remove NotExisting IPRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                ExpectedContainErrorMessage(notExistingErrorIpRule);

                if (allowedNetworkRules.Length >= 2) //Add then remove not existing rule
                {                    
                    string[] networkRules = GetRandomNetworkRules(random.Next(1, allowedNetworkRules.Length - 1));
                    List<string> networkRuleList = new List<string>(GetRandomRulesToRemove(networkRules, allowEmpty: true));
                    networkRuleList.Add(allowedNetworkRules[allowedNetworkRules.Length - 1]); //networkRules only containes Network Rule index from 0 to (allowedNetworkRules.Length-2), so index as (allowedNetworkRules.Length-1) not exist
                    string[] networkRuleNotExisting = networkRuleList.ToArray();

                    //Add Network Rules
                    AddNetworkACLRulesCheckResult(virtualNetworkResourceId: networkRules);

                    //Remove Not Existing Network Rules
                    Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, networkRuleNotExisting, isIPRule: false),
                               string.Format("Remove NotExisting NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(notExistingErrorNetworkRule);
                    Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, GetNetworkRules(networkRuleNotExisting)),
                               string.Format("Remove NotExisting NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(notExistingErrorNetworkRule);
                }
                else //Remove not existing rule
                {
                    Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, GetNetworkRules(new string[] { "/subscriptions/45b60d85-fd72-427a-a708-f994d26e593e/resourceGroups/weitest/providers/Microsoft.Network/virtualNetworks/networkRuleNotExisting" })),
                               string.Format("Remove NotExisting NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(notExistingErrorNetworkRule);
                    Test.Assert(!CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, new string[] { "/subscriptions/45b60d85-fd72-427a-a708-f994d26e593e/resourceGroups/weitest/providers/Microsoft.Network/virtualNetworks/networkRuleNotExisting" }, isIPRule: false),
                               string.Format("Remove NotExisting NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should Fail.", accountName, resourceGroupName));
                    ExpectedContainErrorMessage(notExistingErrorNetworkRule);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void SetAccountWithNetworkAcl()
        {
            if (isResourceMode)
            {
                PSNetworkRuleSet acl = new PSNetworkRuleSet()
                {
                    Bypass = PSNetWorkRuleBypassEnum.AzureServices | PSNetWorkRuleBypassEnum.Metrics,
                    DefaultAction = PSNetWorkRuleDefaultActionEnum.Deny,
                    IpRules = GetIpRules(GetRandomIpRules(random.Next(0, maxRuleCount + 1))),
                    VirtualNetworkRules = GetNetworkRules(GetRandomNetworkRules(random.Next(0, allowedNetworkRules.Length + 1)))
                };

                Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, networkAcl: acl), "Set networkAcl should succeed.");
                ValidateNetworkACL(acl, originalAcl: GetNetworkAclFromServer());

                acl = new PSNetworkRuleSet()
                {
                    Bypass = PSNetWorkRuleBypassEnum.None,
                    DefaultAction = PSNetWorkRuleDefaultActionEnum.Allow,
                    IpRules = GetIpRules(GetRandomIpRules(random.Next(0, maxRuleCount + 1))),
                    VirtualNetworkRules = GetNetworkRules(GetRandomNetworkRules(random.Next(0, allowedNetworkRules.Length + 1)))
                };

                Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, networkAcl: acl), "Set networkAcl should succeed.");
                ValidateNetworkACL(acl, originalAcl: GetNetworkAclFromServer());
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function_SRP)]
        public void NewAccountWithNetworkAcl()
        {
            if (isResourceMode)
            {
                try
                {
                    //Delete the oringal account
                    accountUtils.SRPStorageClient.StorageAccounts.DeleteAsync(resourceGroupName, accountName, CancellationToken.None).Wait();
                }
                catch (Exception e)
                {
                    Test.Warn(string.Format("Remove Account with \" -ResourceGroupName {0} -Name {1} \" fail since: {2}", resourceGroupName, accountName, e.Message));
                }

                try
                {
                    accountName = accountUtils.GenerateAccountName();

                    PSNetworkRuleSet acl = new PSNetworkRuleSet()
                    {
                        Bypass = PSNetWorkRuleBypassEnum.Logging | PSNetWorkRuleBypassEnum.Metrics,
                        DefaultAction = PSNetWorkRuleDefaultActionEnum.Deny,
                        IpRules = GetIpRules(GetRandomIpRules(random.Next(0, maxRuleCount + 1))),
                        VirtualNetworkRules = GetNetworkRules(GetRandomNetworkRules(random.Next(0, allowedNetworkRules.Length + 1)))
                    };

                    Test.Assert(CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, Constants.AccountType.Standard_GRS, allowedLocation, networkAcl: acl), "Create Account with networkAcl should succeed.");
                    ValidateNetworkACL(acl, originalAcl: GetNetworkAclFromServer());
                }
                finally
                {
                    //Delete Account
                    accountUtils.SRPStorageClient.StorageAccounts.DeleteAsync(resourceGroupName, accountName, CancellationToken.None).Wait();
                }
            }
        }

#region Help Functions

        //Generate Random IpAddressOrRange array
        private string[] GetRandomIpRules(int count, string[] existingRules = null)
        {
            Test.Verbose(string.Format("Generate {0} Ip rules.", count));
            List<string> newRules = new List<string>();
            List<string> existRuleList = existingRules == null? new List<string>(): new List<string>(existingRules);
            for (int i =0; i< count; i++)
            {
                string ruleToAdd = string.Empty;
                while(true)
                {
                    ruleToAdd = string.Format("{0}.{1}.{2}.{3}/{4}", random.Next(11, 99), i, random.Next(1, 255), random.Next(1, 255), random.Next(1, 30));
                    if (!existRuleList.Contains(ruleToAdd))
                    {
                        newRules.Add(ruleToAdd);
                        break;
                    }
                }
            }
            return newRules.ToArray();
        }

        //select part of the rules from the input Rules Arrary
        private string[] GetRandomRulesToRemove(string[] rules, bool allowEmpty = false)
        {
            if (rules == null || rules.Length == 0)
                return new string[] { };

            List<string> removeRules = new List<string>();
            for (int i = 0; i < rules.Length; i++)
            {
                if (random.Next()%2 == 0)
                    removeRules.Add(rules[i]);
            }
            if (removeRules.Count == 0 && !allowEmpty)
                removeRules.Add(rules[0]);

            Test.Verbose(string.Format("Generate {0} rules to remove.", removeRules.Count));
            return removeRules.ToArray();
        }

        //Generate Random NetworkRuleResourceID array
        private string[] GetRandomNetworkRules(int count, string[] existingRules = null)
        {
            Test.Verbose(string.Format("Generate {0} network rules.", count));
            List<string> newRules = new List<string>();
            List<string> existRuleList = existingRules == null ? new List<string>() : new List<string>(existingRules);
            if ((count + existRuleList.Count) > allowedNetworkRules.Length)
            {
                count = allowedNetworkRules.Length - existRuleList.Count;
                Test.Warn(String.Format("The Network Rule Count is too much, change it to {0}", count));
            }

            foreach (string rule in allowedNetworkRules)
            {
                if (!existRuleList.Contains(rule))
                {
                    newRules.Add(rule);
                    if (newRules.Count >= count)
                        break;
                }
            }

            return newRules.ToArray();
        }

        // Get NetworkACL object from PowerShell run result
        private PSNetworkRuleSet GetNetworkAclFromOutput( )
        {
            PSNetworkRuleSet acl = new PSNetworkRuleSet();
            try
            {
                acl.Bypass = (PSNetWorkRuleBypassEnum)CommandAgent.Output[0]["Bypass"];
                acl.DefaultAction = (PSNetWorkRuleDefaultActionEnum)CommandAgent.Output[0]["DefaultAction"];
                acl.IpRules = CommandAgent.Output[0]["IpRules"] as PSIpRule[];
                acl.VirtualNetworkRules = CommandAgent.Output[0]["VirtualNetworkRules"] as PSVirtualNetworkRule[];
            }
            catch (Exception e)
            {
                Test.Warn("Get ACL from output failed: " + e.Message);
            }
            return acl;
        }


        // Get NetworkACL IPRule objects from PowerShell run result
        private PSIpRule[] GetIPRuleFromOutput()
        {
            try
            {
                return CommandAgent.Output[0]["_baseObject"] as PSIpRule[];
            }
            catch (Exception e)
            {
                Test.Warn("Get IpRule from output failed: " + e.Message);
            }
            return null;
        }


        // Get NetworkACL NetworkRule objects from PowerShell run result
        private PSVirtualNetworkRule[] GetNetworkRuleFromOutput()
        {
            try
            {
                return CommandAgent.Output[0]["_baseObject"] as PSVirtualNetworkRule[];
            }
            catch (Exception e)
            {
                Test.Warn("Get VirtualNetworkRule from output failed: " + e.Message);
            }
            return null;
        }


        // Run Get-AzureRMStorageAccountNetworkAclRule and return result
        private PSNetworkRuleSet GetNetworkAclFromServer(string accountNameToGet = null)
        {
            if (accountNameToGet == null)
                accountNameToGet = accountName;
            Test.Assert(CommandAgent.GetSRPAzureStorageAccountNetworkAcl(resourceGroupName, accountNameToGet),
                       string.Format("Get NetworkAcl of storage account {0} in the resource group {1} should success.", accountNameToGet, resourceGroupName));

            return GetNetworkAclFromOutput();
        }

        //Random Get NetworkACL from last cmdlet output or from server by run cmdlet Get-AzureRMStorageAccountNetworkAclRule
        private PSNetworkRuleSet GetNetworkAcl()
        {
            if (random.Next() % 2 == 0)
                return GetNetworkAclFromServer();
            else
                return GetNetworkAclFromOutput();
        }

        private PSIpRule[] GetIpRule()
        {
            if (random.Next() % 2 == 0)
                return GetNetworkAclFromServer().IpRules;
            else
                return GetIPRuleFromOutput();
        }

        private PSVirtualNetworkRule[] GetNetworkRule()
        {
            if (random.Next() % 2 == 0)
                return GetNetworkAclFromServer().VirtualNetworkRules;
            else
                return GetNetworkRuleFromOutput();
        }

        // Run Update-AzureRMStorageAccountNetworkAcl and check result
        private PSNetworkRuleSet UpdatetNetworkACLCheckResult(
            PSNetWorkRuleBypassEnum? bypass = null,
            PSNetWorkRuleDefaultActionEnum? defaultAction = null,
            PSIpRule[] ipRules = null,
            PSVirtualNetworkRule[] networkRules = null,
            PSNetworkRuleSet originalAcl = null)
        {

            if (CommandAgent.UpdateSRPAzureStorageAccountNetworkAcl(resourceGroupName, accountName, bypass, defaultAction, ipRules, networkRules))
            {
                Test.Assert(true, string.Format("Update NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
            }
            else
            {
                ExpectedContainErrorMessage(string.Format("An operation is currently performing on this storage account that requires exclusive access."));
                ReGenerateAccount(originalAcl);

                Test.Assert(CommandAgent.UpdateSRPAzureStorageAccountNetworkAcl(resourceGroupName, accountName, bypass, defaultAction, ipRules, networkRules),
                           string.Format("Update NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
            }

            return ValidateNetworkACL(GetNetworkAcl(), bypass, defaultAction, ipRules, networkRules, originalAcl);
        }

        // Run add-AzureRMStorageAccountNetworkAclRule and check result
        private PSNetworkRuleSet AddNetworkACLRulesCheckResult(
            string[] ipAddressRange = null,
            string[] virtualNetworkResourceId = null,
            PSIpRule[] ipRules = null,
            PSVirtualNetworkRule[] networkRules = null,
            PSNetworkRuleSet originalAcl = null)
        {
            if (ipAddressRange != null)
            {
                Test.Assert(CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, ipAddressRange, isIPRule: true),
                           string.Format("Add IPRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                ValidateIpRules(GetNewIpRules(GetIpRules(ipAddressRange), originalAcl), GetIpRule());
            }
            if (virtualNetworkResourceId != null)
            {
                Test.Assert(CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, virtualNetworkResourceId, isIPRule: false),
                           string.Format("Add NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                ValidateNetworkRules(GetNewNetworkRules(GetNetworkRules(virtualNetworkResourceId), originalAcl), GetNetworkRule());
            }
            if (ipRules != null)
            {
                Test.Assert(CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, ipRules),
                           string.Format("Add IPRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                ValidateIpRules(GetNewIpRules(ipRules, originalAcl), GetIpRule());
            }
            if (networkRules != null)
            {
                Test.Assert(CommandAgent.AddSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, networkRules),
                           string.Format("Add NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                ValidateNetworkRules(GetNewNetworkRules(networkRules, originalAcl), GetNetworkRule());
            }
            return GetNetworkAclFromServer();
        }

        //Set the netwrokACL to default value
        private PSNetworkRuleSet ResetNetworkACLCheckResult()
        {
            return UpdatetNetworkACLCheckResult(PSNetWorkRuleBypassEnum.AzureServices, PSNetWorkRuleDefaultActionEnum.Allow, new PSIpRule[] { }, new PSVirtualNetworkRule[] { });
        }

        // Run Remove-AzureRMStorageAccountNetworkAclRule and check result
        private PSNetworkRuleSet RemoveNetworkACLRulesCheckResult(
            string[] ipAddressRange = null,
            string[] virtualNetworkResourceId = null,
            PSIpRule[] ipRules = null,
            PSVirtualNetworkRule[] networkRules = null,
            PSNetworkRuleSet originalAcl = null)
        {
            if (ipAddressRange != null)
            {
                Test.Assert(CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, ipAddressRange, isIPRule: true),
                           string.Format("Add IPRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                ValidateIpRules(GetNewIpRules(GetIpRules(ipAddressRange), originalAcl, isAddRule: false), GetIpRule());
            }
            if (virtualNetworkResourceId != null)
            {
                Test.Assert(CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, virtualNetworkResourceId, isIPRule: false),
                           string.Format("Add NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                ValidateNetworkRules(GetNewNetworkRules(GetNetworkRules(virtualNetworkResourceId), originalAcl, isAddRule: false), GetNetworkRule());
            }
            if (ipRules != null)
            {
                Test.Assert(CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, ipRules),
                           string.Format("Add IPRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                ValidateIpRules(GetNewIpRules(ipRules, originalAcl, isAddRule: false), GetIpRule());
            }
            if (networkRules != null)
            {
                Test.Assert(CommandAgent.RemoveSRPAzureStorageAccountNetworkAclRule(resourceGroupName, accountName, networkRules),
                           string.Format("Add NetworkRule to NetworkAcl of storage account {0} in the resource group {1} should success.", accountName, resourceGroupName));
                ValidateNetworkRules(GetNewNetworkRules(networkRules, originalAcl, isAddRule: false), GetNetworkRule());
            }
            return GetNetworkAclFromServer();
        }

        //Get new IPRule Array after add/remove rules
        private PSIpRule[] GetNewIpRules(PSIpRule[] IpRules,
            PSNetworkRuleSet originalAcl = null,
            bool isAddRule = true)
        {
            if (originalAcl == null || originalAcl.IpRules == null || originalAcl.IpRules.Length == 0)
                return IpRules;

            List<PSIpRule> rules = new List<PSIpRule>(originalAcl.IpRules);
            if (isAddRule)
                rules.AddRange(IpRules);
            else
            {
                foreach (PSIpRule ruleToRemove in IpRules)
                {
                    foreach (PSIpRule ruleToCheck in rules)
                    {
                        if (ruleToRemove.IPAddressOrRange == ruleToCheck.IPAddressOrRange)
                        {
                            rules.Remove(ruleToCheck);
                            break;
                        }
                    }
                }
            }
            return rules.ToArray();
        }

        //Get new NetworkRule Array after add/remove rules
        private PSVirtualNetworkRule[] GetNewNetworkRules(PSVirtualNetworkRule[] networkRules,
            PSNetworkRuleSet originalAcl = null,
            bool isAddRule = true)
        {
            if (originalAcl == null || originalAcl.VirtualNetworkRules == null || originalAcl.VirtualNetworkRules.Length == 0)
                return networkRules;

            List<PSVirtualNetworkRule> rules = new List<PSVirtualNetworkRule>(originalAcl.VirtualNetworkRules);
            if (isAddRule)
                rules.AddRange(networkRules);
            else
            {
                foreach (PSVirtualNetworkRule ruleToRemove in networkRules)
                {
                    foreach (PSVirtualNetworkRule ruleToCheck in rules)
                    {
                        if (ruleToRemove.VirtualNetworkResourceId == ruleToCheck.VirtualNetworkResourceId)
                        {
                            rules.Remove(ruleToCheck);
                            break;
                        }
                    }
                }
            }
            return rules.ToArray();
        }

        // Create IPRule object array from the ipAddressRange string array
        private PSIpRule[] GetIpRules(string[] ipAddressRange)
        {
            if (ipAddressRange == null)
                return null;

            List<PSIpRule> rules = new List<PSIpRule>();

            foreach (string s in ipAddressRange)
                rules.Add(new PSIpRule() { IPAddressOrRange = s, Action = PSNetworkRuleActionEnum.Allow });

            return rules.ToArray();
        }

        // Create NetworkRule object array from the virtualNetworkResourceId string array
        private PSVirtualNetworkRule[] GetNetworkRules(string[] virtualNetworkResourceId)
        {
            if (virtualNetworkResourceId == null)
                return null;

            List<PSVirtualNetworkRule> rules = new List<PSVirtualNetworkRule>();

            foreach (string s in virtualNetworkResourceId)
                rules.Add(new PSVirtualNetworkRule() { VirtualNetworkResourceId = s, Action = PSNetworkRuleActionEnum.Allow });

            return rules.ToArray();
        }
        
        private PSNetworkRuleSet ValidateNetworkACL(PSNetworkRuleSet aclToValidate,
            PSNetWorkRuleBypassEnum? bypass = null,
            PSNetWorkRuleDefaultActionEnum? defaultAction = null,
            PSIpRule[] ipRules = null,
            PSVirtualNetworkRule[] networkRules = null,
            PSNetworkRuleSet originalAcl = null)
        {
            //Validate bypass
            if (bypass != null)
            {
                Test.Assert(bypass == aclToValidate.Bypass, string.Format("Expected Bypass is {0} and actually it is {1}", bypass.Value, aclToValidate.Bypass));
            }
            else if (originalAcl != null)
            {
                Test.Assert(originalAcl.Bypass == aclToValidate.Bypass, string.Format("Expected Bypass is {0} and actually it is {1}", originalAcl.Bypass.Value, aclToValidate.Bypass));
            }

            //Validate DefaultAction
            if (defaultAction != null)
            {
                Test.Assert(defaultAction == aclToValidate.DefaultAction, string.Format("Expected DefaultAction is {0} and actually it is {1}", defaultAction.Value, aclToValidate.DefaultAction));
            }
            else if (originalAcl != null)
            {
                Test.Assert(originalAcl.DefaultAction == aclToValidate.DefaultAction, string.Format("Expected DefaultAction is {0} and actually it is {1}", originalAcl.DefaultAction, aclToValidate.Bypass));
            }

            //Validate IPRule
            if (ipRules != null)
            {
                ValidateIpRules(ipRules, aclToValidate.IpRules);
            }
            else if (originalAcl != null)
            {
                ValidateIpRules(originalAcl.IpRules, aclToValidate.IpRules);
            }

            //Validate NetworkRule
            if (networkRules != null)
            {
                ValidateNetworkRules(networkRules, aclToValidate.VirtualNetworkRules);
            }
            else if (originalAcl != null)
            {
                ValidateNetworkRules(originalAcl.VirtualNetworkRules, aclToValidate.VirtualNetworkRules);
            }
            return aclToValidate;

        }

        private void ValidateIpRules(PSIpRule[] ruleExpected, PSIpRule[] ruleToValidate)
        {
            if ((ruleExpected == null || ruleExpected.Length == 0) && (ruleToValidate == null || ruleToValidate.Length == 0))
            {
                Test.Assert(true, "Both IPRule list are empty");
                return;
            }
            Test.Assert(ruleExpected.Length == ruleToValidate.Length, string.Format("IP Rule count: {0} = {1} ", ruleExpected.Length, ruleToValidate.Length));
            for (int i = 0; i < (ruleExpected.Length <= ruleToValidate.Length ? ruleExpected.Length : ruleToValidate.Length); i++)
            {
                Test.Assert(ruleExpected[i].IPAddressOrRange == ruleToValidate[i].IPAddressOrRange, string.Format("Rule {0} IpAddressOrRange: {1} = {2} ", i, ruleExpected[i].IPAddressOrRange, ruleToValidate[i].IPAddressOrRange));
                Test.Assert(ruleExpected[i].Action == ruleToValidate[i].Action, string.Format("Rule {0} Action: {1} = {2} ", i, ruleExpected[i].Action, ruleToValidate[i].Action));
            }
        }

        private void ValidateNetworkRules(PSVirtualNetworkRule[] ruleExpected, PSVirtualNetworkRule[] ruleToValidate)
        {
            if ((ruleExpected == null || ruleExpected.Length == 0) && (ruleToValidate == null || ruleToValidate.Length == 0))
            {
                Test.Assert(true, "Both VirtualNetworkRule list are empty");
                return;
            }
            Test.Assert(ruleExpected.Length == ruleToValidate.Length, string.Format("Network Rule count: {0} = {1} ", ruleExpected.Length, ruleToValidate.Length));
            for (int i = 0; i < (ruleExpected.Length <= ruleToValidate.Length ? ruleExpected.Length : ruleToValidate.Length); i++)
            {
                Test.Assert(ruleExpected[i].VirtualNetworkResourceId == ruleToValidate[i].VirtualNetworkResourceId, string.Format("Rule {0} VirtualNetworkResourceId: {1} = {2} ", i, ruleExpected[i].VirtualNetworkResourceId, ruleToValidate[i].VirtualNetworkResourceId));
                Test.Assert(ruleExpected[i].Action == ruleToValidate[i].Action, string.Format("Rule {0} Action: {1} = {2} ", i, ruleExpected[i].Action, ruleToValidate[i].Action));
            }
        }

        //Delete the old account and generate a new account for testing
        private void ReGenerateAccount(PSNetworkRuleSet acl = null)
        {

            if (isResourceMode)
            {
                try
                {
                    accountUtils.SRPStorageClient.StorageAccounts.DeleteAsync(resourceGroupName, accountName, CancellationToken.None).Wait();
                }
                catch (Exception e)
                {
                    Test.Warn(string.Format("Remove Account with \" -ResourceGroupName {0} -Name {1} \" fail since: {2}", resourceGroupName, accountName, e.Message));
                }

                accountName = accountUtils.GenerateAccountName();

                var parameters = new SRPModel.StorageAccountCreateParameters(new SRPModel.Sku(SRPModel.SkuName.StandardGRS), SRPModel.Kind.Storage,
                    isMooncake ? Constants.MCLocation.ChinaEast : allowedLocation);
                accountUtils.SRPStorageClient.StorageAccounts.CreateAsync(resourceGroupName, accountName, parameters, CancellationToken.None).Wait();

                if (acl != null)
                {
                    UpdatetNetworkACLCheckResult(acl.Bypass, acl.DefaultAction, acl.IpRules, acl.VirtualNetworkRules);
                }
            }
        }
#endregion
    }
}

