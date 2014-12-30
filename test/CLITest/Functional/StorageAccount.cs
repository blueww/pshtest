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
    using System.Net;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Management;
    using Microsoft.WindowsAzure.Management.Models;
    using Microsoft.WindowsAzure.Management.Storage;
    using Microsoft.WindowsAzure.Management.Storage.Models;
    using MS.Test.Common.MsTestLib;
    using Newtonsoft.Json;

    /// <summary>
    /// this class contains all the account parameter settings for Node.js commands
    /// </summary>
    [TestClass]
    public class StorageAccountTest : TestBase
    {
        #region Additional test attributes

        [ClassInitialize()]
        public static void StorageAccountTestInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
            NodeJSAgent.AgentConfig.UseEnvVar = false;

            string certFile = Test.Data.Get("ManagementCert");
            string certPassword = Test.Data.Get("CertPassword");
            X509Certificate2 cert = new X509Certificate2(certFile, certPassword);
            CertificateCloudCredentials creadetial = new CertificateCloudCredentials(Test.Data.Get("AzureSubscriptionID"), cert);
            storageClient = new StorageManagementClient(creadetial);
            managementClient = new ManagementClient(creadetial);
        }

        [ClassCleanup()]
        public static void StorageAccountTestCleanup()
        {
            TestBase.TestClassCleanup();
        }
                                                         
        private static ManagementClient managementClient;
        private static StorageManagementClient storageClient;

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

        public struct AccountType
        {
            public const string Standard_LRS = "LRS";
            public const string Standard_ZRS = "ZRS";
            public const string Standard_GRS = "GRS";
            public const string Standard_RAGRS = "RAGRS";
            public const string Premium_LRS = "PLRS";
        };

        public struct AccountLocation
        {
            public const string EUROPE_WEST = "West Europe";
            public const string EUROPE_NORTH = "North Europe";
            public const string US_EAST2 = "East US 2";
            public const string US_CENTRAL = "Central US";
            public const string US_SOUTHCENTRAL = "South Central US";
            public const string US_WEST = "West US";
            public const string US_EAST = "East US";
            public const string ASIA_SOUTHEAST = "Southeast Asia";
            public const string ASIA_EAST = "East Asia";
        };

        private bool accountImported = false;

        private Tuple<int, int> validNameRange = new Tuple<int, int>((int)'a', (int)'z');

        #endregion

        /// <summary>
        /// Sprint 35 Test Spec: 1.1; 1.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount009_ConnectionStringShow_BlobEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.Blob, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.11; 2.4.12
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount010_ConnectionStringShow_BlobEndpoint_Invalid()
        {
            this.ErrorEndpoint(ServiceType.Blob, ErrorType.Invalid);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount015_ConnectionStringShow_QueueEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.Queue, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.9; 2.4.10
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount016_ConnectionStringShow_QueueEndpoint_Invalid()
        {
            this.ErrorEndpoint(ServiceType.Queue, ErrorType.Invalid);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount021_ConnectionStringShow_TableEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.Table, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.7; 2.4.8
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount022_ConnectionStringShow_TableEndpoint_Unsupported()
        {
            this.ErrorEndpoint(ServiceType.Table, ErrorType.Unsupported);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount027_ConnectionStringShow_FileEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.File, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.9; 2.4.10
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount028_ConnectionStringShow_FileEndpoint_Invalid()
        {
            this.ErrorEndpoint(ServiceType.File, ErrorType.Invalid);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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
        [TestCategory(CLITag.NodeJSAccount)]
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

        [TestMethod]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount101_CreateAccount_FullParams()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = "StorageAccountLabel";
            string description = "Storage Account Description";
            string location = AccountLocation.ASIA_EAST;
            string affinityGroup = null;
            string accountType = AccountType.Standard_GRS;

            CreateAndValidateAccount(accountName, label, description, location, affinityGroup, accountType);
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount102_CreateAccount_Localtion()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Location Setting";
            string affinityGroup = null;
            string accountType = AccountType.Standard_GRS;

            foreach (FieldInfo info in typeof(AccountLocation).GetFields())
            {
                string location = info.GetRawConstantValue() as string;
                CreateAndValidateAccount(accountName, label, description, location, affinityGroup, accountType);
            }
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount103_CreateAccount_AffinityGroup()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Affinity Group";
            string location = AccountLocation.ASIA_EAST;
            string accountType = AccountType.Standard_GRS;
            string affinityGroup = "TestAffinityGroup";

            try
            {
                AffinityGroupOperationsExtensions.Create(managementClient.AffinityGroups, new AffinityGroupCreateParameters(affinityGroup, "AffinityGroupLabel", location));
                CreateAndValidateAccount(accountName, label, description, null, affinityGroup, accountType);
            }
            finally
            {
                AffinityGroupOperationsExtensions.Delete(managementClient.AffinityGroups, affinityGroup);
            }
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount104_CreateAccount_AccountType()
        {
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Account Type";
            string location = AccountLocation.ASIA_EAST;
            string affinityGroup = null;

            foreach (FieldInfo info in typeof(AccountType).GetFields())
            {
                string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
                string accountType = info.GetRawConstantValue() as string;
                CreateAndValidateAccount(accountName, label, description, location, affinityGroup, accountType);
            }
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount105_CreateAccount_ExistingAccount()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");
            string label = "StorageAccountLabel";
            string description = "Storage Account Negative Case";
            string location = AccountLocation.ASIA_EAST;
            string affinityGroup = null;
            string accountType = AccountType.Standard_GRS;

            try
            {
                Test.Assert(agent.createAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                    string.Format("Creating stoarge account {0} in location {1} should succeed", accountName, accountType));

                StorageAccountGetResponse response = storageClient.StorageAccounts.Get(accountName);
                Test.Assert(response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

                Test.Assert(!agent.createAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                    string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
                ExpectedContainErrorMessage(string.Format("A storage account named '{0}' already exists in the subscription", accountName));
            }
            finally
            {
                DeleteAccount(accountName);
            }
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount106_CreateAccount_DifferentLocation()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Location and Affinity Group";
            string accoutLocation = AccountLocation.ASIA_EAST;
            string groupLocation = AccountLocation.ASIA_SOUTHEAST;
            string accountType = AccountType.Standard_GRS;
            string affinityGroup = "TestAffinityGroupAndLocation";

            try
            {
                AffinityGroupOperationsExtensions.Create(managementClient.AffinityGroups, new AffinityGroupCreateParameters(affinityGroup, "AffinityGroupLabel", groupLocation));
                CreateAndValidateAccount(accountName, label, description, accoutLocation, affinityGroup, accountType);
            }
            finally
            {
                AffinityGroupOperationsExtensions.Delete(managementClient.AffinityGroups, affinityGroup);
            }
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount107_CreateAccount_NonExistingAffinityGroup()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Non-Existing Affinity Group";
            string location = AccountLocation.ASIA_EAST;
            string accountType = AccountType.Standard_GRS;
            string affinityGroup = FileNamingGenerator.GenerateNameFromRange(15, validNameRange);

            Test.Assert(!agent.createAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
            ExpectedContainErrorMessage("The affinity group does not exist");
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount108_CreateAccount_InvalidLocation()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Invalid Location";
            string location = FileNamingGenerator.GenerateNameFromRange(8, validNameRange); ;
            string accountType = AccountType.Standard_GRS;
            string affinityGroup = null;

            Test.Assert(!agent.createAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
            ExpectedContainErrorMessage("The location constraint is not valid");
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount109_CreateAccount_InvalidType()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Invalid Type";
            string location = AccountLocation.ASIA_EAST;
            string accountType = FileNamingGenerator.GenerateNameFromRange(8, validNameRange);
            string affinityGroup = null;

            Test.Assert(!agent.createAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
            ExpectedContainErrorMessage(string.Format("Invalid value: {0}. Options are: LRS,ZRS,GRS,RAGRS,PLRS", accountType));
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount201_SetAccount_ChangeType_Lable_Description()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = FileNamingGenerator.GenerateNameFromRange(15, validNameRange);
            string description = FileNamingGenerator.GenerateNameFromRange(20, validNameRange);
            string originalAccountType = AccountType.Standard_GRS;
            string newAccountType = AccountType.Standard_LRS;

            SetAndValidateAccount(accountName, originalAccountType, label, description, newAccountType);
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount202_SetAccount_NonExisitingAccount()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = FileNamingGenerator.GenerateNameFromRange(15, validNameRange);
            string description = FileNamingGenerator.GenerateNameFromRange(20, validNameRange);
            string accountType = AccountType.Standard_LRS;

            Test.Assert(!agent.setAzureStorageAccount(accountName, label, description, accountType),
                string.Format("Setting non-existing stoarge account {0} should fail", accountName));
            ExpectedContainErrorMessage(string.Format("The storage account '{0}' was not found", accountName));
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount203_SetAccount_NonExistingType()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Non-Existing Type";

            // No need to create a real accout for NodeJS as it won't pass the parameter validation
            string nonExistingType = FileNamingGenerator.GenerateNameFromRange(6, validNameRange);
            Test.Assert(!agent.setAzureStorageAccount(accountName, label, description, nonExistingType),
                string.Format("Setting stoarge account {0} to type {1} should fail", accountName, nonExistingType));
            ExpectedContainErrorMessage(string.Format("Invalid value: {0}. Options are: LRS,GRS,RAGRS", nonExistingType));
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount204_SetAccount_ZRSToOthers()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Set Type";
            string location = AccountLocation.ASIA_EAST;
            string accountType = AccountType.Standard_ZRS;
            string affinityGroup = null;

            CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);

            foreach (FieldInfo info in typeof(AccountType).GetFields())
            {
                string newAccountType = info.GetRawConstantValue() as string;
                Test.Assert(!agent.setAzureStorageAccount(accountName, label, description, newAccountType),
                    string.Format("Setting stoarge account {0} to type {1} should fail", accountName, newAccountType));
                
                if (newAccountType == AccountType.Standard_ZRS || newAccountType == AccountType.Premium_LRS)
                {
                    ExpectedContainErrorMessage(string.Format("Invalid value: {0}. Options are: LRS,GRS,RAGRS", newAccountType));
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("Cannot change storage account type from Standard_ZRS to {0} or vice versa", info.Name));
                }
            }

            DeleteAccount(accountName);
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount205_SetAccount_PLRSToOthers()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Set Type";
            string location = AccountLocation.ASIA_EAST;
            string accountType = AccountType.Premium_LRS;
            string affinityGroup = null;

            CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);

            foreach (FieldInfo info in typeof(AccountType).GetFields())
            {
                string newAccountType = info.GetRawConstantValue() as string;
                Test.Assert(!agent.setAzureStorageAccount(accountName, label, description, newAccountType),
                    string.Format("Setting stoarge account {0} to type {1} should fail", accountName, newAccountType));

                if (newAccountType == AccountType.Standard_ZRS || newAccountType == AccountType.Premium_LRS)
                {
                    ExpectedContainErrorMessage(string.Format("Invalid value: {0}. Options are: LRS,GRS,RAGRS", newAccountType));
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("Cannot change storage account type from Premium_LRS to {0} or vice versa",
                        typeof(AccountType).GetField(newAccountType).GetRawConstantValue()));
                }
            }

            DeleteAccount(accountName);
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSAccount)]
        public void FTAccount206_SetAccount_OthersToZRSOrPLRS()
        {
            string accountName = FileNamingGenerator.GenerateNameFromRange(10, validNameRange);
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Set Type";

            // No need to create a real accout for NodeJS as it won't pass the parameter validation
            string[] newAccountTypes = {AccountType.Standard_ZRS, AccountType.Premium_LRS};
            foreach (string accountType in newAccountTypes)
            {
                Test.Assert(!agent.setAzureStorageAccount(accountName, label, description, accountType),
                    string.Format("Setting stoarge account {0} to type {1} should fail", accountName, accountType));
                ExpectedContainErrorMessage(string.Format("Invalid value: {0}. Options are: LRS,GRS,RAGRS", accountType));
            }
        }

        private void CreateAndValidateAccount(string accountName, string label, string description, string location, string affinityGroup, string accountType, bool? geoReplication = null)
        {
            CreateNewAccount(accountName, label, description, location, affinityGroup, accountType, geoReplication);
            ValidateAccount(accountName, label, description, location, affinityGroup, accountType, geoReplication);
            DeleteAccount(accountName);
        }

        private void SetAndValidateAccount(string accountName, string originalAccountType, string newLabel, string newDescription, string newAccountType, bool? geoReplication = null)
        {
            string location = AccountLocation.ASIA_EAST;
            string affinityGroup = null;
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Setting";

            CreateNewAccount(accountName, label, description, location, affinityGroup, originalAccountType, geoReplication);
            ValidateAccount(accountName, label, description, location, affinityGroup, originalAccountType, geoReplication);
            
            SetAccount(accountName, newLabel, newDescription, newAccountType, geoReplication);
            ValidateAccount(accountName, newLabel, newDescription, null, null, newAccountType, geoReplication);

            DeleteAccount(accountName);
        }

        private void CreateNewAccount(string accountName, string label, string description, string location, string affinityGroup, string accountType, bool? geoReplication = null)
        {    
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");

            StorageAccountGetResponse response;
            try
            {
                response = storageClient.StorageAccounts.Get(accountName);
            }
            catch (CloudException ex)
            {
                Test.Assert(ex.ErrorCode.Equals("ResourceNotFound"), string.Format("Account {0} should not exist. Exception: {1}", accountName, ex));
            }

            Test.Assert(agent.createAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType, geoReplication),
                string.Format("Creating stoarge account {0} in location {1} should succeed", accountName, location));
        }

        private void SetAccount(string accountName, string newLabel, string newDescription, string newAccountType, bool? geoReplication = null)
        {
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");

            StorageAccountGetResponse response = storageClient.StorageAccounts.Get(accountName);
            Test.Assert(response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            Test.Assert(agent.setAzureStorageAccount(accountName, newLabel, newDescription, newAccountType, geoReplication),
                string.Format("Creating stoarge account {0} with type {1} should succeed", accountName, newAccountType));
        }

        private void ValidateAccount(string accountName, string label, string description, string location, string affinityGroup, string accountType, bool? geoReplication = null)
        {
            StorageAccountGetResponse response = storageClient.StorageAccounts.Get(accountName);
            Test.Assert(response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            StorageAccount account = response.StorageAccount;
            Test.Assert(accountName == account.Name, string.Format("Expected account name is {0} and actually it is {1}", accountName, account.Name));
            Test.Assert(label == account.Properties.Label, string.Format("Expected label is {0} and actually it is {1}", label, account.Properties.Label));
            Test.Assert(description == account.Properties.Description, string.Format("Expected description is {0} and actually it is {1}", description, account.Properties.Description));
            Test.Assert(typeof(AccountType).GetField(account.Properties.AccountType).GetRawConstantValue().Equals(accountType), string.Format("Expected account type is {0} and actually it is {1}", accountType, account.Properties.AccountType));

            if (!string.IsNullOrEmpty(location))
            {
                if (string.IsNullOrEmpty(affinityGroup))
                {
                    Test.Assert(location == account.Properties.Location, string.Format("Expected location is {0} and actually it is {1}", location, account.Properties.Location));
                }
                else
                {
                    Test.Assert(null == account.Properties.Location, string.Format("Expected location is null and actually it is {1}", location, account.Properties.Location));
                }
            }

            if (string.IsNullOrEmpty(affinityGroup))
            {
                Test.Assert(affinityGroup == account.Properties.AffinityGroup, string.Format("Expected affinity group is {0} and actually it is {1}", affinityGroup, account.Properties.AffinityGroup));
            }
        }

        private void DeleteAccount(string accountName)
        {
            try
            {
                storageClient.StorageAccounts.DeleteAsync(accountName).Wait();
            }
            catch (Exception ex)
            {
                Test.Info(string.Format("Deleting Account Exception: {0}", ex));
            }
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

