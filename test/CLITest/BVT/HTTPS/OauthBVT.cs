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
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    /// <summary>
    /// bvt tests using connection string
    /// </summary>
    [TestClass]
    public class OAuthBVT : CLICommonBVT
    {
        [ClassInitialize()]
        public static void OAuthBVTClassInitialize(TestContext testContext)
        {
            useHttps = true;
            CLICommonBVT.CLICommonBVTInitialize(testContext);
            SetupOAuth();
        }

        [ClassCleanup()]
        public static void OAuthBVTCleanup()
        {
            CLICommonBVT.CLICommonBVTCleanup();
        }

        /// <summary>
        /// set up azure subscription
        /// </summary>
        private static void SetupOAuth()
        {
            if (isResourceMode) 
            {
                PowerShellAgent ps = new PowerShellAgent();
                ps.Logout();
                ps.Login();
                string storageAccountName = Test.Data.Get("StorageAccountName");
                PowerShellAgent.SetOAuthStorageContext(storageAccountName, useHttps);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.BVT)]
        public void MakeSureBvtUsingOAuthContext()
        {
            string key = System.Environment.GetEnvironmentVariable(EnvKey);
            Test.Assert(string.IsNullOrEmpty(key), string.Format("env connection string {0} should be null or empty", key));
            Test.Assert(PowerShellAgent.Context != null, "PowerShell context should not be null when running bvt against Subscription");

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
