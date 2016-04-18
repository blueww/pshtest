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
    using System.Collections;
    using System.Collections.Generic;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.Azure.Common.Authentication.Models;
    using Microsoft.Azure.Management.Storage;
    using Microsoft.Azure.Management.Storage.Models;
    using Microsoft.Rest.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Management;
    using Microsoft.WindowsAzure.Management.Models;
    using Microsoft.WindowsAzure.Management.Storage;
    using Microsoft.WindowsAzure.Management.Storage.Models;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using MS.Test.Common.MsTestLib;
    using Newtonsoft.Json.Linq;
    using StorageTestLib;
    using SRPModel = Microsoft.Azure.Management.Storage.Models;

    /// <summary>
    /// this class contains all the account parameter settings for Node.js commands
    /// </summary>
    [TestClass]
    public class StorageAccountTest : TestBase
    {
        private static ManagementClient managementClient;
        protected static AccountUtils accountUtils;
        protected static string resourceGroupName = string.Empty;
        protected static string accountNameForConnectionStringTest;
        protected static string primaryKeyForConnectionStringTest;
        private static ResourceManagerWrapper resourceManager;
        private static string resourceLocation;

        private Tuple<int, int> validNameRange = new Tuple<int, int>((int)'a', (int)'z');

        private List<string> createdAccounts = new List<string>();

        private const string PSHInvalidAccountTypeError =
            "Cannot validate argument on parameter 'SkuName'. The argument \"{0}\" does not belong to the set \"Standard_LRS,Standard_ZRS,Standard_GRS,Standard_RAGRS,Premium_LRS\" specified by the ValidateSet attribute.";

        private const string PSHASMAccountTypeInvalidError = "The AccountType {0} is invalid";

        private const string NodeJSInvalidCreateTypeError = "Invalid value: {0}. Options are: LRS,ZRS,GRS,RAGRS,PLRS";

        private const string NodeJSInvalidSetTypeError = "Invalid value: {0}. Options are: LRS,GRS,RAGRS";

        #region Additional test attributes

        [ClassInitialize()]
        public static void StorageAccountTestInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            NodeJSAgent.AgentConfig.UseEnvVar = false;

            AzureEnvironment environment = Utility.GetTargetEnvironment();
            managementClient = new ManagementClient(Utility.GetCertificateCloudCredential(),
                    environment.GetEndpointAsUri(AzureEnvironment.Endpoint.ServiceManagement));

            accountUtils = new AccountUtils(lang, isResourceMode);

            accountNameForConnectionStringTest = accountUtils.GenerateAccountName();

            if (isResourceMode)
            {
                resourceLocation = isMooncake ? Constants.MCLocation.ChinaEast : Constants.Location.EastAsia;
                resourceManager = new ResourceManagerWrapper();
                resourceGroupName = accountUtils.GenerateResourceGroupName();
                resourceManager.CreateResourceGroup(resourceGroupName, resourceLocation);

                accountNameForConnectionStringTest = accountUtils.GenerateAccountName();

                var parameters = new SRPModel.StorageAccountCreateParameters(new SRPModel.Sku(SRPModel.SkuName.StandardGRS), SRPModel.Kind.Storage,
                    isMooncake ? Constants.MCLocation.ChinaEast : Constants.Location.EastAsia);
                accountUtils.SRPStorageClient.StorageAccounts.CreateAsync(resourceGroupName, accountNameForConnectionStringTest, parameters, CancellationToken.None).Wait();
                var keys = accountUtils.SRPStorageClient.StorageAccounts.ListKeysAsync(resourceGroupName, accountNameForConnectionStringTest, CancellationToken.None).Result;
                primaryKeyForConnectionStringTest = keys.Keys[0].Value;
            }
            else
            {
                var parameters = new Microsoft.WindowsAzure.Management.Storage.Models.StorageAccountCreateParameters();
                parameters.Name = accountNameForConnectionStringTest;
                parameters.Label = "Test account in service mode";
                parameters.AccountType = Constants.AccountType.Standard_GRS;
                parameters.Location = isMooncake ? Constants.MCLocation.ChinaEast : Constants.Location.WestUS;
                accountUtils.StorageClient.StorageAccounts.CreateAsync(parameters).Wait();
                var keys = accountUtils.StorageClient.StorageAccounts.GetKeysAsync(accountNameForConnectionStringTest).Result;
                primaryKeyForConnectionStringTest = keys.PrimaryKey;
            }
        }

        [ClassCleanup()]
        public static void StorageAccountTestCleanup()
        {
            if (isResourceMode)
            {
                try
                {
                    accountUtils.SRPStorageClient.StorageAccounts.DeleteAsync(resourceGroupName, accountNameForConnectionStringTest, CancellationToken.None).Wait();
                    resourceManager.DeleteResourceGroup(resourceGroupName);
                }
                catch (Exception ex)
                {
                    Test.Info(string.Format("SRP cleanup exception: {0}", ex));
                }
            }
            else
            {
                try
                {
                    accountUtils.StorageClient.StorageAccounts.DeleteAsync(accountNameForConnectionStringTest).Wait();
                }
                catch (Exception ex)
                {
                    Test.Info(string.Format("Account cleanup exception: {0}", ex));
                }
            }

            TestBase.TestClassCleanup();
        }

        #endregion

        /// <summary>
        /// Sprint 35 Test Spec: 1.1; 1.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount001_ConnectionStringShowHelp()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;

            // Act
            nodeAgent.ShowAzureStorageAccountConnectionString("-h");
            result = nodeAgent.Output[0].Count > 0;
            foreach (var item in nodeAgent.Output[0])
            {
                if (!item.Key.Contains("help"))
                {
                    result = false;
                    break;
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount002_ConnectionStringShow_NoSubscriptionID()
        {
            // Arrange
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;

            // Act
            bool result = nodeAgent.ShowAzureStorageAccountConnectionString(accountNameForConnectionStringTest, resourceGroupName);

            // Assert
            Test.Assert(result, Utility.GenComparisonData("azure storage account connectionstring show", true));

            string expect = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", accountNameForConnectionStringTest, primaryKeyForConnectionStringTest);
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.1.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        public void FTAccount003_ConnectionStringShow_EmptySubscriptionID()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;

            // Act
            // TODO: Investigate sometimes the result is true and the error message is only "\n".
            string argument = string.Format("{0} -s", accountNameForConnectionStringTest);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName) && (nodeAgent.Output.Count != 0);

            // Assert
            Test.Assert(!result, Utility.GenComparisonData("azure storage account connectionstring show -s", false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.1.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        public void FTAccount004_ConnectionStringShow_WithSubscriptionID()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;
            string subscription = Test.Data.Get("AzureSubscriptionID");

            // Act
            string argument = string.Format("{0} -s {1}", accountNameForConnectionStringTest, subscription);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", accountNameForConnectionStringTest, primaryKeyForConnectionStringTest);
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.1.4
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        public void FTAccount005_ConnectionStringShow_WithSubscriptionName()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;
            string subscription = Test.Data.Get("AzureSubscriptionName");

            // Act
            string argument = string.Format("{0} -s \"{1}\"", accountNameForConnectionStringTest, subscription);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", accountNameForConnectionStringTest, primaryKeyForConnectionStringTest);
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.1.5; 2.1.6
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        public void FTAccount006_ConnectionStringShow_InvalidSubscriptionIDOrName()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;
            string subscription = Guid.NewGuid().ToString();

            // Act
            string argument = string.Format("{0} -s \"{1}\"", accountNameForConnectionStringTest, subscription);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName);

            // Assert
            Test.Assert(nodeAgent.ErrorMessages[0].Contains("was not found"), "The subscription should not be found.");
            Test.Assert(!result, Utility.GenComparisonData("azure storage account connectionstring show -s", false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.2.1; 2.5.3 
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount007_ConnectionStringShow_UseHttp()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;

            // Act
            string argument = string.Format("{0} --use-http", accountNameForConnectionStringTest);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=http;AccountName={0};AccountKey={1}", accountNameForConnectionStringTest, primaryKeyForConnectionStringTest);
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.2.2, 2.5.3; 2.6.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount008_ConnectionStringShow_UseHttps()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;

            // Act
            string argument = string.Format("{0} --blob-endpoint http://myBlobEndpoint.core.windows.net", accountNameForConnectionStringTest);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=https;BlobEndpoint=http://myBlobEndpoint.core.windows.net;AccountName={0};AccountKey={1}",
                accountNameForConnectionStringTest,
                primaryKeyForConnectionStringTest);
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.2
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount009_ConnectionStringShow_BlobEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.Blob, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.11; 2.4.12
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount010_ConnectionStringShow_BlobEndpoint_Invalid()
        {
            this.ErrorEndpoint(ServiceType.Blob, ErrorType.Invalid);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount015_ConnectionStringShow_QueueEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.Queue, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.9; 2.4.10
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount016_ConnectionStringShow_QueueEndpoint_Invalid()
        {
            this.ErrorEndpoint(ServiceType.Queue, ErrorType.Invalid);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount021_ConnectionStringShow_TableEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.Table, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.7; 2.4.8
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount022_ConnectionStringShow_TableEndpoint_Unsupported()
        {
            this.ErrorEndpoint(ServiceType.Table, ErrorType.Unsupported);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount027_ConnectionStringShow_FileEndpoint_Empty()
        {
            this.ErrorEndpoint(ServiceType.File, ErrorType.Empty);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.9; 2.4.10
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount028_ConnectionStringShow_FileEndpoint_Invalid()
        {
            this.ErrorEndpoint(ServiceType.File, ErrorType.Invalid);
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.4.3
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount033_ConnectionStringShow_NoAccount()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;

            // Act
            // TODO: Investigate sometimes the result is true and the error message is only "\n".
            result = nodeAgent.ShowAzureStorageAccountConnectionString(string.Empty, resourceGroupName) && (nodeAgent.Output.Count != 0);

            // Assert
            // TODO: Assert error message:  "error: missing required argument `name'" when the error message issue is resolved.
            Test.Assert(!result, Utility.GenComparisonData("azure storage account connectionstring show", false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.5.4
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount034_ConnectionStringShow_NonExistingAccount()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;
            string account = "idonotexist";

            // Act
            result = nodeAgent.ShowAzureStorageAccountConnectionString(account, resourceGroupName);

            // Assert
            if (isResourceMode)
            {
                ExpectedAccoutNotFoundErrorMessage(resourceGroupName, account);
            }
            else
            {
                Test.Assert(nodeAgent.ErrorMessages[0].Contains(string.Format("The storage account '{0}' was not found.", account)), "The invalid account should be prompted.");
            }

            Test.Assert(!result, Utility.GenComparisonData(string.Format("azure storage account connectionstring show {0}", account), false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.5.5
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount035_ConnectionStringShow_InvalidAccountFormat()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;
            string account = "invalid-account";

            // Act
            result = nodeAgent.ShowAzureStorageAccountConnectionString(account, resourceGroupName);

            // Assert
            if (isResourceMode)
            {
                ExpectedAccoutNotFoundErrorMessage(resourceGroupName, account);
            }
            else
            {
                Test.Assert(nodeAgent.ErrorMessages[0].Contains("The name is not a valid storage account name."), "The invalid account should be prompted.");
            }
            Test.Assert(!result, Utility.GenComparisonData(string.Format("azure storage account connectionstring show {0}", account), false));
        }

        /// <summary>
        /// Sprint 35 Test Spec: 2.6.1
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount036_ConnectionStringShow_MixParams()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;

            // Act
            string argument = string.Format("{0} --use-http --file-endpoint https://myFileEndpoint.chinacloud.api.cn", accountNameForConnectionStringTest);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName);

            // Assert
            string expect = string.Format("DefaultEndpointsProtocol=http;FileEndpoint=https://myFileEndpoint.chinacloud.api.cn;AccountName={0};AccountKey={1}",
                accountNameForConnectionStringTest,
                primaryKeyForConnectionStringTest);
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        /// <summary>
        /// Sprint 35 Test Spec: Mixture
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount037_ConnectionStringShow_FullParams()
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;
            string argumentTemplate = "{0} --use-http --blob-endpoint {1} --queue-endpoint {2} --table-endpoint {3} --file-endpoint {4}";
            string expectTemplate = "DefaultEndpointsProtocol=http;BlobEndpoint={0};QueueEndpoint={1};TableEndpoint={2};FileEndpoint={3};AccountName={4};AccountKey={5}";
            string blobEndpoint = "10.0.0.1";
            string queueEndpoint = "10.0.0.2";
            string tableEndpoint = "10.0.0.2:8008";
            string fileEndpont = "https://10.0.0.3";

            // Act
            string argument = string.Format(argumentTemplate, accountNameForConnectionStringTest, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpont);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName);

            // Assert
            string expect = string.Format(expectTemplate, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpont, accountNameForConnectionStringTest, primaryKeyForConnectionStringTest);
            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount101_CreateAccount_FullParams()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Description";
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
            string affinityGroup = null;
            Hashtable[] tags = new Hashtable[random.Next(1, 5)];

            for (int i = 0; i < tags.Length; ++i)
            {
                tags[i] = new Hashtable();
                tags[i].Add("Name", Utility.GenNameString("Name"));
                tags[i].Add("Value", Utility.GenNameString("Value"));
            }

            CreateAndValidateAccount(accountName, label, description, location, affinityGroup, accountType, tags, null, Kind.BlobStorage, Constants.EncryptionSupportServiceEnum.Blob, AccessTier.Cool);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount102_CreateAccount_Location()
        {
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Location Setting";
            string affinityGroup = null;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);

            string[] locationsArray = isResourceMode ? Constants.SRPLocations : (isMooncake ? Constants.MCLocations : Constants.Locations);

            foreach (var location in locationsArray)
            {
                string accountName = accountUtils.GenerateAccountName();
                CreateAndValidateAccount(accountName, label, description, location, affinityGroup, accountType, null);
            }
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        public void FTAccount103_CreateAccount_AffinityGroup()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Affinity Group";
            string location = isMooncake ? Constants.MCLocation.ChinaEast : Constants.Location.EastAsia;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string affinityGroup = "TestAffinityGroup";

            try
            {
                AffinityGroupOperationsExtensions.Create(managementClient.AffinityGroups, new AffinityGroupCreateParameters(affinityGroup, "AffinityGroupLabel", location));
                CreateAndValidateAccount(accountName, label, description, isResourceMode ? location : null, affinityGroup, accountType, null);
            }
            finally
            {
                AffinityGroupOperationsExtensions.Delete(managementClient.AffinityGroups, affinityGroup);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount104_CreateAccount_AccountType()
        {
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Account Type";
            string affinityGroup = null;

            foreach (var accountType in Constants.AccountTypes)
            {
                if (isResourceMode && accountType.Equals(Constants.AccountType.Premium_LRS))
                {
                    continue;
                }

                if (isMooncake &&
                    (accountType.Equals(Constants.AccountType.Premium_LRS) ||
                     accountType.Equals(Constants.AccountType.Standard_ZRS)))
                {
                    continue;
                }

                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
                CreateAndValidateAccount(accountName, label, description, location, affinityGroup, accountUtils.mapAccountType(accountType), null);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount105_CreateAccount_ExistingAccount()
        {
            string accountName = accountUtils.GenerateAccountName();
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location),
                        string.Format("Creating an storage account {0} in location {1} with the same properties with an existing account should fail.", accountName, location));

                    string newLocation = isMooncake ? Constants.MCLocation.ChinaNorth : Constants.Location.WestUS;
                    Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, newLocation),
                        string.Format("Creating an existing storage account {0} in location {1} should fail", accountName, newLocation));
                    ExpectedContainErrorMessage(string.Format("The storage account named '{0}' is already taken.", accountName));

                    string newType = accountUtils.mapAccountType(Constants.AccountType.Standard_RAGRS); ;
                    Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, newType, location),
                        string.Format("Creating an existing storage account {0} in location {1} should fail", accountName, newLocation));
                    ExpectedContainErrorMessage(string.Format("The storage account named '{0}' is already taken.", accountName));
                }
                else
                {
                    string subscriptionId = Test.Data.Get("AzureSubscriptionID");
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Negative Case";
                    string affinityGroup = null;
                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                    Test.Assert(!CommandAgent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                        string.Format("Creating an existing stoarge account {0} in location {1} should fail", accountName, location));
                    ExpectedContainErrorMessage(string.Format("A storage account named '{0}' already exists in the subscription", accountName));
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount106_CreateAccount_DifferentLocation()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Location and Affinity Group";
            string accoutLocation = isMooncake ? Constants.MCLocation.ChinaEast : Constants.Location.EastAsia;
            string groupLocation = isMooncake ? Constants.MCLocation.ChinaNorth : Constants.Location.SoutheastAsia;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string affinityGroup = "TestAffinityGroupAndLocation";

            try
            {
                AffinityGroupOperationsExtensions.Create(managementClient.AffinityGroups, new AffinityGroupCreateParameters(affinityGroup, "AffinityGroupLabel", groupLocation));
                CreateAndValidateAccount(accountName, label, description, accoutLocation, affinityGroup, accountType, null);
            }
            finally
            {
                AffinityGroupOperationsExtensions.Delete(managementClient.AffinityGroups, affinityGroup);
            }
        }

        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount107_CreateAccount_NonExistingAffinityGroup()
        {
            string accountName = accountUtils.GenerateAccountName();
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Non-Existing Affinity Group";
            string location = isMooncake ? Constants.MCLocation.ChinaEast : Constants.Location.EastAsia;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string affinityGroup = FileNamingGenerator.GenerateNameFromRange(15, validNameRange);

            if (isResourceMode)
            {
                Test.Assert(CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location),
                    string.Format("Creating existing stoarge account {0} in location {1} should succeed", accountName, location));
            }
            else
            {
                Test.Assert(!CommandAgent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                    string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
                ExpectedContainErrorMessage("The affinity group does not exist");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount108_CreateAccount_InvalidLocation()
        {
            string accountName = accountUtils.GenerateAccountName();
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Invalid Location";
            string location = FileNamingGenerator.GenerateNameFromRange(8, validNameRange);
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string affinityGroup = null;

            if (isResourceMode)
            {
                Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location),
                    string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
                ExpectedContainErrorMessage(string.Format("The provided location '{0}' is not available for resource type 'Microsoft.Storage/storageAccounts'.", location));
            }
            else
            {
                Test.Assert(!CommandAgent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                    string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
                ExpectedContainErrorMessage("The location constraint is not valid");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount109_CreateAccount_InvalidType()
        {
            string accountName = accountUtils.GenerateAccountName();
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Invalid Type";
            string accountType = FileNamingGenerator.GenerateNameFromRange(8, validNameRange);
            string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
            string affinityGroup = null;

            string errorMessageFormat = Language.PowerShell == lang ? PSHInvalidAccountTypeError : NodeJSInvalidCreateTypeError;

            if (isResourceMode)
            {
                Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location),
                    string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
            }
            else
            {
                Test.Assert(!CommandAgent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                    string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));

                if (Language.PowerShell == lang)
                {
                    errorMessageFormat = PSHASMAccountTypeInvalidError;
                }
            }

            ExpectedContainErrorMessage(string.Format(errorMessageFormat, accountType));
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount110_CreateAccount_Tags()
        {
            if (isResourceMode)
            {
                string accountType = Constants.AccountType.Standard_GRS;
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);

                Hashtable[] tags = this.GetUnicodeTags();
                CreateAndValidateAccount(accountName, null, null, location, null, accountUtils.mapAccountType(accountType), tags);

                accountName = accountUtils.GenerateAccountName();
                tags = new Hashtable[0];
                CreateAndValidateAccount(accountName, null, null, location, null, accountUtils.mapAccountType(accountType), tags);

                accountName = accountUtils.GenerateAccountName();
                tags = new Hashtable[1];
                tags[0] = new Hashtable();
                tags[0].Add("Name", Utility.GenNameString("Name"));
                tags[0].Add("Value", "");
                CreateAndValidateAccount(accountName, null, null, location, null, accountUtils.mapAccountType(accountType), tags);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount111_CreateAccount_InvalidTags()
        {
            if (isResourceMode)
            {
                string accountType = Constants.AccountType.Standard_GRS;
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);

                Hashtable[] tags = new Hashtable[1];
                tags[0] = new Hashtable();
                tags[0].Add("Name", "");
                tags[0].Add("Value", Utility.GenNameString("Value"));
                CreateAndValidateAccountWithInvalidTags(accountName, location, accountUtils.mapAccountType(accountType), tags);
                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("The tag name must be non-null, non-empty and non-whitespace only. Please provide an actual value.");
                }
                else
                {
                    ExpectedContainErrorMessage("The tag name must be non-null, non-empty and non-whitespace only. Please provide an actual value.");
                }

                accountName = accountUtils.GenerateAccountName();
                tags[0] = new Hashtable();
                tags[0].Add("Name", Utility.GenNameString("Name", random.Next(125, 500)));
                tags[0].Add("Value", Utility.GenNameString("Value"));
                CreateAndValidateAccountWithInvalidTags(accountName, location, accountUtils.mapAccountType(accountType), tags);

                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage("Maximum allowed length of 128 for tag key exceeded.");
                }
                else
                {
                    ExpectedContainErrorMessage("Maximum allowed length of 128 for tag key exceeded.");
                }

                accountName = accountUtils.GenerateAccountName();
                tags[0] = new Hashtable();
                tags[0].Add("Name", Utility.GenNameString("Name"));
                tags[0].Add("Value", Utility.GenNameString("Value", random.Next(253, 500)));
                CreateAndValidateAccountWithInvalidTags(accountName, location, accountUtils.mapAccountType(accountType), tags);

                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage(string.Format("Tag value too large.  Following tag value '{0}' exceeded the maximum length. Maximum allowed length for tag value - '256' characters.",
                        tags[0]["Value"].ToString()));
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("Tag value too large.  Following tag value '{0}' exceeded the maximum length. Maximum allowed length for tag value - '256' characters.",
                        tags[0]["Value"].ToString()));
                }

                accountName = accountUtils.GenerateAccountName();
                tags = new Hashtable[random.Next(16, 50)];
                for (int i = 0; i < tags.Length; ++i)
                {
                    tags[i] = new Hashtable();
                    tags[i].Add("Name", Utility.GenNameString("Name"));
                    tags[i].Add("Value", Utility.GenNameString("Value"));
                }
                CreateAndValidateAccountWithInvalidTags(accountName, location, accountUtils.mapAccountType(accountType), tags);

                if (lang == Language.PowerShell)
                {
                    ExpectedContainErrorMessage(string.Format("Too many tags on the resource/resource group. Requested tag count - '{0}'. Maximum number of tags allowed - '15'.",
                       tags.Length));
                }
                else
                {
                    ExpectedContainErrorMessage(string.Format("Too many tags on the resource/resource group. Requested tag count - '{0}'. Maximum number of tags allowed - '15'.",
                       tags.Length));
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount112_CreateAccount_TagsWordCase()
        {
            if (isResourceMode)
            {
                string accountType = Constants.AccountType.Standard_GRS;
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);

                Hashtable[] tags = this.GetUnicodeTags(true);

                if (Language.PowerShell == lang)
                {
                    Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountUtils.mapAccountType(accountType), location, tags),
                        string.Format("Creating storage account {0} in the resource group {1} at location {2} should fail", accountName, resourceGroupName, location));

                    ExpectedContainErrorMessage("Invalid tag format. Ensure that each tag has a unique name.");
                }
                else
                {
                    Test.Assert(CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountUtils.mapAccountType(accountType), location, tags),
                        string.Format("Creating storage account {0} in the resource group {1} at location {2} should succeeded.", accountName, resourceGroupName, location));

                    Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                    "Get storage account {0} in resource group {1} should succeed.", resourceGroupName, accountName);

                    var targetTags = GetTagsFromOutput();
                    accountUtils.ValidateTags(tags, targetTags);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount113_CreateAccount_BlobStorageAccessTier()
        {
            if (isResourceMode)
            {
                string accountType = Constants.AccountType.Standard_RAGRS;
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
                CreateAndValidateAccount(accountName, null, null, location, null, accountUtils.mapAccountType(accountType), null, kind: Kind.BlobStorage, accessTier: AccessTier.Hot);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount114_CreateAccount_BlobEncryption()
        {
            if (isResourceMode)
            {
                string accountType = Constants.AccountType.Standard_GRS;
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
                CreateAndValidateAccount(accountName, null, null, location, null, accountUtils.mapAccountType(accountType), null, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount115_CreateAccount_InvalidCustomDomain()
        {
            if (isResourceMode)
            {
                string accountType = Constants.AccountType.Standard_LRS;
                string accountName = accountUtils.GenerateAccountName();
                string invalidCustomeDomain = "abc.edf.com";
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
                Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountUtils.mapAccountType(accountType), location, customDomain: invalidCustomeDomain, useSubdomain: (new Random().Next(0, 1) == 0)),
                    string.Format("Creating storage account {0} in the resource group {1}  with customDomain {2} should failed", accountName, resourceGroupName, invalidCustomeDomain));

                if (lang == Language.NodeJS)
                {
                    string error = string.Format("The custom domain name could not be verified. CNAME mapping from {0} to", invalidCustomeDomain);
                    ExpectedContainErrorMessage(error);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount116_CreateAccount_StorageAccessTier()
        {
            if (isResourceMode)
            {
                string accountType = Constants.AccountType.Standard_GRS;
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
                Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountUtils.mapAccountType(accountType), location, kind: Kind.Storage, accessTier: AccessTier.Cool),
                    string.Format("Creating storage account {0} in the resource group {1} with Kind.Storage and AccessTier.Cool should failed", accountName, resourceGroupName));

                if (lang == Language.NodeJS)
                {       
                    ExpectedContainErrorMessage("The storage account property '--access-tier' cannot be set at this time");
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        // Don't enable this case because CLI will prompt to ask for the access tier
        public void FTAccount117_CreateAccount_BlobStorage_NoAccessTier()
        {
            if (isResourceMode)
            {
                string accountType = Constants.AccountType.Standard_GRS;
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
                Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountUtils.mapAccountType(accountType), location, kind: Kind.BlobStorage),
                    string.Format("Creating storage account {0} in the resource group {1} with Kind.BlobStorage and no AccessTier should failed", accountName, resourceGroupName));
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount201_SetAccount_ChangeType_AccessTier_LabeL_Description()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = FileNamingGenerator.GenerateNameFromRange(15, validNameRange);
            string description = FileNamingGenerator.GenerateNameFromRange(20, validNameRange);
            string originalAccountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string newAccountType = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);

            SetAndValidateAccount(accountName, originalAccountType, label, description, newAccountType, kind: Kind.BlobStorage, originalAccessTier: AccessTier.Cool, newAccessTier: AccessTier.Hot);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount202_SetAccount_NonExisitingAccount()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = FileNamingGenerator.GenerateNameFromRange(15, validNameRange);
            string description = FileNamingGenerator.GenerateNameFromRange(20, validNameRange);
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);

            if (isResourceMode)
            {
                Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, accountType),
                    string.Format("Setting non-existing stoarge account {0} should fail", accountName));
                ExpectedAccoutNotFoundErrorMessage(resourceGroupName, accountName);
            }
            else
            {
                Test.Assert(!CommandAgent.SetAzureStorageAccount(accountName, label, description, accountType),
                    string.Format("Setting non-existing stoarge account {0} should fail", accountName));
                ExpectedContainErrorMessage(string.Format("The storage account '{0}' was not found", accountName));
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount203_SetAccount_NonExistingType()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Non-Existing Type";

            // No need to create a real accout for NodeJS as it won't pass the parameter validation
            string nonExistingType = FileNamingGenerator.GenerateNameFromRange(6, validNameRange);
            string errorMessageFormat = Language.PowerShell == lang ? PSHInvalidAccountTypeError : NodeJSInvalidSetTypeError;

            if (isResourceMode)
            {
                Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, nonExistingType),
                    string.Format("Setting stoarge account {0} to type {1} should fail", accountName, nonExistingType));
            }
            else
            {
                Test.Assert(!CommandAgent.SetAzureStorageAccount(accountName, label, description, nonExistingType),
                    string.Format("Setting stoarge account {0} to type {1} should fail", accountName, nonExistingType));

                if (Language.PowerShell == lang)
                {
                    errorMessageFormat = PSHASMAccountTypeInvalidError;
                }
            }

            ExpectedContainErrorMessage(string.Format(errorMessageFormat, nonExistingType));
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount204_SetAccount_ZRSToOthers()
        {
            if (isMooncake) return;

            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Set Type";
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_ZRS);
            string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
            string affinityGroup = null;

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                }
                else
                {
                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                }

                WaitForAccountAvailableToSet();

                foreach (FieldInfo info in typeof(Constants.AccountType).GetFields())
                {
                    string newAccountType = accountUtils.mapAccountType(info.GetRawConstantValue() as string);
                    string errorMsg = string.Empty;

                    if (isResourceMode)
                    {
                        if (string.Equals(newAccountType, Constants.AccountType.Standard_ZRS))
                        {
                            Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, newAccountType),
                                string.Format("Setting stoarge account {0} to type {1} should succeed.", accountName, newAccountType));
                            continue;
                        }
                        else
                        {
                            errorMsg = string.Format("Storage account type cannot be changed to {0}.", newAccountType);
                            Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, newAccountType),
                                string.Format("Setting storage account {0} to type {1} should fail", accountName, newAccountType));
                        }
                    }
                    else
                    {
                        if (string.Equals(newAccountType, Constants.AccountType.Standard_ZRS))
                        {
                            Test.Assert(CommandAgent.SetAzureStorageAccount(accountName, label, description, newAccountType),
                                string.Format("Setting stoarge account {0} to type {1} should succeed.", accountName, newAccountType));
                            continue;
                        }
                        else
                        {
                            errorMsg = string.Format("Cannot change storage account type from Standard_ZRS to {0} or vice versa", info.Name);
                            Test.Assert(!CommandAgent.SetAzureStorageAccount(accountName, label, description, newAccountType),
                                string.Format("Setting stoarge account {0} to type {1} should fail", accountName, newAccountType));
                        }
                    }

                    if (Language.NodeJS == lang)
                    {
                        if (newAccountType == accountUtils.mapAccountType(Constants.AccountType.Standard_ZRS) ||
                            newAccountType == accountUtils.mapAccountType(Constants.AccountType.Premium_LRS))
                        {
                            errorMsg = string.Format(NodeJSInvalidSetTypeError, newAccountType);
                        }
                    }
                    else if (isResourceMode)
                    {
                        if (newAccountType == accountUtils.mapAccountType(Constants.AccountType.Standard_ZRS)
                            || newAccountType == accountUtils.mapAccountType(Constants.AccountType.Premium_LRS))
                        {
                            errorMsg = string.Format("Storage account type cannot be changed to {0}.", info.Name);
                        }
                    }

                    ExpectedContainErrorMessage(errorMsg);
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount205_SetAccount_PLRSToOthers()
        {
            if (isMooncake) return;

            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Set Type";
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Premium_LRS);
            string location = accountUtils.GenerateAccountLocation(accountType, false, isMooncake);
            string affinityGroup = null;

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                }
                else
                {
                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                }

                WaitForAccountAvailableToSet();

                foreach (FieldInfo info in typeof(Constants.AccountType).GetFields())
                {
                    string newAccountType = accountUtils.mapAccountType(info.GetRawConstantValue() as string);
                    string errorMsg = string.Empty;

                    if (isResourceMode)
                    {
                        if (string.Equals(newAccountType, Constants.AccountType.Premium_LRS))
                        {
                            Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, newAccountType),
                             string.Format("Setting storage account {0} to type {1} should succeed", accountName, newAccountType));
                        }
                        else
                        {
                            errorMsg = string.Format("Storage account type cannot be changed to {0}.", newAccountType);
                            Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, newAccountType),
                                string.Format("Setting storage account {0} to type {1} should fail", accountName, newAccountType));
                        }
                    }
                    else
                    {
                        if (string.Equals(newAccountType, Constants.AccountType.Premium_LRS))
                        {
                            Test.Assert(CommandAgent.SetAzureStorageAccount(accountName, label, description, newAccountType),
                             string.Format("Setting storage account {0} to type {1} should succeed", accountName, newAccountType));
                        }
                        else
                        {
                            errorMsg = string.Format("Cannot change storage account type from Premium_LRS to {0} or vice versa", info.Name);
                            Test.Assert(!CommandAgent.SetAzureStorageAccount(accountName, label, description, newAccountType),
                                string.Format("Setting storage account {0} to type {1} should fail", accountName, newAccountType));
                        }
                    }

                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        if (Language.NodeJS == lang)
                        {
                            if (newAccountType == accountUtils.mapAccountType(Constants.AccountType.Standard_ZRS) ||
                                newAccountType == accountUtils.mapAccountType(Constants.AccountType.Premium_LRS))
                            {
                                errorMsg = string.Format(NodeJSInvalidSetTypeError, newAccountType);
                            }
                        }
                        else if (isResourceMode)
                        {
                            if (newAccountType == accountUtils.mapAccountType(Constants.AccountType.Premium_LRS)
                                || newAccountType == accountUtils.mapAccountType(Constants.AccountType.Standard_ZRS))
                            {
                                errorMsg = string.Format("Storage account type cannot be changed to {0}.", info.Name);
                            }
                        }

                        ExpectedContainErrorMessage(errorMsg);
                    }
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount206_SetAccount_OthersToZRSOrPLRS()
        {
            if (isMooncake) return;

            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Set Type";
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
            string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);

            if (isResourceMode)
            {
                this.CreateNewSRPAccount(accountName, location, accountType);
            }
            else
            {
                this.CreateNewAccount(accountName, label, description, location, null, accountType);
            }

            WaitForAccountAvailableToSet();

            // No need to create a real accout for NodeJS as it won't pass the parameter validation
            string[] newAccountTypes = { Constants.AccountType.Standard_ZRS, Constants.AccountType.Premium_LRS };
            foreach (string targetAccountType in newAccountTypes)
            {
                string type = accountUtils.mapAccountType(targetAccountType);
                string errorMessage = string.Empty;

                if (isResourceMode)
                {
                    errorMessage = string.Format("Storage account type cannot be changed to {0}.", targetAccountType);
                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, type),
                        string.Format("Setting stoarge account {0} to type {1} should fail", accountName, type));
                }
                else
                {
                    Test.Assert(!CommandAgent.SetAzureStorageAccount(accountName, label, description, type),
                        string.Format("Setting stoarge account {0} to type {1} should fail", accountName, type));

                    errorMessage = string.Format("Cannot change storage account type from Standard_LRS to {0} or vice versa", targetAccountType);
                }

                if (Language.NodeJS == lang)
                {
                    errorMessage = string.Format(NodeJSInvalidSetTypeError, type);
                }

                ExpectedContainErrorMessage(errorMessage);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount207_SetAccount_Tags()
        {
            if (isResourceMode)
            {
                string accountName = accountUtils.GenerateAccountName();
                try
                {
                    string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
                    string location = Constants.Location.EastAsia;

                    this.CreateNewSRPAccount(accountName, location, accountType);

                    WaitForAccountAvailableToSet();

                    Hashtable[] tags = this.GetUnicodeTags();

                    Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                        "Set tags of account {0} in reource group {1} should succeed", accountName, resourceGroupName);

                    Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                        "Get storage account {0} in resource group {1} should succeed.", resourceGroupName, accountName);

                    var targetTags = GetTagsFromOutput();
                    accountUtils.ValidateTags(tags, targetTags);

                    tags = new Hashtable[1];
                    tags[0] = new Hashtable();
                    tags[0].Add("Name", Utility.GenNameString("Value"));
                    tags[0].Add("Value", "");

                    Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                        "Set tags of account {0} in reource group {1} should succeed", accountName, resourceGroupName);

                    Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                        "Get storage account {0} in resource group {1} should succeed.", resourceGroupName, accountName);

                    targetTags = GetTagsFromOutput();
                    accountUtils.ValidateTags(tags, targetTags);

                    tags = new Hashtable[0];
                    Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                        "Set tags of account {0} in reource group {1} should succeed", accountName, resourceGroupName);

                    Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                        "Get storage account {0} in resource group {1} should succeed.", resourceGroupName, accountName);

                    targetTags = GetTagsFromOutput();
                    accountUtils.ValidateTags(tags, targetTags);
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount208_SetAccount_InvalidTags()
        {
            if (isResourceMode)
            {
                string accountName = accountUtils.GenerateAccountName();
                try
                {
                    string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
                    string location = Constants.Location.EastAsia;

                    this.CreateNewSRPAccount(accountName, location, accountType);

                    Hashtable[] tags = new Hashtable[1];
                    tags[0] = new Hashtable();
                    tags[0].Add("Name", "");
                    tags[0].Add("Value", Utility.GenNameString("Value"));

                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                        "Set tags of account {0} in reource group {1} should fail", accountName, resourceGroupName);
                    if (lang == Language.PowerShell)
                    {
                        ExpectedContainErrorMessage("The tag name must be non-null, non-empty and non-whitespace only. Please provide an actual value.");
                    }
                    else
                    {
                        ExpectedContainErrorMessage("The tag name must be non-null, non-empty and non-whitespace only. Please provide an actual value.");
                    }

                    tags[0] = new Hashtable();
                    tags[0].Add("Name", Utility.GenNameString("Name", random.Next(125, 500)));
                    tags[0].Add("Value", Utility.GenNameString("Value"));
                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                        "Set tags of account {0} in reource group {1} should fail", accountName, resourceGroupName);
                    if (lang == Language.PowerShell)
                    {
                        ExpectedContainErrorMessage("Maximum allowed length of 128 for tag key exceeded.");
                    }
                    else
                    {
                        ExpectedContainErrorMessage("Maximum allowed length of 128 for tag key exceeded");
                    }

                    tags[0] = new Hashtable();
                    tags[0].Add("Name", Utility.GenNameString("Name"));
                    tags[0].Add("Value", Utility.GenNameString("Value", random.Next(253, 500)));
                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                        "Set tags of account {0} in reource group {1} should fail", accountName, resourceGroupName);
                    if (lang == Language.PowerShell)
                    {
                        ExpectedContainErrorMessage(string.Format("Tag value too large.  Following tag value '{0}' exceeded the maximum length. Maximum allowed length for tag value - '256' characters.",
                            tags[0]["Value"].ToString()));
                    }
                    else
                    {
                        ExpectedContainErrorMessage(string.Format("Tag value too large.  Following tag value '{0}' exceeded the maximum length. Maximum allowed length for tag value - '256' characters.",
                            tags[0]["Value"].ToString()));
                    }

                    tags = new Hashtable[random.Next(16, 50)];
                    for (int i = 0; i < tags.Length; ++i)
                    {
                        tags[i] = new Hashtable();
                        tags[i].Add("Name", Utility.GenNameString("Name"));
                        tags[i].Add("Value", Utility.GenNameString("Value"));
                    }
                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                        "Set tags of account {0} in reource group {1} should fail", accountName, resourceGroupName);
                    if (lang == Language.PowerShell)
                    {
                        ExpectedContainErrorMessage(string.Format("Too many tags on the resource/resource group. Requested tag count - '{0}'. Maximum number of tags allowed - '15'.",
                           tags.Length));
                    }
                    else
                    {
                        ExpectedContainErrorMessage(string.Format("Too many tags on the resource/resource group. Requested tag count - '{0}'. Maximum number of tags allowed - '15'.",
                           tags.Length));
                    }
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount209_SetAccount_TagsWordCaseAndDuplicatedName()
        {
            if (isResourceMode)
            {
                string accountName = accountUtils.GenerateAccountName();
                try
                {
                    string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
                    string location = Constants.Location.EastAsia;

                    this.CreateNewSRPAccount(accountName, location, accountType);

                    WaitForAccountAvailableToSet();

                    Hashtable[] tags = this.GetUnicodeTags(caseTest: true);

                    if (Language.PowerShell == lang)
                    {
                        Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                            "Set tags of account {0} in reource group {1} should fail.", accountName, resourceGroupName);
                        ExpectedContainErrorMessage("Invalid tag format. Ensure that each tag has a unique name.");
                    }
                    else
                    {
                        Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                            "Set tags of account {0} in reource group {1} should succeeded.", accountName, resourceGroupName);

                        Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                        "Get storage account {0} in resource group {1} should succeed.", resourceGroupName, accountName);

                        var targetTags = GetTagsFromOutput();
                        accountUtils.ValidateTags(tags, targetTags);
                    }

                    tags = this.GetUnicodeTags(duplicatedName: true);

                    if (Language.PowerShell == lang)
                    {
                        Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                            "Set tags of account {0} in reource group {1} with empty value should fail", accountName, resourceGroupName);
                        ExpectedContainErrorMessage("Invalid tag format. Ensure that each tag has a unique name.");
                    }
                    else
                    {
                        Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, null, tags),
                            "Set tags of account {0} in reource group {1} with empty value should succeeded.", accountName, resourceGroupName);

                        Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                        "Get storage account {0} in resource group {1} should succeed.", resourceGroupName, accountName);

                        var targetTags = GetTagsFromOutput();
                        accountUtils.ValidateTags(tags, targetTags);
                    }
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount210_CreateSetAccount_CustomDomain()
        {
            if (isResourceMode)
            {
                string resourceGroup = resourceGroupName;
                string accountName = Test.Data.Get("CustomDomainAccountName");
                string customDomainName = Test.Data.Get("CustomDomain");
                string accountType = Constants.AccountType.Standard_LRS;
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
                bool? useSubdomain = GetRandomNullableBool();
                try
                {
                    //Create account with Cutomer Domain
                    CreateNewSRPAccount(accountName, location, accountType, null, Kind.Storage, Constants.EncryptionSupportServiceEnum.Blob, null, customDomainName, useSubdomain);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, accountType, null, Kind.Storage, null, customDomainName, useSubdomain, Constants.EncryptionSupportServiceEnum.Blob);

                    //Set Cutomer Domain to ""
                    useSubdomain = GetRandomNullableBool();
                    Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroup, accountName, customDomain: "", useSubdomain: useSubdomain), "Set custom domain should succeed.");
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, accountType, null, Kind.Storage, null, customDomain: null, useSubdomain: useSubdomain, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);

                    //Set Cutomer Domain to valid domain
                    useSubdomain = GetRandomNullableBool();
                    Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroup, accountName, customDomain: customDomainName, useSubdomain: useSubdomain, disableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob), "Set custom domain should succeed.");
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, accountType, null, Kind.Storage, null, customDomain: customDomainName, useSubdomain: useSubdomain);

                    Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(resourceGroup, accountName), "Get storage account should succeed.");

                    var targetCustomDomain = GetCustomDomainFromOutput();

                    Test.Assert(string.Equals(customDomainName, targetCustomDomain.Name), "Custom should be the one got set {0}.", targetCustomDomain.Name);

                    string accountKey = accountUtils.SRPStorageClient.StorageAccounts.ListKeys(resourceGroupName, accountName).Keys[0].Value;

                    CloudBlobContainer container = new CloudBlobContainer(
                        new Uri(string.Format("http://{0}/{1}", customDomainName, Utility.GenNameString("container"))),
                        new StorageCredentials(accountName, accountKey));

                    try
                    {
                        container.CreateIfNotExists();
                        container.FetchAttributes();
                    }
                    catch (Exception)
                    {
                        Test.Error("Operation on container with custom domain should succeed.");
                    }
                    finally
                    {
                        container.Delete();
                    }
                }
                finally
                {
                    //this.SetCustomDomain(resourceGroup, accountName, "", null);
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount212_SetAccount_InvalidCustomDomain()
        {
            if (isResourceMode)
            {
                string accountName = accountUtils.GenerateAccountName();
                try
                {
                    string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
                    string location = Constants.Location.EastAsia;

                    this.CreateNewSRPAccount(accountName, location, accountType);

                    string invalidCustomDomainName = "www.bing.com";
                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, customDomain: invalidCustomDomainName, useSubdomain: null), "Set custom domain should fail.");
                    ExpectedContainErrorMessage(string.Format("The custom domain name could not be verified. CNAME mapping from {0} to {1}.blob.core.windows.net does not exist.", invalidCustomDomainName, accountName));

                    invalidCustomDomainName = accountUtils.GenerateAccountName();
                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, customDomain: invalidCustomDomainName, useSubdomain: null), "Set custom domain should fail.");
                    ExpectedContainErrorMessage(string.Format("The custom domain name could not be verified. CNAME mapping from {0} to {1}.blob.core.windows.net does not exist.", invalidCustomDomainName, accountName));
                }
                finally
                {
                    this.DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount213_SetAccount_ChangeTags_EnableEncryptionService()
        {
            if (isResourceMode)
            {
                string skuName = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(skuName), isResourceMode, isMooncake);
                Hashtable[] origianlTags = this.GetUnicodeTags();
                Hashtable[] newTags = this.GetUnicodeTags();

                try
                {
                    CreateNewSRPAccount(accountName, location, skuName, tags: origianlTags, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, skuName, tags: origianlTags, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);

                    WaitForAccountAvailableToSet();

                    SetSRPAccount(accountName, disableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, skuName, tags: origianlTags, enableEncryptionService: null);

                    WaitForAccountAvailableToSet();

                    SetSRPAccount(accountName, tags: newTags, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, skuName, tags: newTags, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount214_SetAccount_AllParameters()
        {
            if (isResourceMode)
            {
                string originalSkuName = accountUtils.mapAccountType(Constants.AccountType.Standard_RAGRS);
                string newSkuName = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(originalSkuName), isResourceMode, isMooncake);
                Hashtable[] origianlTags = this.GetUnicodeTags();
                Hashtable[] newTags = this.GetUnicodeTags();

                try
                {
                    CreateNewSRPAccount(accountName, location, originalSkuName, kind: Kind.BlobStorage, accessTier: AccessTier.Cool, tags: origianlTags, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, originalSkuName, kind: Kind.BlobStorage, accessTier: AccessTier.Cool, tags: origianlTags, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);

                    WaitForAccountAvailableToSet();

                    //TODO: The customer domain not set.
                    SetSRPAccount(accountName, newSkuName, newTags, disableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob, accessTier: AccessTier.Hot);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, newSkuName, newTags, Kind.BlobStorage, accessTier: AccessTier.Hot, enableEncryptionService: null);
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount215_SetAccount_EachParameters()
        {
            if (isResourceMode)
            {
                string originalSkuName = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
                string newSkuName = accountUtils.mapAccountType(Constants.AccountType.Standard_RAGRS);
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(originalSkuName), isResourceMode, isMooncake);
                Hashtable[] origianlTags = this.GetUnicodeTags();
                Hashtable[] newTags = this.GetUnicodeTags();

                try
                {
                    CreateNewSRPAccount(accountName, location, originalSkuName, kind: Kind.BlobStorage, accessTier: AccessTier.Cool, tags: origianlTags, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, originalSkuName, kind: Kind.BlobStorage, accessTier: AccessTier.Cool, tags: origianlTags, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);

                    WaitForAccountAvailableToSet();

                    //Set Tags
                    SetSRPAccount(accountName, tags: newTags);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, originalSkuName, newTags, Kind.BlobStorage, accessTier: AccessTier.Cool, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);

                    //Set SkuName
                    SetSRPAccount(accountName, newSkuName);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, newSkuName, newTags, Kind.BlobStorage, accessTier: AccessTier.Cool, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);

                    //Set DisableEncryptionService
                    SetSRPAccount(accountName, disableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, newSkuName, newTags, Kind.BlobStorage, accessTier: AccessTier.Cool, enableEncryptionService: null);

                    //Set EnableEncryptionService
                    SetSRPAccount(accountName, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, newSkuName, newTags, Kind.BlobStorage, accessTier: AccessTier.Cool, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);

                    //Set AccessTier
                    SetSRPAccount(accountName, accessTier: AccessTier.Hot);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, newSkuName, newTags, Kind.BlobStorage, accessTier: AccessTier.Hot, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob);

                    //TODO: The customer domain not set.
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount216_SetAccount_Storage_SetAccessTier()
        {
            if (isResourceMode)
            {
                string skuName = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(skuName), isResourceMode, isMooncake);

                try
                {
                    CreateNewSRPAccount(accountName, location, skuName, kind: Kind.Storage);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, skuName, kind: Kind.Storage);

                    WaitForAccountAvailableToSet();

                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, accessTier: AccessTier.Cool),
                        string.Format("Change accessTier of storage account {0} in the resource group {1} with Kind.Storage should failed", accountName, resourceGroupName));

                    if (lang == Language.NodeJS)
                    {
                        ExpectedContainErrorMessage("The storage account property 'accessTier' is invalid or cannot be set at this time");
                    }
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount217_SetAccount_BlobStorage_SetAccessTier2Times()
        {
            if (isResourceMode)
            {
                string skuName = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(skuName), isResourceMode, isMooncake);

                try
                {
                    CreateNewSRPAccount(accountName, location, skuName, kind: Kind.BlobStorage, accessTier: AccessTier.Cool);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, skuName, kind: Kind.BlobStorage, accessTier: AccessTier.Cool);

                    WaitForAccountAvailableToSet();

                    SetSRPAccount(accountName, accessTier: AccessTier.Hot);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, skuName, kind: Kind.BlobStorage, accessTier: AccessTier.Hot);

                    WaitForAccountAvailableToSet();

                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, accessTier: AccessTier.Cool),
                        string.Format("Change accessTier 2 times continuelly of storage account {0} in the resource group {1}  should failed", accountName, resourceGroupName));

                    if (lang == Language.NodeJS)
                    {
                        ExpectedContainErrorMessage("The property 'accessTier' was specified in the input, but it cannot be updated");
                    }
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount218_SetAccount_SetBothEnableDisableBlobEncrption()
        {
            if (isResourceMode)
            {
                string skuName = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);
                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(skuName), isResourceMode, isMooncake);

                try
                {
                    CreateNewSRPAccount(accountName, location, skuName);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, skuName);

                    WaitForAccountAvailableToSet();

                    Test.Assert(!CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, enableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob, disableEncryptionService: Constants.EncryptionSupportServiceEnum.Blob),
                        string.Format("Set both enable and disable Blob Encryption of storage account {0} in the resource group {1}  should failed", accountName, resourceGroupName));

                    if (lang == Language.NodeJS)
                    {
                        ExpectedContainErrorMessage("Only one of --enable-encryption-service and --disable-encryption-service can be set for a service");
                    }
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount301_DeleteAccount_ExistingAccount()
        {
            string accountName = accountUtils.GenerateAccountName();
            string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode, isMooncake));
            string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode, isMooncake);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(CommandAgent.DeleteSRPAzureStorageAccount(resourceGroupName, accountName),
                        string.Format("Deleting stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;

                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                    Test.Assert(CommandAgent.DeleteAzureStorageAccount(accountName),
                        string.Format("Deleting stoarge account {0} should succeed", accountName));
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount302_DeleteAccount_NonExistingAccount()
        {
            string accountName = accountUtils.GenerateAccountName();

            if (isResourceMode)
            {
                Test.Assert(CommandAgent.DeleteSRPAzureStorageAccount(resourceGroupName, accountName),
                    string.Format("Deleting stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
            }
            else
            {
                Test.Assert(!CommandAgent.DeleteAzureStorageAccount(accountName),
                    string.Format("Deleting stoarge account {0} should fail", accountName));
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount303_DeleteAccount_NonExistingResourceGroup()
        {
            string accountName = accountUtils.GenerateAccountName();

            if (isResourceMode)
            {
                string nonExsitingGroupName = accountUtils.GenerateResourceGroupName();
                Test.Assert(!CommandAgent.DeleteSRPAzureStorageAccount(nonExsitingGroupName, accountName),
                    string.Format("Deleting stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount401_GetAccount_ShowAnExistingAccount()
        {
            string accountName = accountUtils.GenerateAccountName();
            string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode, isMooncake));
            string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode, isMooncake);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                        string.Format("Showing stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;

                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                    Test.Assert(CommandAgent.ShowAzureStorageAccount(accountName),
                        string.Format("Showing stoarge account {0} should succeed", accountName));
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount402_GetAccount_ListAccounts()
        {
            int accountCount = 3;
            string[] accountNames = new string[accountCount];
            string accountName = string.Empty;
            string location = string.Empty;
            string accountType = string.Empty;

            try
            {
                for (int i = 0; i < accountCount; i++)
                {
                    accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode, isMooncake));
                    accountName = accountUtils.GenerateAccountName();
                    location = accountUtils.GenerateAccountLocation(accountType, isResourceMode, isMooncake);

                    if (isResourceMode)
                    {
                        CreateNewSRPAccount(accountName, location, accountType);
                    }
                    else
                    {
                        string label = "StorageAccountLabel";
                        string description = "Storage Account Test Set Type";
                        string affinityGroup = string.Empty;

                        CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                    }
                    accountNames[i] = accountName;
                }

                string message = "Listing all stoarge accounts in the subscription should succeed";
                if (isResourceMode)
                {
                    Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(string.Empty, string.Empty), message);
                    Test.Assert(CommandAgent.ShowSRPAzureStorageAccount(resourceGroupName, string.Empty),
                        string.Format("Listing all stoarge accounts in the resource group {0} should succeed", resourceGroupName));
                }
                else
                {
                    Test.Assert(CommandAgent.ShowAzureStorageAccount(string.Empty), message);
                }

            }
            finally
            {
                for (int i = 0; i < accountCount; i++)
                {
                    DeleteAccountWrapper(accountNames[i]);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount403_GetAccount_ShowNonExistingAccount()
        {
            string nonExsitingGroupName = accountUtils.GenerateResourceGroupName();
            string accountName = accountUtils.GenerateAccountName();

            if (isResourceMode)
            {
                Test.Assert(!CommandAgent.ShowSRPAzureStorageAccount(nonExsitingGroupName, accountName),
                    string.Format("Showing non-existing stoarge account {0} in non-existing resource group {1} should fail", accountName, nonExsitingGroupName));

                Test.Assert(!CommandAgent.ShowSRPAzureStorageAccount(nonExsitingGroupName, string.Empty),
                    string.Format("Listing stoarge accounts in non-existing resource group {0} should fail", nonExsitingGroupName));

                Test.Assert(!CommandAgent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                    string.Format("Showing non-existing stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
            }
            else
            {
                Test.Assert(!CommandAgent.ShowAzureStorageAccount(accountName),
                    string.Format("Showing stoarge accounts {0} should fail", accountName));
            }

            try
            {
                if (isResourceMode)
                {
                    string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode, isMooncake));
                    string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode, isMooncake);
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(!CommandAgent.ShowSRPAzureStorageAccount(nonExsitingGroupName, accountName),
                        string.Format("Showing the existing stoarge account {0} in non-existing resource group {1} should fail", accountName, nonExsitingGroupName));
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount501_AccountKey_ListKeys()
        {
            string accountName = accountUtils.GenerateAccountName();
            string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode, isMooncake));
            string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode, isMooncake);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(CommandAgent.ShowSRPAzureStorageAccountKeys(resourceGroupName, accountName),
                        string.Format("Showing keys of the stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;

                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                    Test.Assert(CommandAgent.ShowAzureStorageAccountKeys(accountName),
                        string.Format("Showing keys of the stoarge account {0} should succeed", accountName));
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount502_AccountKey_ListKeysOfNonExistingAccount()
        {
            string accountName = accountUtils.GenerateAccountName();

            if (isResourceMode)
            {
                string nonExsitingGroupName = accountUtils.GenerateResourceGroupName();
                Test.Assert(!CommandAgent.ShowSRPAzureStorageAccountKeys(nonExsitingGroupName, accountName),
                    string.Format("Listing keys of the non-existing stoarge account {0} in non-existing resource group {1} should fail", accountName, nonExsitingGroupName));
                ExpectedContainErrorMessage(string.Format("Resource group '{0}' could not be found", nonExsitingGroupName));

                Test.Assert(!CommandAgent.ShowSRPAzureStorageAccountKeys(resourceGroupName, accountName),
                    string.Format("Listint keys of the non-existing stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
                ExpectedAccoutNotFoundErrorMessage(resourceGroupName, accountName);
            }
            else
            {
                Test.Assert(!CommandAgent.ShowAzureStorageAccountKeys(accountName),
                    string.Format("Listing keys of the stoarge accounts {0} should fail", accountName));
                ExpectedContainErrorMessage(string.Format("The storage account '{0}' was not found", accountName));
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount503_AccountKey_RenewKeys()
        {
            string accountName = accountUtils.GenerateAccountName();
            string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode, isMooncake));
            string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode, isMooncake);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(CommandAgent.RenewSRPAzureStorageAccountKeys(resourceGroupName, accountName, Constants.AccountKeyType.Primary),
                        string.Format("Renewing the primary key of the stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                    Test.Assert(CommandAgent.RenewSRPAzureStorageAccountKeys(resourceGroupName, accountName, Constants.AccountKeyType.Secondary),
                        string.Format("Renewing the secondary key of the stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;
                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);

                    Test.Assert(CommandAgent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Primary),
                        string.Format("Renewing the primary key of the stoarge account {0} should succeed", accountName));
                    Test.Assert(CommandAgent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Secondary),
                        string.Format("Renewing the secondary key of the stoarge account {0} should succeed", accountName));
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount504_AccountKey_RenewKeysForNonExistingAccount()
        {
            string accountName = accountUtils.GenerateAccountName();

            if (isResourceMode)
            {
                string nonExsitingGroupName = accountUtils.GenerateResourceGroupName();
                Test.Assert(!CommandAgent.RenewSRPAzureStorageAccountKeys(nonExsitingGroupName, accountName, Constants.AccountKeyType.Primary),
                    string.Format("Renewing the primary key of the non-existing stoarge account {0} in non-existing resource group {1} should fail", accountName, nonExsitingGroupName));
                ExpectedContainErrorMessage(string.Format("Resource group '{0}' could not be found", nonExsitingGroupName));

                Test.Assert(!CommandAgent.RenewSRPAzureStorageAccountKeys(resourceGroupName, accountName, Constants.AccountKeyType.Secondary),
                    string.Format("Renewing the secondary key of the non-existing stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
                ExpectedAccoutNotFoundErrorMessage(resourceGroupName, accountName);
            }
            else
            {
                Test.Assert(!CommandAgent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Primary),
                    string.Format("Renewing the primary key of the stoarge account {0} should fail", accountName));
                ExpectedContainErrorMessage(string.Format("The storage account '{0}' was not found", accountName));

                Test.Assert(!CommandAgent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Secondary),
                    string.Format("Renewing the secondary key of the stoarge account {0} should fail", accountName));
                ExpectedContainErrorMessage(string.Format("The storage account '{0}' was not found", accountName));
            }

            try
            {
                string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode, isMooncake));
                string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode, isMooncake);
                accountName = accountUtils.GenerateAccountName();

                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);

                    Test.Assert(!CommandAgent.RenewSRPAzureStorageAccountKeys(resourceGroupName, accountName, Constants.AccountKeyType.Invalid),
                        string.Format("Renewing an invalid key type of the stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;
                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);

                    Test.Assert(!CommandAgent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Invalid),
                        string.Format("Renewing an invalid key type of the stoarge account {0} should fail", accountName));
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount601_AccountNameAvailability_SameSubscription()
        {
            if (isResourceMode)
            {
                string accountName = accountUtils.GenerateAccountName();
                string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode, isMooncake));
                string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode, isMooncake);

                try
                {
                    CreateNewSRPAccount(accountName, location, accountType);

                    Test.Assert(CommandAgent.CheckNameAvailability(accountName), "Check name availability should succeed.");
                    AccountUtils.CheckNameAvailabilityResponse accountNameAvailability = null;

                    if (lang == Language.PowerShell)
                    {
                        accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(
                            CommandAgent.Output[0][PowerShellAgent.BaseObject] as SRPModel.CheckNameAvailabilityResult);
                    }
                    else
                    {
                        accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(CommandAgent.Output[0], isResourceMode);
                    }

                    this.ValidateAccountNameAvailability(accountNameAvailability, accountName, true, true);

                    accountUtils.SRPStorageClient.StorageAccounts.Delete(resourceGroupName, accountName);

                    Test.Assert(CommandAgent.CheckNameAvailability(accountName), "Check name availability should succeed.");
                    if (lang == Language.PowerShell)
                    {
                        accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(
                            CommandAgent.Output[0][PowerShellAgent.BaseObject] as SRPModel.CheckNameAvailabilityResult);
                    }
                    else
                    {
                        accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(CommandAgent.Output[0], isResourceMode);
                    }

                    this.ValidateAccountNameAvailability(accountNameAvailability, accountName, false, true);
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount602_AccountNameAvailability_DiffSubscription()
        {
            if (isResourceMode)
            {
                AccountUtils.CheckNameAvailabilityResponse accountNameAvailability = null;

                string accountName = Test.Data.Get("StorageAccountNameInOtherSubscription");
                Test.Assert(CommandAgent.CheckNameAvailability(accountName), "Check name availability should succeed.");

                if (isResourceMode && lang == Language.PowerShell)
                {
                    accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(
                        CommandAgent.Output[0][PowerShellAgent.BaseObject] as SRPModel.CheckNameAvailabilityResult);
                }
                else
                {
                    accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(CommandAgent.Output[0], isResourceMode);
                }

                this.ValidateAccountNameAvailability(accountNameAvailability, accountName, true, false);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount603_AccountNameAvailability_NotExist()
        {
            if (isResourceMode)
            {
                AccountUtils.CheckNameAvailabilityResponse accountNameAvailability = null;

                string accountName = accountUtils.GenerateAccountName();
                Test.Assert(CommandAgent.CheckNameAvailability(accountName), "Check name availability should succeed.");

                if (isResourceMode && lang == Language.PowerShell)
                {
                    accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(
                        CommandAgent.Output[0][PowerShellAgent.BaseObject] as SRPModel.CheckNameAvailabilityResult);
                }
                else
                {
                    accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(CommandAgent.Output[0], isResourceMode);
                }

                this.ValidateAccountNameAvailability(accountNameAvailability, accountName, false, true);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount604_AccountNameAvailability_NotExist_LongestName()
        {
            if (isResourceMode)
            {
                AccountUtils.CheckNameAvailabilityResponse accountNameAvailability = null;

                string accountName = accountUtils.GenerateAccountName(24);
                Test.Assert(CommandAgent.CheckNameAvailability(accountName), "Check name availability should succeed.");

                if (isResourceMode && lang == Language.PowerShell)
                {
                    accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(
                        CommandAgent.Output[0][PowerShellAgent.BaseObject] as SRPModel.CheckNameAvailabilityResult);
                }
                else
                {
                    accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(CommandAgent.Output[0], isResourceMode);
                }

                this.ValidateAccountNameAvailability(accountNameAvailability, accountName, false, true);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount605_AccountNameAvailability_Exist_ShortestName()
        {
            if (isResourceMode)
            {
                AccountUtils.CheckNameAvailabilityResponse accountNameAvailability = null;

                string accountName = AccountUtils.GenerateAvailableAccountName(3);

                try
                {
                    if (accountUtils.SRPStorageClient.StorageAccounts.CheckNameAvailability(accountName).NameAvailable.Value)
                    {
                        string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
                        string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(accountType), isResourceMode, isMooncake);
                        CreateNewSRPAccount(accountName, location, accountType);
                    }

                    Test.Assert(CommandAgent.CheckNameAvailability(accountName), "Check name availability should succeed.");

                    if (lang == Language.PowerShell)
                    {
                        accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(
                            CommandAgent.Output[0][PowerShellAgent.BaseObject] as SRPModel.CheckNameAvailabilityResult);
                    }
                    else
                    {
                        accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(CommandAgent.Output[0], isResourceMode);
                    }

                    this.ValidateAccountNameAvailability(accountNameAvailability, accountName, true, true);
                }
                finally
                {
                    DeleteAccountWrapper(accountName);
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount606_AccountNameAvailability_InvalidName()
        {
            if (isResourceMode)
            {
                string accountName = AccountUtils.GenerateAvailableAccountName(2);
                AccountNameAvailability_InvalidName_Test(accountName);

                accountName = AccountUtils.GenerateAvailableAccountName(random.Next(25, 100));
                AccountNameAvailability_InvalidName_Test(accountName);

                accountName = "ACCOUNT";
                AccountNameAvailability_InvalidName_Test(accountName);

                accountName = FileNamingGenerator.GenerateInvalidAccountName();
                AccountNameAvailability_InvalidName_Test(accountName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount607_AccountNameAvailability_Negative()
        {
            if (isResourceMode)
            {
                string errorMsg;
                if (lang == Language.PowerShell)
                {
                    errorMsg = "The argument is null or empty. Provide an argument that is not null or empty, and then try the command again.";
                }
                else
                {
                    errorMsg = "The check name availability request must have an account name";
                }

                Test.Assert(!CommandAgent.CheckNameAvailability(null), "Check name availability should fail.");
                ExpectedContainErrorMessage(errorMsg);

                Test.Assert(!CommandAgent.CheckNameAvailability(""), "Check name availability should fail.");
                ExpectedContainErrorMessage(errorMsg);
            }
        }

        private void AccountNameAvailability_InvalidName_Test(string accountName)
        {
            AccountUtils.CheckNameAvailabilityResponse accountNameAvailability = null;

            Test.Assert(CommandAgent.CheckNameAvailability(accountName), "Check name availability should succeed.");

            if (isResourceMode && lang == Language.PowerShell)
            {
                accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(
                    CommandAgent.Output[0][PowerShellAgent.BaseObject] as SRPModel.CheckNameAvailabilityResult);
            }
            else
            {
                accountNameAvailability = AccountUtils.CheckNameAvailabilityResponse.Create(CommandAgent.Output[0], isResourceMode);
            }

            this.ValidateAccountNameAvailabilityInvalidName(accountNameAvailability, accountName);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount701_GetAzureStorageUsage()
        {
            if (isResourceMode)
            {
                GetAzureStorageUsage_Test();

                List<string> accountNames = new List<string>();

                try
                {
                    int accountCount = random.Next(1, 5);

                    while (accountCount > 0)
                    {
                        string accountName = accountUtils.GenerateAccountName();
                        string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode, isMooncake));
                        string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode, isMooncake);

                        CreateNewSRPAccount(accountName, location, accountType);
                        accountCount--;
                    }

                    GetAzureStorageUsage_Test();

                    foreach (var accountName in accountNames)
                    {
                        DeleteAccountWrapper(accountName);
                    }

                    GetAzureStorageUsage_Test();
                }
                finally
                {
                    foreach (var accountName in accountNames)
                    {
                        DeleteAccountWrapper(accountName);
                    }
                }
            }
            else
            {
                if (lang == Language.NodeJS)
                {
                    Test.Assert(!CommandAgent.GetAzureStorageUsage(), "Get azure storage usage should fail.");
                    ExpectedContainErrorMessage("'usage' is not an azure command. See 'azure help'.");
                }
            }
        }

        private void GetAzureStorageUsage_Test()
        {
            Test.Assert(CommandAgent.GetAzureStorageUsage(), "Get azure storage usage should succeeded.");
            var usages = accountUtils.SRPStorageClient.Usage.List().Value;
            ValidateGetUsageOutput(usages);
        }

        private void CreateAndValidateAccountWithInvalidTags(string accountName, string location, string accountType, Hashtable[] tags)
        {
            try
            {
                StorageAccountGetResponse response;
                try
                {
                    // Use service management client to check the existing account for a global search
                    response = accountUtils.StorageClient.StorageAccounts.Get(accountName);
                }
                catch (Hyak.Common.CloudException ex)
                {
                    Test.Assert(ex.Error.Code.Equals("ResourceNotFound"), string.Format("Account {0} should not exist. Exception: {1}", accountName, ex));
                    createdAccounts.Add(accountName);
                }

                Test.Assert(!CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location, tags),
                    string.Format("Creating storage account {0} in the resource group {1} at location {2} should failed", accountName, resourceGroupName, location));
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        private void CreateAndValidateAccount(
            string accountName,
            string label,
            string description,
            string location,
            string affinityGroup,
            string accountType,
            Hashtable[] tags,
            bool? geoReplication = null,
            Kind kind = Kind.Storage,
            Constants.EncryptionSupportServiceEnum? enableEncryptionService = null,
            AccessTier? accessTier = null,
            string customDomain = null,
            bool? useSubdomain = null)
        {
            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType, tags, kind, enableEncryptionService, accessTier, customDomain, useSubdomain);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, accountType, tags, kind, accessTier, customDomain, useSubdomain, enableEncryptionService);
                }
                else
                {
                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType, geoReplication);
                    ValidateAccount(accountName, label, description, location, affinityGroup, accountType, geoReplication);
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        private void SetAndValidateAccount(string accountName,
            string originalAccountType,
            string newLabel,
            string newDescription,
            string newAccountType,
            bool? geoReplication = null,
            Hashtable[] tags = null,
            Kind kind = Kind.Storage,
            AccessTier? originalAccessTier = null,
            AccessTier? newAccessTier = null)
        {
            string location = accountUtils.GenerateAccountLocation(accountUtils.mapAccountType(originalAccountType), isResourceMode, isMooncake);
            string affinityGroup = null;
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Setting";

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, originalAccountType, kind: kind, accessTier: originalAccessTier);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, originalAccountType, kind: kind, accessTier: originalAccessTier);

                    WaitForAccountAvailableToSet();

                    SetSRPAccount(accountName, newAccountType, tags, null, null, newAccessTier);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, null, newAccountType, tags, kind, newAccessTier);
                }
                else
                {
                    CreateNewAccount(accountName, label, description, location, affinityGroup, originalAccountType, geoReplication);
                    ValidateAccount(accountName, label, description, location, affinityGroup, originalAccountType, geoReplication);

                    WaitForAccountAvailableToSet();

                    SetAccount(accountName, newLabel, newDescription, newAccountType, geoReplication);
                    ValidateAccount(accountName, newLabel, newDescription, null, null, newAccountType, geoReplication);
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        private void ValidateAccountNameAvailability(AccountUtils.CheckNameAvailabilityResponse accountNameAvailability, string accountName, bool exist, bool sameSubscription)
        {
            if (exist)
            {
                if (isResourceMode)
                {
                    Test.Assert(accountNameAvailability.Message.Contains(string.Format("The storage account named {0} is already taken.", accountName)),
                        "Account name availability message should be correct. {0}",
                        accountNameAvailability.Message);
                    Test.Assert(!accountNameAvailability.NameAvailable.Value, "Account name should be not available.");
                    Test.Assert(accountNameAvailability.Reason == SRPModel.Reason.AlreadyExists, "Reason should be AlreadyExists.");
                }
                else
                {
                    if (sameSubscription)
                    {
                        Test.Assert(accountNameAvailability.Message.Contains(string.Format("A storage account named '{0}' already exists in the subscription", accountName)),
                        "Account name availability message should be correct. {0}",
                        accountNameAvailability.Message);
                    }
                    else
                    {
                        Test.Assert(accountNameAvailability.Message.Contains(string.Format("The storage account named '{0}' is already taken.", accountName)),
                        "Account name availability message should be correct. {0}",
                        accountNameAvailability.Message);
                    }

                    Test.Assert(!accountNameAvailability.NameAvailable.Value, "Account name should be not available.");
                }
            }
            else
            {
                Test.Assert(null == accountNameAvailability.Message,
                    "Account name availability message should be null, {0}.", accountNameAvailability.Message);
                Test.Assert(accountNameAvailability.NameAvailable.Value, "Account name should be available.");
                Test.Assert(null == accountNameAvailability.Reason, "Reason should be null.");
            }
        }

        private void ValidateAccountNameAvailabilityInvalidName(AccountUtils.CheckNameAvailabilityResponse accountNameAvailability, string accountName)
        {
            Test.Assert(!accountNameAvailability.NameAvailable.Value, "Account name should be not available.");

            if (isResourceMode)
            {
                Test.Assert(accountNameAvailability.Reason == SRPModel.Reason.AccountNameInvalid, "Reason should be AccountNameInvalid.");
                Test.Assert(accountNameAvailability.Message.Contains(string.Format("{0} is not a valid storage account name.", accountName)),
                "Account name availability message should be correct. {0}",
                accountNameAvailability.Message);
            }
            else
            {
                string errorMsg1 = "The name is not a valid storage account name. Storage account names must be between 3 and 24 characters in length and use numbers and lower-case letters only.";
                string errorMsg2 = "The resource service name storageservices is not supported.";
                Test.Assert(accountNameAvailability.Message.Contains(errorMsg1) || accountNameAvailability.Message.Contains(errorMsg2),
                "Account name availability message should be correct. {0}",
                accountNameAvailability.Message);
            }
        }

        private void ValidateGetUsageOutput(IList<SRPModel.Usage> usages)
        {
            for (int i = 0; i < usages.Count; ++i)
            {
                var output = CommandAgent.Output[i];
                if (lang == Language.PowerShell)
                {
                    Test.Assert(string.Equals(output["Name"] as string, usages[i].Name.Value), "Name should be the same {0} == {1}", output["Name"], usages[i].Name.Value);
                    Test.Assert(string.Equals(output["LocalizedName"] as string, usages[i].Name.LocalizedValue), "LocalizedName should be the same {0} == {1}", output["LocalizedName"], usages[i].Name.LocalizedValue);
                    Test.Assert(output["Unit"] as SRPModel.UsageUnit? == usages[i].Unit, "Unit should be the same {0} == {1}", output["Unit"], usages[i].Unit);
                    Test.Assert(output["CurrentValue"] as int? == usages[i].CurrentValue, "CurrentValue should be the same {0} == {1}", output["CurrentValue"], usages[i].CurrentValue);
                    Test.Assert(output["Limit"] as int? == usages[i].Limit, "Limit should be the same {0} == {1}", output["Limit"], usages[i].Limit);
                }
                else
                {
                    int used = Utility.ParseIntFromJsonOutput(output, "used");
                    int limit = Utility.ParseIntFromJsonOutput(output, "limit");
                    Test.Assert(used == usages[i].CurrentValue, "CurrentValue should be the same {0} == {1}", used, usages[i].CurrentValue);
                    Test.Assert(limit == usages[i].Limit, "Limit should be the same {0} == {1}", limit, usages[i].Limit);
                }
            }
        }

        private void DeleteAccountWrapper(string accountName)
        {
            if (isResourceMode)
            {
                DeleteSRPAccount(accountName);
            }
            else
            {
                DeleteAccount(accountName);
            }
        }

        #region Service managment account operations
        private void CreateNewAccount(string accountName, string label, string description, string location, string affinityGroup, string accountType, bool? geoReplication = null)
        {
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");

            StorageAccountGetResponse response;
            try
            {
                response = accountUtils.StorageClient.StorageAccounts.Get(accountName);
            }
            catch (Hyak.Common.CloudException ex)
            {
                Test.Assert(ex.Error.Code.Equals("ResourceNotFound"), string.Format("Account {0} should not exist. Exception: {1}", accountName, ex));
                createdAccounts.Add(accountName);
            }

            Test.Assert(CommandAgent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType, geoReplication),
                string.Format("Creating stoarge account {0} in location {1} should succeed", accountName, location));
        }

        private void SetAccount(string accountName, string newLabel, string newDescription, string newAccountType, bool? geoReplication = null)
        {
            StorageAccountGetResponse response = accountUtils.StorageClient.StorageAccounts.Get(accountName);
            Test.Assert(response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            Test.Assert(CommandAgent.SetAzureStorageAccount(accountName, newLabel, newDescription, newAccountType, geoReplication),
                string.Format("Creating stoarge account {0} with type {1} should succeed", accountName, newAccountType));
        }

        private void ValidateAccount(string accountName, string label, string description, string location, string affinityGroup, string accountType, bool? geoReplication = null)
        {
            StorageAccountGetResponse response = accountUtils.StorageClient.StorageAccounts.Get(accountName);
            Test.Assert(response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            Microsoft.WindowsAzure.Management.Storage.Models.StorageAccount account = response.StorageAccount;
            Test.Assert(accountName == account.Name, string.Format("Expected account name is {0} and actually it is {1}", accountName, account.Name));
            Test.Assert(label == account.Properties.Label, string.Format("Expected label is {0} and actually it is {1}", label, account.Properties.Label));
            Test.Assert(description == account.Properties.Description, string.Format("Expected description is {0} and actually it is {1}", description, account.Properties.Description));
            Test.Assert(accountUtils.mapAccountType(typeof(Constants.AccountType).GetField(account.Properties.AccountType).GetRawConstantValue() as string).Equals(accountType),
                string.Format("Expected account type is {0} and actually it is {1}", accountType, account.Properties.AccountType));

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

            if (!string.IsNullOrEmpty(affinityGroup))
            {
                Test.Assert(affinityGroup == account.Properties.AffinityGroup, string.Format("Expected affinity group is {0} and actually it is {1}", affinityGroup, account.Properties.AffinityGroup));
            }
        }

        private void DeleteAccount(string accountName)
        {
            try
            {
                if (createdAccounts.Contains(accountName))
                {
                    accountUtils.StorageClient.StorageAccounts.DeleteAsync(accountName).Wait();
                }
            }
            catch (Exception ex)
            {
                Test.Info(string.Format("Deleting Account Exception: {0}", ex));
            }
        }
        #endregion

        #region Resource management account operations

        private void CreateNewSRPAccount(string accountName,
            string location,
            string skuName,
            Hashtable[] tags = null,
            Kind kind = Kind.Storage,
            Constants.EncryptionSupportServiceEnum? enableEncryptionService = null,
            AccessTier? accessTier = null,
            string customDomain = null,
            bool? useSubdomain = null)
        {
            try
            {
                createdAccounts.Add(accountName);
                DeleteAccountWrapper(accountName);
            }
            catch (Exception ex)
            {
                Test.Assert(false, string.Format("Account {0} should not exist and be deleted successfully. Exception: {1}", accountName, ex));
            }

            DateTime startTime = DateTime.Now;
            bool accountCreated = false;
            while (!accountCreated && DateTime.Now.CompareTo(startTime.AddMinutes(15)) < 0)
            {
                if (CommandAgent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, skuName, location, tags, kind, enableEncryptionService, accessTier, customDomain, useSubdomain))
                {
                    accountCreated = true;
                }
                else
                {
                    Thread.Sleep(10000);
                }
            }
            Test.Assert(accountCreated, string.Format("Creating storage account {0} in the resource group {1} at location {2} should succeed", accountName, resourceGroupName, location));
        }

        private void SetSRPAccount(string accountName,
            string skuName = null,
            Hashtable[] tags = null,
            Constants.EncryptionSupportServiceEnum? enableEncryptionService = null,
            Constants.EncryptionSupportServiceEnum? disableEncryptionService = null,
            AccessTier? accessTier = null,
            string customDomain = null,
            bool? useSubdomain = null)
        {
            AzureOperationResponse<SRPModel.StorageAccount> response = accountUtils.SRPStorageClient.StorageAccounts.GetPropertiesWithHttpMessagesAsync(resourceGroupName, accountName).Result;
            Test.Assert(response.Response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            Test.Assert(CommandAgent.SetSRPAzureStorageAccount(resourceGroupName, accountName, skuName, tags, enableEncryptionService, disableEncryptionService, accessTier, customDomain, useSubdomain),
                string.Format("Setting storage account {0} in resource group {1} should succeed: SkuName:{2}; Tags: {3}; enableEncryptionService: {4}; disableEncryptionService: {5}, accessTier: {6}, customDomain: {7}; useSubdomain: {8}",
                accountName, resourceGroupName, skuName, tags, enableEncryptionService, disableEncryptionService, accessTier, customDomain, useSubdomain));
        }

        private void DeleteSRPAccount(string accountName)
        {
            try
            {
                if (createdAccounts.Contains(accountName))
                {
                    accountUtils.SRPStorageClient.StorageAccounts.DeleteAsync(resourceGroupName, accountName, CancellationToken.None).Wait();
                }
            }
            catch (Exception ex)
            {
                Test.Info(string.Format("Deleting SRP Account Exception: {0}", ex));
            }
        }
        #endregion

        private void ErrorEndpoint(ServiceType type, ErrorType errorType)
        {
            // Arrange
            bool result = false;
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;
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
                argument = string.Format("{0} {1}", accountNameForConnectionStringTest, option);
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

                argument = string.Format("{0} {1} {2}", accountNameForConnectionStringTest, option, endpoint);
                comparison = string.Format("azure storage account connectionstring show {0} {1} {2}", accountNameForConnectionStringTest, option, endpoint);
            }

            // Act
            // TODO: Investigate sometimes the result is true and the error message is only "\n".    
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName) && (nodeAgent.Output.Count != 0);

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
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;
            string endpoint = "a.a";

            // Act
            string argument = string.Format("{0} --blob-endpoint {1}", accountNameForConnectionStringTest, endpoint);
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
            NodeJSAgent nodeAgent = (NodeJSAgent)CommandAgent;

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
            string argument = string.Format(argumentTemplate, accountNameForConnectionStringTest, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpoint);
            result = nodeAgent.ShowAzureStorageAccountConnectionString(argument, resourceGroupName);

            // Assert
            string expect = string.Format(expectTempalte, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpoint, accountNameForConnectionStringTest, primaryKeyForConnectionStringTest);

            Test.Info(string.Format("The connection string is: {0}", nodeAgent.Output[0]["string"] as string));
            Test.Assert(expect.Equals(nodeAgent.Output[0]["string"] as string), "Two strings should be identical.");
        }

        private void WaitForAccountAvailableToSet()
        {
            if (isResourceMode)
            {
                Thread.Sleep(60000);
            }
        }

        private string ConvertAccountType(string accountType)
        {
            string accountTypeInErrorMessage = accountType.Replace('_', '-');

            if ((Language.PowerShell == lang) && isResourceMode)
            {
                return accountTypeInErrorMessage.Replace("Premium", "Provisioned");
            }

            return accountTypeInErrorMessage;
        }

        private Hashtable[] GetUnicodeTags(bool caseTest = false, bool duplicatedName = false)
        {
            var unicodeNameChars = new List<string>(FileNamingGenerator.GenerateTagValidateUnicodeName(random.Next(1, 129)));
            var unicodeValueChars = new List<string>(FileNamingGenerator.GenerateTagValidateUnicodeName(random.Next(0, 257)));

            int maxTagCount = duplicatedName ? 16 : 15;
            int count = (caseTest || duplicatedName) ? Math.Min(maxTagCount, 2 * unicodeNameChars.Count) : unicodeNameChars.Count;
            Hashtable[] tags = new Hashtable[count];

            for (int i = 0; i < unicodeNameChars.Count; ++i)
            {
                tags[i] = new Hashtable();
                tags[i].Add("Name", unicodeNameChars[i]);
                tags[i].Add("Value", unicodeValueChars[i]);

                Test.Info("Tag Name: '{0}'  Tag Value: '{1}'", unicodeNameChars[i], unicodeValueChars[i]);
            }

            if (caseTest || duplicatedName)
            {
                for (int j = unicodeNameChars.Count; j < count; j++)
                {
                    tags[j] = new Hashtable();
                    string name = string.Empty;

                    foreach (char ch in unicodeNameChars[j - unicodeNameChars.Count])
                    {
                        string s = new string(ch, 1);
                        if (random.Next() % 2 == 0)
                        {
                            name += s.ToLowerInvariant();
                        }
                        else
                        {
                            name += s.ToUpperInvariant();
                        }
                    }
                    tags[j].Add("Name", name);
                    tags[j].Add("Value", unicodeValueChars[j - unicodeNameChars.Count]);

                    Test.Info("Tag Name for word case: '{0}'  Tag Value: '{1}'", name, unicodeValueChars[j - unicodeNameChars.Count]);
                }
            }

            return tags;
        }

        private void SetCustomDomain(string resourceGroupName, string accountName, string customDomain, bool? useSubdomain)
        {
            var result = accountUtils.SRPStorageClient.StorageAccounts.UpdateAsync(resourceGroupName, accountName,
                new SRPModel.StorageAccountUpdateParameters()
                {
                    CustomDomain = new SRPModel.CustomDomain
                    {
                        Name = customDomain,
                        UseSubDomain = useSubdomain
                    }
                },
                CancellationToken.None).Result;
        }

        private IDictionary<string, string> GetTagsFromOutput()
        {
            Dictionary<string, string> tags = null;
            if (lang == Language.PowerShell)
            {
                return CommandAgent.Output[0]["Tags"] as IDictionary<string, string>;
            }
            else
            {
                tags = new Dictionary<string, string>();
                IDictionary<string, JToken> targetTags = (IDictionary<string, JToken>)CommandAgent.Output[0]["tags"];

                foreach (string key in targetTags.Keys)
                {
                    tags[key] = targetTags[key].ToString();
                }
            }

            return tags;
        }

        private SRPModel.CustomDomain GetCustomDomainFromOutput()
        {
            if (lang == Language.PowerShell)
            {
                return CommandAgent.Output[0]["CustomDomain"] as SRPModel.CustomDomain;
            }
            else
            {
                SRPModel.CustomDomain customDomain = null;
                if (CommandAgent.Output[0].ContainsKey("customDomain"))
                {
                    customDomain = new SRPModel.CustomDomain();
                    JObject outputObj = (JObject)CommandAgent.Output[0]["customDomain"];
                    customDomain.Name = outputObj["name"].ToString();
                }

                return customDomain;
            }
        }

        private string GetAccountKeyFromOutput()
        {
            if (lang == Language.PowerShell)
            {
                return (CommandAgent.Output[0]["Keys"] as IList<StorageAccountKey>)[0].Value;
            }
            else
            {
                return null;
            }
        }

        public bool? GetRandomNullableBool()
        {
            switch (random.Next(0, 3))
            {
                case 0:
                    return null;
                case 1:
                    return false;
                case 2:
                default:
                    return true;
            }
        }

        private enum ServiceType { Blob, Queue, Table, File }

        private enum ErrorType { Empty, Invalid, Unsupported }
    }
}

