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
    using HTTPSNameKeyContextBVT = Management.Storage.ScenarioTest.BVT.HTTPS.NameKeyContextBVT;

    /// <summary>
    /// Bvt test using name and key context in http mode
    /// </summary>
    [TestClass]
    public class NameKeyContextBVT : HTTPSNameKeyContextBVT
    {
        [ClassInitialize()]
        public static void NameKeyContextHTTPBVTClassInitialize(TestContext testContext)
        {
            useHttps = false;
            HTTPSNameKeyContextBVT.Initialize(testContext, useHttps);
        }

        [ClassCleanup()]
        public static void NameKeyContextHTTPBVTCleanup()
        {
            HTTPSNameKeyContextBVT.NameKeyContextBVTCleanup();
        }
    }
}
