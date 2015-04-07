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

namespace Management.Storage.ScenarioTest
{
    using System.Security.Cryptography.X509Certificates;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.Azure;
    using Microsoft.Azure.Management.Resources;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MS.Test.Common.MsTestLib;

    /// <summary>
    /// this class contains all the account parameter settings for Node.js commands
    /// </summary>
    [TestClass]
    public class SRPStorageAccountTest : StorageAccountTest
    {
        #region Additional test attributes

        [ClassInitialize()]
        public static void SRPStorageAccountTestInit(TestContext testContext)
        {
            isResourceMode = true;
            StorageAccountTest.TestClassInitialize(testContext);

            resourceLocation = accountUtils.GenerateAccountLocation();
            resourceManager = new ResourceManagerWrapper();
            resourceGroupName = accountUtils.GenerateResourceGroupName();
            resourceManager.CreateResourceGroup(resourceGroupName, resourceLocation);
        }

        private static ResourceManagerWrapper resourceManager;
        private static string resourceLocation;

        [ClassCleanup()]
        public static void SRFPStorageAccountTestCleanup()
        {
            resourceManager.DeleteResourceGroup(resourceGroupName);
            StorageAccountTest.TestClassCleanup();
        }
        #endregion
    }
}
