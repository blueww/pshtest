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
    using System;
    using Management.Storage.ScenarioTest.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MS.Test.Common.MsTestLib;
    using Newtonsoft.Json;

    /// <summary>
    /// this class contains all the account parameter settings for Node.js commands
    /// </summary>
    [TestClass]
    public class StorageAccount : TestBase
    {
        #region Additional test attributes

        [ClassInitialize()]
        public static void StorageAccountTestInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
            NodeJSAgent.AgentConfig.UseEnvVar = false;
        }

        [ClassCleanup()]
        public static void StorageAccountTestCleanup()
        {
            TestBase.TestClassCleanup();
        }

        public override void OnTestSetup()
        {
            if (!accountImported)
            {
                NodeJSAgent nodeAgent = (NodeJSAgent)agent;
                nodeAgent.ImportAzureSubscription();

                string subscriptionID = Test.Data.Get("AzureSubscriptionID");
                if (!string.IsNullOrEmpty(subscriptionID))
                {
                    nodeAgent.SetActiveSubscription(subscriptionID);
                }
                else
                {
                    string subscriptionName = Test.Data.Get("AzureSubscriptionName");
                    if (!string.IsNullOrEmpty(subscriptionName))
                    {
                        nodeAgent.SetActiveSubscription(subscriptionName);
                    }
                }

                accountImported = true;
            }
        }

        private bool accountImported = false;

        #endregion

        /// <summary>
        /// Sprint 35 Test Spec: 1.1; 1.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount001_ConnectionStringShowHelp()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

            // Act
            try
            {
                nodeAgent.ShowAzureStorageAccountConnectionString("-h");
            }
            catch (JsonReaderException)
            {
                string output = nodeAgent.Output[0]["output"] as string;
                if (!string.IsNullOrWhiteSpace(output))
                {
                    result = output.Contains("storage account connectionstring show [options] <name>");
                }
            }

            // Assert
            Test.Assert(result, Utility.GenComparisonData("azure storage account connectionstring show -h", true));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.1.1
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount002_ConnectionStringShow_NoSubscriptionID()
        {
            // Arrange
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");

            // Act
            bool result = nodeAgent.ShowAzureStorageAccountConnectionString(account);

            // Assert
            Test.Assert(result, Utility.GenComparisonData("azure storage account connectionstring show", true));

            string expect = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", account, Test.Data.Get("StorageAccountKey"));
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.1.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount003_ConnectionStringShow_EmptySubscriptionID()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");

            // Act
            // TODO: Investigate sometimes the result is true and the error message is only "\n".
            string argument = string.Format("{0} -s", account);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument) && (nodeAgent.Output.Count != 0);

            // Assert
            Test.Assert(!result, Utility.GenComparisonData("azure storage account connectionstring show -s", false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.1.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount004_ConnectionStringShow_WithSubscriptionID()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");
            string subscription = Test.Data.Get("AzureSubscriptionID");

            // Act
            string argument = string.Format("{0} -s {1}", account, subscription);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", account, Test.Data.Get("StorageAccountKey"));
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.1.4
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount005_ConnectionStringShow_WithSubscriptionName()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");
            string subscription = Test.Data.Get("AzureSubscriptionName");

            // Act
            string argument = string.Format("{0} -s \"{1}\"", account, subscription);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", account, Test.Data.Get("StorageAccountKey"));
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.1.5; 2.1.6
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount006_ConnectionStringShow_InvalidSubscriptionIDOrName()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");
            string subscription = Guid.NewGuid().ToString();

            // Act
            string argument = string.Format("{0} -s \"{1}\"", account, subscription);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument);

            // Assert
            Test.Assert(nodeAgent.ErrorMessages[0].Contains("was not found"), "The subscription should not be found.");
            Test.Assert(!result, Utility.GenComparisonData("azure storage account connectionstring show -s", false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.2.1; 2.5.3 
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount007_ConnectionStringShow_UseHttp()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");

            // Act
            string argument = string.Format("{0} --use-http", account);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=http;AccountName={0};AccountKey={1}", account, Test.Data.Get("StorageAccountKey"));
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.2.2, 2.5.3; 2.6.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount008_ConnectionStringShow_UseHttps()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");

            // Act
            string argument = string.Format("{0} --blob-endpoint http://myBlobEndpoint.core.windows.net", account);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=https;BlobEndpoint=http://myBlobEndpoint.core.windows.net;AccountName={0};AccountKey={1}", account, Test.Data.Get("StorageAccountKey"));
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount009_ConnectionStringShow_BlobEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.Blob, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.11; 2.4.12
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount010_ConnectionStringShow_BlobEndpoint_Invalid()
        {
            this.ErrorEndpoint(ServiceType.Blob, ErrorType.Invalid);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount011_ConnectionStringShow_BlobEndpoint_URL_Protocol()
        {
            string endpoint = "https://myBlobEndpoint.core.windows.net";
            this.ShowWithBlobEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.4
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount012_ConnectionStringShow_BlobEndpoint_IP_Protocol()
        {
            string endpoint = "https://10.0.0.172";
            this.ShowWithBlobEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.5
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount013_ConnectionStringShow_BlobEndpoint_URL_NoProtocol()
        {
            string endpoint = "myBlobEndpoint.core.windows.net";
            this.ShowWithBlobEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.6
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount014_ConnectionStringShow_BlobEndpoint_IP_NoProtocol()
        {
            string endpoint = "10.0.0.172";
            this.ShowWithBlobEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount015_ConnectionStringShow_QueueEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.Queue, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.9; 2.4.10
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount016_ConnectionStringShow_QueueEndpoint_Invalid()
        {
            this.ErrorEndpoint(ServiceType.Queue, ErrorType.Invalid);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount017_ConnectionStringShow_QueueEndpoint_URL_Protocol()
        {
            string endpoint = "https://myQueueEndpoint.core.windows.net";
            this.ShowWithQueueEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.4
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount018_ConnectionStringShow_QueueEndpoint_IP_Protocol()
        {
            string endpoint = "https://10.0.0.172";
            this.ShowWithQueueEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.5
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount019_ConnectionStringShow_QueueEndpoint_URL_NoProtocol()
        {
            string endpoint = "myQueueEndpoint.core.windows.net";
            this.ShowWithQueueEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.6
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount020_ConnectionStringShow_QueueEndpoint_IP_NoProtocol()
        {
            string endpoint = "10.0.0.172";
            this.ShowWithQueueEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount021_ConnectionStringShow_TableEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.Table, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.7; 2.4.8
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount022_ConnectionStringShow_TableEndpoint_Unsupported()
        {
            this.ErrorEndpoint(ServiceType.Table, ErrorType.Unsupported);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount023_ConnectionStringShow_TableEndpoint_URL_Protocol()
        {
            string endpoint = "https://myTableEndpoint.core.windows.net";
            this.ShowWithTableEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.4
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount024_ConnectionStringShow_TableEndpoint_IP_Protocol()
        {
            string endpoint = "https://10.0.0.172";
            this.ShowWithTableEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.5
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount025_ConnectionStringShow_TableEndpoint_URL_NoProtocol()
        {
            string endpoint = "myTableEndpoint.core.windows.net";
            this.ShowWithTableEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.6
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount026_ConnectionStringShow_TableEndpoint_IP_NoProtocol()
        {
            string endpoint = "10.0.0.172";
            this.ShowWithTableEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount027_ConnectionStringShow_FileEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.File, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.9; 2.4.10
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount028_ConnectionStringShow_FileEndpoint_Invalid()
        {
            this.ErrorEndpoint(ServiceType.File, ErrorType.Invalid);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount029_ConnectionStringShow_FileEndpoint_URL_Protocol()
        {
            string endpoint = "https://myFileEndpoint.core.windows.net";
            this.ShowWithFileEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.4
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount030_ConnectionStringShow_FileEndpoint_IP_Protocol()
        {
            string endpoint = "https://10.0.0.172";
            this.ShowWithFileEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.5
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount031_ConnectionStringShow_FileEndpoint_URL_NoProtocol()
        {
            string endpoint = "myFileEndpoint.core.windows.net";
            this.ShowWithFileEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.6
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount032_ConnectionStringShow_FileEndpoint_IP_NoProtocol()
        {
            string endpoint = "10.0.0.172";
            this.ShowWithFileEndpoint(endpoint);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.5.1
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount033_ConnectionStringShow_NoAccount()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

            // Act
            // TODO: Investigate sometimes the result is true and the error message is only "\n".
            result = nodeAgent.ShowAzureStorageAccountConnectionString(string.Empty) && (nodeAgent.Output.Count != 0);

            // Assert
            // TODO: Assert error message:  "error: missing required argument `name'" when the error message issue is resolved.
            Test.Assert(!result, Utility.GenComparisonData("azure storage account connectionstring show", false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.5.4
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount034_ConnectionStringShow_NonExistingAccount()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = "idonotexist";

            // Act
            result = nodeAgent.ShowAzureStorageAccountConnectionString(account);

            // Assert
            Test.Assert(nodeAgent.ErrorMessages[0].Contains(string.Format("The storage account '{0}' was not found.", account)), "The invalid account should be prompted.");
            Test.Assert(!result, Utility.GenComparisonData(string.Format("azure storage account connectionstring show {0}", account), false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.5.5
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount035_ConnectionStringShow_InvalidAccountFormat()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = "invalid-account";

            // Act
            result = nodeAgent.ShowAzureStorageAccountConnectionString(account);

            // Assert
            Test.Assert(nodeAgent.ErrorMessages[0].Contains("The name is not a valid storage account name."), "The invalid account should be prompted.");
            Test.Assert(!result, Utility.GenComparisonData(string.Format("azure storage account connectionstring show {0}", account), false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.6.1
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount036_ConnectionStringShow_MixParams()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");

            // Act
            string argument = string.Format("{0} --use-http --file-endpoint https://myFileEndpoint.chinacloud.api.cn", account);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=http;FileEndpoint=https://myFileEndpoint.chinacloud.api.cn;AccountName={0};AccountKey={1}", account, Test.Data.Get("StorageAccountKey"));
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: Mixture
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSConString)]
        public void FTAccount037_ConnectionStringShow_FullParams()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");
            string argumentTemplate = "{0} --use-http --blob-endpoint {1} --queue-endpoint {2} --table-endpoint {3} --file-endpoint {4}";
            string expectTemplate = "DefaultEndpointsProtocol=http;BlobEndpoint={0};QueueEndpoint={1};TableEndpoint={2};FileEndpoint={3};AccountName={4};AccountKey={5}";
            string blobEndpoint = "10.0.0.1";
            string queueEndpoint = "10.0.0.2";
            string tableEndpoint = "10.0.0.2:8008";
            string fileEndpont = "https://10.0.0.3";

            // Act
            string argument = string.Format(argumentTemplate, account, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpont);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument);

            // Assert
            string expect = string.Format(expectTemplate, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpont, account, Test.Data.Get("StorageAccountKey"));
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        private void ErrorEndpoint(ServiceType type, ErrorType errorType)
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");
            string option = string.Empty;
            switch (type)
            {
                case ServiceType.Blob:
                    option = "--blob-endpoint";
                    break;
                case ServiceType.Queue:
                    option = "--queue-endpoint";
                    break;
                case ServiceType.Table:
                    option = "--table-endpoint";
                    break;
                case ServiceType.File:
                    option = "--file-endpoint";
                    break;
            }

            string endpoint = string.Empty;
            string argument = string.Empty;
            string comparison = string.Empty;
            if (errorType == ErrorType.Empty)
            {
                argument = string.Format("{0} {1}", account, option);
                comparison = string.Format("azure storage account connectionstring show <name> {0}", option);
            }
            else
            {                   
                if (errorType == ErrorType.Unsupported)
                {
                    endpoint = "ftp://endpoint.core.windows.net";
                }
                else
                {
                    Random random = new Random();
                    endpoint = ((random.Next(0, 2) == 1) ? "http://" : "") + ((random.Next(0, 2) == 1) ? "end*?point.core.windows.a" : "10.0.0.1000");
                }

                argument = string.Format("{0} {1} {2}", account, option, endpoint);
                comparison = string.Format("azure storage account connectionstring show {0} {1} {2}", account, option, endpoint);
            }

            // Act
            // TODO: Investigate sometimes the result is true and the error message is only "\n".    
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument) && (nodeAgent.Output.Count != 0);

            // Assert 
            if (errorType == ErrorType.Unsupported)
            {
                Test.Assert(nodeAgent.ErrorMessages[0].Contains(string.Format("The provided URI \"{0}\" is not supported.", endpoint)), "The unsupported URI should be prompted.");
            }
            else if (errorType == ErrorType.Invalid)
            {
                Test.Assert(nodeAgent.ErrorMessages[0].Contains(string.Format("The provided URI \"{0}\" is invalid.", endpoint)), "The invalid URI should be prompted.");
            }

            Test.Assert(!result, Utility.GenComparisonData(comparison, false));
        }

        private void InvalidEndpoit(ServiceType type)
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");
            string endpoint = "a.a";

            // Act
            string argument = string.Format("{0} --blob-endpoint {1}", account, endpoint);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument);

            // Assert
            Test.Assert(nodeAgent.ErrorMessages[0].Contains(string.Format("The provided URI \"{0}\" is invalid.", endpoint)), "The invalid URI should be prompted.");
            Test.Assert(!result, Utility.GenComparisonData("azure storage account connectionstring show <name> --blob-endpoint a.a", false));
        }

        private void ShowWithBlobEndpoint(string endpoint)
        {
            this.ShowWithEndpoint(blobEndpoint: endpoint);
        }

        private void ShowWithQueueEndpoint(string endpoint)
        {
            this.ShowWithEndpoint(queueEndpoint: endpoint);
        }

        private void ShowWithTableEndpoint(string endpoint)
        {
            this.ShowWithEndpoint(tableEndpoint: endpoint);
        }

        private void ShowWithFileEndpoint(string endpoint)
        {
            this.ShowWithEndpoint(fileEndpoint: endpoint);
        }

        private void ShowWithEndpoint(string blobEndpoint = null, string queueEndpoint = null, string tableEndpoint = null, string fileEndpoint = null)
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = Test.Data.Get("StorageAccountName");

            string argumentTemplate = "{0}";
            string expectTempalte = "DefaultEndpointsProtocol=https";
            if (!string.IsNullOrEmpty(blobEndpoint))
            {
                argumentTemplate += " --blob-endpoint {1}";
                expectTempalte += ";BlobEndpoint={0}";
            }

            if (!string.IsNullOrEmpty(queueEndpoint))
            {
                argumentTemplate += " --queue-endpoint {2}";
                expectTempalte += ";QueueEndpoint={1}";
            }

            if (!string.IsNullOrEmpty(tableEndpoint))
            {
                argumentTemplate += " --table-endpoint {3}";
                expectTempalte += ";TableEndpoint={2}";
            }

            if (!string.IsNullOrEmpty(fileEndpoint))
            {
                argumentTemplate += " --file-endpoint {4}";
                expectTempalte += ";FileEndpoint={3}";
            }

            expectTempalte += ";AccountName={4};AccountKey={5}";

            // Act
            string argument = string.Format(argumentTemplate, account, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpoint);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument);

            // Assert
            string expect = string.Format(expectTempalte, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpoint, account, Test.Data.Get("StorageAccountKey"));

            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        private enum ServiceType { Blob, Queue, Table, File }

        private enum ErrorType { Empty, Invalid, Unsupported }
    }
}
