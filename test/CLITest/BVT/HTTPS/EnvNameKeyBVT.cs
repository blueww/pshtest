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

using System;
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS.Test.Common.MsTestLib;

namespace Management.Storage.ScenarioTest.BVT.HTTPS
{
    /// <summary>
    /// bvt tests using environment variables: AZURE_STORAGE_ACCOUNT, AZURE_STORAGE_ACCESS_KEY
    /// only for Node.js commands
    /// </summary>
    [TestClass]
    class EnvNameKeyBVT : CLICommonBVT
    {
        [ClassInitialize()]
        public static void EnvNameKeyBVTClassInitialize(TestContext testContext)
        {
            useHttps = true;
            //set the storage account
            SetUpStorageAccount = TestBase.GetCloudStorageAccountFromConfig(useHttps: useHttps);

            //second init common bvt            
            CLICommonBVT.CLICommonBVTInitialize(testContext);

            //remove env connection string
            System.Environment.SetEnvironmentVariable(EnvKey, string.Empty);

            //set account name & key
            string StorageAccountName = Test.Data.Get("StorageAccountName");
            string StorageAccountKey = Test.Data.Get("StorageAccountKey");

            if (NodeJSAgent.AgentOSType == OSType.Windows)
            {
                Environment.SetEnvironmentVariable("AZURE_STORAGE_ACCOUNT", StorageAccountName);
                Environment.SetEnvironmentVariable("AZURE_STORAGE_ACCESS_KEY", StorageAccountKey);
            }
            else if (NodeJSAgent.AgentOSType == OSType.Linux || NodeJSAgent.AgentOSType == OSType.Mac)
            {
                NodeJSAgent.AgentConfig.AccountName = StorageAccountName;
                NodeJSAgent.AgentConfig.AccountKey = StorageAccountKey;
            }
            NodeJSAgent.AgentConfig.UseEnvVar = true;
        }

        [ClassCleanup()]
        public static void EnvNameKeyBVTCleanUp()
        {
            CLICommonBVT.CLICommonBVTCleanup();
        }
    }
}
