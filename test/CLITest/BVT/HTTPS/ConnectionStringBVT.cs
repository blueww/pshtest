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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    /// <summary>
    /// bvt tests using connection string
    /// </summary>
    [TestClass]
    public class ConnectionStringBVT : CLICommonBVT
    {
        [ClassInitialize()]
        public static void ConnectionStringBVTClassInitialize(TestContext testContext)
        {
            useHttps = true;
            Initialize(testContext, useHttps);
        }

        [ClassCleanup()]
        public static void ConnectionStringBVTCleanup()
        {
            CLICommonBVT.CLICommonBVTCleanup();
        }

        [TestMethod()]
        [TestCategory(Tag.BVT)]
        public void MakeSureBvtUsingConnectionStringContext()
        {
            string key = System.Environment.GetEnvironmentVariable(EnvKey);
            Test.Assert(string.IsNullOrEmpty(key), string.Format("env connection string {0} should be null or empty", key));
            Test.Assert(PowerShellAgent.Context != null, "PowerShell context should be not null when running bvt against connection string");

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

        public static void Initialize(TestContext testContext, bool useHttps)
        {
            //first set the storage account
            //second init common bvt
            //third set storage context in powershell
            string storageAccountName = Test.Data.Get("StorageAccountName");
            string storageAccountKey = Test.Data.Get("StorageAccountKey");
            string storageEndPoint = Test.Data.Get("StorageEndPoint");
            string connectionString = Utility.GenConnectionString(storageAccountName, storageAccountKey, useHttps, storageEndPoint);
            SetUpStorageAccount = CloudStorageAccount.Parse(connectionString);

            CLICommonBVT.CLICommonBVTInitialize(testContext);

            if (lang == Language.PowerShell)
            {
                PowerShellAgent.SetStorageContext(connectionString);
            }
        }
    }
}
