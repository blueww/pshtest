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

namespace Management.Storage.ScenarioTest.BVT.HTTPS
{
    using System.Security.Cryptography.X509Certificates;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.Azure.Management.Storage;
    using Microsoft.Azure.Management.Storage.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    /// <summary>
    /// bvt tests using Azure subscription
    /// </summary>
    [TestClass]
    class SubScriptionBVT : CLICommonBVT
    {
        private static AccountUtils AccountUtils;

        [ClassInitialize()]
        public static void SubScriptionBVTClassInitialize(TestContext testContext)
        {
            useHttps = true;
            //first set the storage account
            //second init common bvt
            //third set storage context in powershell
            SetUpStorageAccount = TestBase.GetCloudStorageAccountFromConfig(useHttps: useHttps);
            CLICommonBVT.CLICommonBVTInitialize(testContext);
            SetupSubscription();

            AccountUtils = new AccountUtils();
        }

        /// <summary>
        /// set up azure subscription
        /// </summary>
        private static void SetupSubscription()
        {
            string subscriptionFile = Test.Data.Get("AzureSubscriptionPath");
            string subscriptionName = Test.Data.Get("AzureSubscriptionName");
            //TODO add tests about invalid storage account name
            string storageAccountName = Test.Data.Get("StorageAccountName");
            PowerShellAgent.ImportAzureSubscriptionAndSetStorageAccount(subscriptionFile, subscriptionName, storageAccountName);
        }

        [ClassCleanup()]
        public static void SubScriptionBVTCleanUp()
        {
            CLICommonBVT.CLICommonBVTCleanup();
        }

        [TestMethod()]
        [TestCategory(Tag.BVT)]
        public void MakeSureBvtUsingSubscriptionContext()
        {
            string key = System.Environment.GetEnvironmentVariable(EnvKey);
            Test.Assert(string.IsNullOrEmpty(key), string.Format("env connection string {0} should be null or empty", key));
            Test.Assert(PowerShellAgent.Context == null, "PowerShell context should be null when running bvt against Subscription");
        }

        [TestMethod()]
        [TestCategory(Tag.BVT)]
        public void CreateSRPAccount()
        {
            string resourceGroupName = AccountUtils.GenerateResourceGroupName();
            string accountName = AccountUtils.GenerateAccountName();
            string location = Constants.Locations[random.Next(0, Constants.Locations.Length)];

            try
            {
                Test.Assert(agent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, Constants.AccountType.Standard_LRS, location),
                    "Create account {0} of resource group {1} should succeeded.", accountName, resourceGroupName);

                StorageAccountGetPropertiesResponse properties = AccountUtils.SRPStorageClient.StorageAccounts.GetProperties(resourceGroupName, accountName);
                Test.Assert(properties.StorageAccount.AccountType.Value == AccountType.StandardLRS, "Account type should be StandardLRS");
                Test.Assert(properties.StorageAccount.Location == location, "Account type should be StandardLRS");
            }
            finally
            {
                AccountUtils.SRPStorageClient.StorageAccounts.Delete(resourceGroupName, accountName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.BVT)]
        public void SetSRPAccount()
        {
            string resourceGroupName = AccountUtils.GenerateResourceGroupName();
            string accountName = AccountUtils.GenerateAccountName();
            string location = Constants.Locations[random.Next(0, Constants.Locations.Length)];

            try
            {
                AccountUtils.SRPStorageClient.StorageAccounts.Create(resourceGroupName, accountName, new StorageAccountCreateParameters()
                    {
                        AccountType = AccountType.StandardLRS,
                        Location = location
                    });

                Test.Assert(agent.SetSRPAzureStorageAccount(resourceGroupName, accountName, Constants.AccountType.Standard_GRS),
                    "Set account {0} of resource group {1} should succeeded.", accountName, resourceGroupName);

                //TODO Validate
            }
            finally
            {
                AccountUtils.SRPStorageClient.StorageAccounts.Delete(resourceGroupName, accountName);
            }
        }
    }
}
