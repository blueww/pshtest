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

using System;
using System.Reflection;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;

namespace Management.Storage.ScenarioTest
{
#if !DOTNET5_4
    /// <summary>
    /// this class contains all the account parameter settings related negative functional test cases for Node.js commands
    /// </summary>
    [TestClass]
    public class CLIAccountParamFunc : TestBase
    {
        private static string InvalidAccountName = "invalid";
        private static string StorageAccountKey;
        private static string TempTestFile;

    #region Additional test attributes

        [ClassInitialize()]
        public static void CLIAccountParamFuncClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            // generate a random account key
            byte[] bytes = new byte[100];
            Random rnd = new Random();
            rnd.NextBytes(bytes);

            StorageAccountKey = Convert.ToBase64String(bytes);
            NodeJSAgent.AgentConfig.UseEnvVar = false;

            TempTestFile = FileUtil.GenerateOneTempTestFile();
        }

        [ClassCleanup()]
        public static void CLIAccountParamFuncClassCleanup()
        {
            FileUtil.RemoveFile(TempTestFile);
            TestBase.TestClassCleanup();
        }

    #endregion

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        public void UseInvalidAccount()
        {
            NodeJSAgent.AgentConfig.AccountName = InvalidAccountName;
            NodeJSAgent.AgentConfig.AccountKey = StorageAccountKey;

            StorageTest(MethodBase.GetCurrentMethod().Name);
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        public void MissingAccountName()
        {
            NodeJSAgent.AgentConfig.AccountKey = StorageAccountKey;
            NodeJSAgent.AgentConfig.AccountName = string.Empty;

        StorageTest(MethodBase.GetCurrentMethod().Name);
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        public void OveruseAccountParams()
        {
            NodeJSAgent.AgentConfig.AccountName = InvalidAccountName;
            NodeJSAgent.AgentConfig.AccountKey = StorageAccountKey;
            NodeJSAgent.AgentConfig.ConnectionStr = StorageAccount.ToString(true);

            StorageTest(MethodBase.GetCurrentMethod().Name);
        }

        internal void StorageTest(string caseName)
        {
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;
            string containerName = Utility.GenNameString("astoria-");
            string blobName = Utility.GenNameString("astoria-");

            Test.Assert(!nodeAgent.NewAzureStorageContainer(containerName), Utility.GenComparisonData("NewAzureStorageContainer", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.GetAzureStorageContainer(containerName), Utility.GenComparisonData("GetAzureStorageContainer", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.SetAzureStorageContainerACL(containerName, BlobContainerPublicAccessType.Container),
                Utility.GenComparisonData("SetAzureStorageContainerACL", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.RemoveAzureStorageContainer(containerName), Utility.GenComparisonData("RemoveAzureStorageContainer", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.ShowAzureStorageContainer(containerName), Utility.GenComparisonData("ShowAzureStorageContainer", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.SetAzureStorageBlobContent(TempTestFile, containerName, BlobType.BlockBlob, blobName), 
                Utility.GenComparisonData("SetAzureStorageBlobContent", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.SetAzureStorageBlobContent(TempTestFile, containerName, BlobType.PageBlob, blobName),
                Utility.GenComparisonData("SetAzureStorageBlobContent", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.GetAzureStorageBlobContent(blobName, TempTestFile, containerName),
                Utility.GenComparisonData("GetAzureStorageBlobContent", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.GetAzureStorageBlob(blobName, containerName),
                Utility.GenComparisonData("GetAzureStorageBlob", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.ShowAzureStorageBlob(blobName, containerName),
                Utility.GenComparisonData("ShowAzureStorageBlob", false));
            nodeAgent.ValidateErrorMessage(caseName);

            Test.Assert(!nodeAgent.RemoveAzureStorageBlob(blobName, containerName),
                Utility.GenComparisonData("RemoveAzureStorageBlob", false));
            nodeAgent.ValidateErrorMessage(caseName);
        }
}
#endif
}
