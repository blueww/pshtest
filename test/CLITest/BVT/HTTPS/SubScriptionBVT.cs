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
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
#if !DOTNET5_4
    using Microsoft.Azure.Management.Resources;
#endif
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
        }

        /// <summary>
        /// set up azure subscription
        /// </summary>
        private static void SetupSubscription()
        {
            if (!isResourceMode) //Service Mode
            {
                string subscriptionFile = Test.Data.Get("AzureSubscriptionPath");
                string subscriptionName = Test.Data.Get("AzureSubscriptionName");
                //TODO add tests about invalid storage account name
                string storageAccountName = Test.Data.Get("StorageAccountName");
                PowerShellAgent.ImportAzureSubscriptionAndSetStorageAccount(subscriptionFile, subscriptionName, storageAccountName);
            }
            else
            {
                PowerShellAgent ps = new PowerShellAgent();
                ps.Logout();
                ps.Login();
                string storageAccountName = Test.Data.Get("StorageAccountName");
                string resourceGroupName = Test.Data.Get("StorageAccountResourceGroup");
                ps.SetRmCurrentStorageAccount(storageAccountName, resourceGroupName);
            }
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
    }
}
