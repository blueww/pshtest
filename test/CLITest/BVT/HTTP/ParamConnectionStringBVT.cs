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
    using HTTPSParamConnectionStringBVT = Management.Storage.ScenarioTest.BVT.HTTPS.ParamConnectionStringBVT;

    /// <summary>
    /// bvt tests using parameter setting: connection-string
    /// only for Node.js now
    /// </summary>
    [TestClass]
    public class ParamConnectionStringBVT : HTTPSParamConnectionStringBVT
    {
        [ClassInitialize()]
        public static void ParamConnectionStringHTTPBVTClassInitialize(TestContext testContext)
        {
            useHttps = false;
            HTTPSParamConnectionStringBVT.Initialize(testContext, useHttps);
        }

        [ClassCleanup()]
        public static void ParamConnectionStringHTTPBVTCleanUp()
        {
            HTTPSParamConnectionStringBVT.ParamConnectionStringBVTCleanUp();
        }
    }
}
