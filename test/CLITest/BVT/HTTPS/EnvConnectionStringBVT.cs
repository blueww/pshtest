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

using StorageTestLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Management.Storage.ScenarioTest.Common;

namespace Management.Storage.ScenarioTest.BVT.HTTPS
{
    /// <summary>
    /// bvt tests using  environment variable "AZURE_STORAGE_CONNECTION_STRING"
    /// </summary>
    [TestClass]
    class EnvConnectionStringBVT : CLICommonBVT
    {
        [ClassInitialize()]
        public static void EnvConnectionStringBVTClassInitialize(TestContext testContext)
        {
            useHttps = true;
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
            Test.Info("set env var {0} = {1}", EnvKey, SetUpStorageAccount.ToString(true));
        }

        [ClassCleanup()]
        public static void EnvConnectionStringBVTCleanUp()
        {
            CLICommonBVT.CLICommonBVTCleanup();
        }

        [TestMethod()]
        [TestCategory(Tag.BVT)]
        public void MakeSureBvtUsingEnvConnectionStringContext()
        {
            string key = System.Environment.GetEnvironmentVariable(EnvKey);
            Test.Assert(!string.IsNullOrEmpty(key), string.Format("env connection string {0} should be not null or empty", key));
            Test.Assert(PowerShellAgent.Context == null, "PowerShell context should be null when running bvt against env connection string");

            //check the container uri is valid for env connection string context
            CloudBlobContainer retrievedContainer = CreateAndPsGetARandomContainer();
            string uri = retrievedContainer.Uri.ToString();
            string uriPrefix = string.Empty;

            //only check the http/https usage
            if (useHttps)
            {
                uriPrefix = "https";
            }
            else
            {
                uriPrefix = "http";
            }

            Test.Assert(uri.ToString().StartsWith(uriPrefix), string.Format("The prefix of container uri should be {0}, actually it's {1}", uriPrefix, uri));
        }
    }
}
