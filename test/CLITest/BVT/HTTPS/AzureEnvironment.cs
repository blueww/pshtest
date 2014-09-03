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
    using Management.Storage.ScenarioTest.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MS.Test.Common.MsTestLib;

    [TestClass]
    public class AzureEnvironment : NameKeyContextBVT
    {
        [ClassInitialize()]
        public static void AzureEnvironmentBVTClassInitialize(TestContext testContext)
        {
            useHttps = true;
            ClassInitialize(testContext, useHttps);
        }

        [ClassCleanup()]
        public static void AzureEnvironmentBVTCleanup()
        {
            CLICommonBVT.CLICommonBVTCleanup();
        }

        public static void ClassInitialize(TestContext testContext, bool useHttps)
        {
            //first set the storage account
            //second init common bvt
            //third set storage context in powershell
            isSecondary = true;
            SetUpStorageAccount = TestBase.GetCloudStorageAccountFromConfig("Secondary", useHttps);
            StorageAccountName = SetUpStorageAccount.Credentials.AccountName;
            string StorageEndpoint = Test.Data.Get("SecondaryStorageEndPoint");
            string StorageAccountKey = Test.Data.Get("SecondaryStorageAccountKey");
            CLICommonBVT.CLICommonBVTInitialize(testContext);
            if (lang == Language.PowerShell)
            {
                string azureEnvironmentName = PowerShellAgent.AddRandomAzureEnvironment(StorageEndpoint, "bvt");
                PowerShellAgent.SetStorageContextWithAzureEnvironment(StorageAccountName, StorageAccountKey, useHttps, azureEnvironmentName);
            }
            else
            {
                NodeJSAgent.AgentConfig.UseEnvVar = false;
                NodeJSAgent.AgentConfig.AccountName = StorageAccountName;
                NodeJSAgent.AgentConfig.AccountKey = StorageAccountKey;
            }
        }
    }
}
