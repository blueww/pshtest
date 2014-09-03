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

    /// <summary>
    /// bvt tests using parameter setting: connection-string
    /// only for Node.js now
    /// </summary>
    [TestClass]
    public class ParamConnectionStringBVT : CLICommonBVT
    {
        [ClassInitialize()]
        public static void ParamConnectionStringBVTClassInitialize(TestContext testContext)
        {
            useHttps = true;
            Initialize(testContext, useHttps);
        }

        [ClassCleanup()]
        public static void ParamConnectionStringBVTCleanUp()
        {
            CLICommonBVT.CLICommonBVTCleanup();
        }

        public static void Initialize(TestContext testContext, bool useHttps)
        {
            SetUpStorageAccount = TestBase.GetCloudStorageAccountFromConfig(useHttps: useHttps);
            CLICommonBVT.CLICommonBVTInitialize(testContext);

            NodeJSAgent.AgentConfig.ConnectionString = SetUpStorageAccount.ToString(true);
            NodeJSAgent.AgentConfig.UseEnvVar = false;
        }
    }
}
