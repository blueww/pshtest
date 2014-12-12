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
    using HTTPSSecondaryEndPointBVT = Management.Storage.ScenarioTest.BVT.HTTPS.SecondaryEndPointBVT;

    /// <summary>
    /// BVT test case for secondary end point with http protocol
    /// </summary>
    [TestClass]
    public class SecondaryEndPointBVT : HTTPSSecondaryEndPointBVT
    {
        [ClassInitialize()]
        public static void SecondaryEndPointHTTPBVTClassInitialize(TestContext testContext)
        {
            useHttps = false;
            HTTPSSecondaryEndPointBVT.ClassInitialize(testContext, useHttps);
        }

        [ClassCleanup()]
        public static void SecondaryEndPointHTTPBVTCleanup()
        {
            HTTPSSecondaryEndPointBVT.SecondaryEndPointBVTCleanup();
        }
    }
}
