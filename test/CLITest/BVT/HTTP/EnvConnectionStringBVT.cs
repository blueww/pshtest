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

using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using MS.Test.Common.MsTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest.BVT.HTTP
{
    /// <summary>
    /// bvt tests using  environment variable "AZURE_STORAGE_CONNECTION_STRING"
    /// </summary>
    [TestClass]
    public class EnvConnectionStringBVT : Management.Storage.ScenarioTest.BVT.HTTPS.EnvConnectionStringBVT
    {
        [ClassInitialize()]
        public static void EnvConnectionStringHTTPBVTClassInitialize(TestContext testContext)
        {
            useHttps = false;
            SetUpStorageAccount = CloudStorageAccount.Parse(Test.Data.Get("StorageConnectionString"));
            CLICommonBVT.CLICommonBVTInitialize(testContext);

            if (lang == Language.PowerShell)
            {
                Environment.SetEnvironmentVariable(EnvKey, SetUpStorageAccount.ToString(true));
            }
            else if (lang == Language.NodeJS)
            {
                switch (NodeJSAgent.AgentOSType)
                {
                    case OSType.Windows:
                        Environment.SetEnvironmentVariable(EnvKey, SetUpStorageAccount.ToString(true));
                        break;
                    case OSType.Linux:
                    case OSType.Mac:
                        NodeJSAgent.AgentConfig.ConnectionString = SetUpStorageAccount.ToString(true);
                        break;
                }

                NodeJSAgent.AgentConfig.UseEnvVar = true;
            }
        }

        [ClassCleanup()]
        public static void EnvConnectionStringHTTPBVTCleanUp()
        {
            CLICommonBVT.CLICommonBVTCleanup();
        }
    }
}
