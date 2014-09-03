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

namespace Management.Storage.ScenarioTest.BVT.HTTP
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using HTTPSEnvConnectionStringBVT = Management.Storage.ScenarioTest.BVT.HTTPS.EnvConnectionStringBVT;

    /// <summary>
    /// bvt tests using  environment variable "AZURE_STORAGE_CONNECTION_STRING"
    /// </summary>
    [TestClass]
    public class EnvConnectionStringBVT : HTTPSEnvConnectionStringBVT
    {
        [ClassInitialize()]
        public static void EnvConnectionStringHTTPBVTClassInitialize(TestContext testContext)
        {
            useHttps = false;
            HTTPSEnvConnectionStringBVT.Initialize(testContext, useHttps);
        }

        [ClassCleanup()]
        public static void EnvConnectionStringHTTPBVTCleanUp()
        {
            HTTPSEnvConnectionStringBVT.EnvConnectionStringBVTCleanUp();
        }
    }
}
