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
    using System.Threading;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MS.Test.Common.MsTestLib;
    using SRPModel = Microsoft.Azure.Management.Storage.Models;

    /// <summary>
    /// this class contains all the account parameter settings for Node.js commands
    /// </summary>
    [TestClass]
    public class SRPStorageAccountTest : StorageAccountTest
    {
        #region Additional test attributes

        [ClassInitialize()]
        public static void SRPStorageAccountTestInit(TestContext testContext)
        {
            isResourceMode = true;
            StorageAccountTest.StorageAccountTestInit(testContext);

            resourceLocation = accountUtils.GenerateAccountLocation(Constants.AccountType.Standard_GRS, true);
            resourceManager = new ResourceManagerWrapper();
            resourceGroupName = accountUtils.GenerateResourceGroupName();
            resourceManager.CreateResourceGroup(resourceGroupName, resourceLocation);

            accountNameForConnectionStringTest = accountUtils.GenerateAccountName();

            var parameters = new SRPModel.StorageAccountCreateParameters(SRPModel.AccountType.StandardGRS, Constants.Location.EastAsia);
            accountUtils.SRPStorageClient.StorageAccounts.CreateAsync(resourceGroupName, accountNameForConnectionStringTest, parameters, CancellationToken.None).Wait();
            var keys = accountUtils.SRPStorageClient.StorageAccounts.ListKeysAsync(resourceGroupName, accountNameForConnectionStringTest, CancellationToken.None).Result;
            primaryKeyForConnectionStringTest = keys.StorageAccountKeys.Key1;
        }

        private static ResourceManagerWrapper resourceManager;
        private static string resourceLocation;

        [ClassCleanup()]
        public static void SRPStorageAccountTestCleanup()
        {
            try
            {
                accountUtils.SRPStorageClient.StorageAccounts.DeleteAsync(resourceGroupName, accountNameForConnectionStringTest, CancellationToken.None).Wait();
                resourceManager.DeleteResourceGroup(resourceGroupName);
            }
            catch (Exception ex)
            {
                Test.Info(string.Format("SRP cleanup exception: {0}", ex));
            }

            StorageAccountTest.StorageAccountTestCleanup();
        }

        public override void OnTestSetup()
        {
            if (lang == Language.NodeJS)
            {
                agent.ChangeCLIMode(Constants.Mode.arm);
            }

            if (!isLogin)
            {
                int retry = 0;
                do
                {
                    if (agent.HadErrors)
                    {
                        Thread.Sleep(5000);
                        Test.Info(string.Format("Retry login... Count:{0}", retry));
                    }

                    agent.Logout();
                    agent.Login();
                }
                while (agent.HadErrors && retry++ < 5);

                if (lang == Language.NodeJS)
                {
                    SetActiveSubscription();
                }
                else
                {
                    string subscriptionID = Test.Data.Get("AzureSubscriptionID");
                    agent.SetActiveSubscription(subscriptionID);
                }

                isLogin = true;
            }
        }

        private bool isLogin = false;
        #endregion
    }
}
