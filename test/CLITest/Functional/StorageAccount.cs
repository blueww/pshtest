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
    using System.Collections.Generic;
    using System.Net;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Management;
    using Microsoft.WindowsAzure.Management.Models;
    using Microsoft.WindowsAzure.Management.Storage;
    using Microsoft.WindowsAzure.Management.Storage.Models;
    using MS.Test.Common.MsTestLib;
    using SRPModel = Microsoft.Azure.Management.Storage.Models;
    using StorageTestLib;

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

        private bool isLogin = false;

        private bool accountImported = false;

        private Tuple<int, int> validNameRange = new Tuple<int, int>((int)'a', (int)'z');

        private List<string> createdAccounts = new List<string>();

        private const string PSHInvalidAccountTypeError =
            "Cannot validate argument on parameter 'Type'. The argument \"{0}\" does not belong to the set \"Standard_LRS,Standard_ZRS,Standard_GRS,Standard_RAGRS,Premium_LRS\" specified by the ValidateSet attribute.";

        private const string NodeJSInvalidTypeError = "Invalid value: {0}. Options are: LRS,ZRS,GRS,RAGRS,PLRS";

        #region Additional test attributes

        [ClassInitialize()]
        public static void StorageAccountTestInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            NodeJSAgent.AgentConfig.UseEnvVar = false;

            managementClient = new ManagementClient(Utility.GetCertificateCloudCredential());
            accountUtils = new AccountUtils(lang);

            accountNameForConnectionStringTest = Test.Data.Get("StorageAccountName");
            primaryKeyForConnectionStringTest = Test.Data.Get("StorageAccountKey");

            if (isResourceMode)
            {
                resourceLocation = accountUtils.GenerateAccountLocation(Constants.AccountType.Standard_GRS, true);
                resourceManager = new ResourceManagerWrapper();
                resourceGroupName = accountUtils.GenerateResourceGroupName();
                resourceManager.CreateResourceGroup(resourceGroupName, resourceLocation);

                accountNameForConnectionStringTest = accountUtils.GenerateAccountName();

                var parameters = new SRPModel.StorageAccountCreateParameters(SRPModel.AccountType.StandardGRS, Constants.Location.EastAsia);
                accountUtils.SRPStorageClient.StorageAccounts.CreateAsync(resourceGroupName, accountNameForConnectionStringTest, parameters, CancellationToken.None).Wait();
                var keys = accountUtils.SRPStorageClient.StorageAccounts.ListKeysAsync(resourceGroupName, accountNameForConnectionStringTest, CancellationToken.None).Result;
                primaryKeyForConnectionStringTest = keys.StorageAccountKeys.Key1;
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

            TestBase.TestClassCleanup();
        }

        public override void OnTestSetup()
        {
            if (isResourceMode)
            {
                if (lang == Language.NodeJS)
                {
                    agent.ChangeCLIMode(Constants.Mode.arm);
                }

                bool needLogin = false;
                string autoLogin = Test.Data.Get("AutoLogin");

                if (!string.IsNullOrEmpty(autoLogin))
                {
                    needLogin = bool.Parse(autoLogin);
                }

                if (!isLogin && needLogin)
                {
                    int retry = 0;
                    do
                    {
                        if (agent.HadErrors)
                        {
                            Thread.Sleep(5000);
                            Test.Info(string.Format("Retry login... Count:{0}", retry));
                        }

                        //TODO: We are using local cached token as credentials for now, 
                        // so disable logout and login here. In the future, we'll still need 
                        // to log on before any test cases running..

                        //agent.Logout();
                        //agent.Login();
                    }
                    while (agent.HadErrors && retry++ < 5);

                    if (lang == Language.NodeJS)
                    {
                        SetActiveSubscription();
                    }
                    else
                    {
                        string subscriptionID = Test.Data.Get("AzureSubscriptionID");
                        agent.SetActiveSubscription(subscriptionID);
                    }

                    isLogin = true;
                }
            }
            else
            {
                if (!accountImported)
                {
                    if (lang == Language.NodeJS)
                    {
                        NodeJSAgent nodeAgent = (NodeJSAgent)agent;
                        nodeAgent.ChangeCLIMode(Constants.Mode.asm);
                    }

                    string settingFile = Test.Data.Get("AzureSubscriptionPath");
                    string subscriptionId = Test.Data.Get("AzureSubscriptionID");
                    agent.ImportAzureSubscription(settingFile);

                    string subscriptionID = Test.Data.Get("AzureSubscriptionID");
                    agent.SetActiveSubscription(subscriptionID);

                    accountImported = true;
                }
            }
        }

        #endregion

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void CreateSRPAccount()
        {
            string accountName = accountUtils.GenerateAccountName();
            string location = Constants.SRPLocations[random.Next(0, Constants.SRPLocations.Length)];

            try
            {
                Test.Assert(agent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, Constants.AccountType.Standard_LRS, location),
                    "Create account {0} of resource group {1} should succeeded.", accountName, resourceGroupName);

                accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, Constants.AccountType.Standard_LRS);
            }
            finally
            {
                this.DeleteSRPAccount(accountName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void SetSRPAccount()
        {
            string accountName = accountUtils.GenerateAccountName();
            string location = Constants.SRPLocations[random.Next(0, Constants.SRPLocations.Length)];

            try
            {
                this.CreateNewSRPAccount(accountName, location, Constants.AccountType.Standard_LRS);

                WaitForAccountAvailableToSet();

                Test.Assert(agent.SetSRPAzureStorageAccount(resourceGroupName, accountName, Constants.AccountType.Standard_GRS),
                    "Set account {0} of resource group {1} should succeeded.", accountName, resourceGroupName);

                accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, Constants.AccountType.Standard_GRS);
            }
            finally
            {
                this.DeleteSRPAccount(accountName);
            }
        }

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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = "idonotexist";

            // Act
            result = nodeAgent.ShowAzureStorageAccountConnectionString(account, resourceGroupName);

            // Assert
            if (isResourceMode)
            {
                ExpectedNotFoundErrorMessage();
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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
            string account = "invalid-account";

            // Act
            result = nodeAgent.ShowAzureStorageAccountConnectionString(account, resourceGroupName);

            // Assert
            if (isResourceMode)
            {
                ExpectedNotFoundErrorMessage();
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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
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
            string location = Constants.Location.EastAsia;
            string affinityGroup = null;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);

            CreateAndValidateAccount(accountName, label, description, location, affinityGroup, accountType);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount102_CreateAccount_Location()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Location Setting";
            string affinityGroup = null;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);

            string[] locationsArray = isResourceMode ? Constants.SRPLocations : Constants.Locations;

            foreach (var location in locationsArray)
            {
                CreateAndValidateAccount(accountName, label, description, location, affinityGroup, accountType);
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
            string location = Constants.Location.EastAsia;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string affinityGroup = "TestAffinityGroup";

            try
            {
                AffinityGroupOperationsExtensions.Create(managementClient.AffinityGroups, new AffinityGroupCreateParameters(affinityGroup, "AffinityGroupLabel", location));
                CreateAndValidateAccount(accountName, label, description, isResourceMode ? location : null, affinityGroup, accountType);
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

                string accountName = accountUtils.GenerateAccountName();
                string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode);
                CreateAndValidateAccount(accountName, label, description, location, affinityGroup, accountUtils.mapAccountType(accountType));
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
            string location = Constants.Location.EastAsia;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(agent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location),
                        string.Format("Creating an storage account {0} in location {1} with the same properties with an existing account should succeed", accountName, location));

                    string newLocation = Constants.Location.WestUS;
                    Test.Assert(!agent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, newLocation),
                        string.Format("Creating an existing storage account {0} in location {1} should fail", accountName, newLocation));
                    ExpectedContainErrorMessage(string.Format("Invalid Resource location '{0}'. The Resource already exists in location '{1}'", newLocation, location.Replace(" ", "").ToLower()));

                    string newType = accountUtils.mapAccountType(Constants.AccountType.Standard_RAGRS);;
                    Test.Assert(!agent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, newType, location),
                        string.Format("Creating an existing storage account {0} in location {1} should fail", accountName, newLocation));
                    ExpectedContainErrorMessage(string.Format("The storage account named {0} already exists under the subscription", accountName));
                }
                else
                {
                    string subscriptionId = Test.Data.Get("AzureSubscriptionID");
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Negative Case";
                    string affinityGroup = null;
                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                    Test.Assert(!agent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
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
            string accoutLocation = Constants.Location.EastAsia;
            string groupLocation = Constants.Location.SoutheastAsia;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
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
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount107_CreateAccount_NonExistingAffinityGroup()
        {
            string accountName = accountUtils.GenerateAccountName();
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Non-Existing Affinity Group";
            string location = Constants.Location.EastAsia;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string affinityGroup = FileNamingGenerator.GenerateNameFromRange(15, validNameRange);

            if (isResourceMode)
            {
                Test.Assert(agent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location),
                    string.Format("Creating existing stoarge account {0} in location {1} should succeed", accountName, location));
            }
            else
            {
                Test.Assert(!agent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
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
                Test.Assert(!agent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location),
                    string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
                ExpectedContainErrorMessage(string.Format("The provided location '{0}' is not permitted for subscription.", location));
            }
            else
            {
                Test.Assert(!agent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
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
            string location = Constants.Location.EastAsia;
            string accountType = FileNamingGenerator.GenerateNameFromRange(8, validNameRange);
            string affinityGroup = null;

            if (isResourceMode)
            {
                Test.Assert(!agent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location),
                    string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
            }
            else
            {
                Test.Assert(!agent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType),
                    string.Format("Creating existing stoarge account {0} in location {1} should fail", accountName, location));
            }

            string errorMessageFormat = Language.PowerShell == lang ? PSHInvalidAccountTypeError : NodeJSInvalidTypeError;
            ExpectedContainErrorMessage(string.Format(errorMessageFormat, accountType));
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount201_SetAccount_ChangeType_LabeL_Description()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = FileNamingGenerator.GenerateNameFromRange(15, validNameRange);
            string description = FileNamingGenerator.GenerateNameFromRange(20, validNameRange);
            string originalAccountType = accountUtils.mapAccountType(Constants.AccountType.Standard_GRS);
            string newAccountType = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);

            SetAndValidateAccount(accountName, originalAccountType, label, description, newAccountType);
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
                Test.Assert(!agent.SetSRPAzureStorageAccount(resourceGroupName, accountName, accountType),
                    string.Format("Setting non-existing stoarge account {0} should fail", accountName));
                ExpectedNotFoundErrorMessage();
            }
            else
            {
                Test.Assert(!agent.SetAzureStorageAccount(accountName, label, description, accountType),
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

            if (isResourceMode)
            {
                Test.Assert(!agent.SetSRPAzureStorageAccount(resourceGroupName, accountName, nonExistingType),
                    string.Format("Setting stoarge account {0} to type {1} should fail", accountName, nonExistingType));
            }
            else
            {
                Test.Assert(!agent.SetAzureStorageAccount(accountName, label, description, nonExistingType),
                    string.Format("Setting stoarge account {0} to type {1} should fail", accountName, nonExistingType));
            }


            string errorMessageFormat = Language.PowerShell == lang ? PSHInvalidAccountTypeError : NodeJSInvalidTypeError;
            ExpectedContainErrorMessage(string.Format(errorMessageFormat, nonExistingType));
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount204_SetAccount_ZRSToOthers()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Set Type";
            string location = Constants.Location.EastAsia;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_ZRS);
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
                        errorMsg = string.Format("Storage account type cannot be changed from Standard-ZRS to {0} or vice versa", info.Name.Replace('_', '-'));
                        Test.Assert(!agent.SetSRPAzureStorageAccount(resourceGroupName, accountName, newAccountType),
                            string.Format("Setting storage account {0} to type {1} should fail", accountName, newAccountType));
                    }
                    else
                    {
                        errorMsg = string.Format("Cannot change storage account type from Standard_ZRS to {0} or vice versa", info.Name);
                        Test.Assert(!agent.SetAzureStorageAccount(accountName, label, description, newAccountType),
                            string.Format("Setting stoarge account {0} to type {1} should fail", accountName, newAccountType));
                    }

                    if (newAccountType == accountUtils.mapAccountType(Constants.AccountType.Standard_ZRS))
                    {
                        errorMsg = Language.PowerShell == lang ?
                            "Storage account type Standard-ZRS cannot be changed." :
                            string.Format(NodeJSInvalidTypeError, newAccountType);
                    }
                    else if (newAccountType == accountUtils.mapAccountType(Constants.AccountType.Premium_LRS))
                    {
                        errorMsg = Language.PowerShell == lang ?
                            "The AccountType Premium_LRS is invalid." :
                            string.Format(NodeJSInvalidTypeError, newAccountType);
                    }
                    else
                    {
                        ExpectedContainErrorMessage(errorMsg);
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
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NodeJSServiceAccount)]
        [TestCategory(CLITag.NodeJSResourceAccount)]
        public void FTAccount205_SetAccount_PLRSToOthers()
        {
            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Set Type";
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Premium_LRS);
            string location = accountUtils.GenerateAccountLocation(accountType, false);
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
                        errorMsg = string.Format("Storage account type cannot be changed from Provisioned-LRS to {0} or vice versa", info.Name.Replace('_', '-'));
                        Test.Assert(!agent.SetSRPAzureStorageAccount(resourceGroupName, accountName, newAccountType),
                            string.Format("Setting stoarge account {0} to type {1} should fail", accountName, newAccountType));
                    }
                    else
                    {
                        errorMsg = string.Format("Cannot change storage account type from Premium_LRS to {0} or vice versa", info.Name);
                        Test.Assert(!agent.SetAzureStorageAccount(accountName, label, description, newAccountType),
                            string.Format("Setting stoarge account {0} to type {1} should fail", accountName, newAccountType));
                    }

                    if (newAccountType == accountUtils.mapAccountType(Constants.AccountType.Standard_ZRS) ||
                        newAccountType == accountUtils.mapAccountType(Constants.AccountType.Premium_LRS))
                    {
                        ExpectedContainErrorMessage(string.Format("Invalid value: {0}. Options are: LRS,GRS,RAGRS", newAccountType));
                    }
                    else
                    {
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
            string accountName = accountUtils.GenerateAccountName();
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Set Type";
            string location = Constants.Location.EastAsia;
            string accountType = accountUtils.mapAccountType(Constants.AccountType.Standard_LRS);

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

                if (isResourceMode)
                {
                    Test.Assert(!agent.SetSRPAzureStorageAccount(resourceGroupName, accountName, type),
                        string.Format("Setting stoarge account {0} to type {1} should fail", accountName, type));
                }
                else
                {
                    Test.Assert(!agent.SetAzureStorageAccount(accountName, label, description, type),
                        string.Format("Setting stoarge account {0} to type {1} should fail", accountName, type));
                }

                string errorMessage = string.Format("Invalid value: {0}. Options are: LRS,GRS,RAGRS", type);

                if (Language.PowerShell == lang)
                {
                    if (string.Equals(Constants.AccountType.Standard_ZRS, type))
                    {
                        errorMessage = "Storage account type cannot be changed from Standard-LRS to Standard-ZRS or vice versa.";
                    }

                    if (string.Equals(Constants.AccountType.Premium_LRS, type))
                    {
                        errorMessage = "The AccountType Premium_LRS is invalid.";
                    }
                }

                ExpectedContainErrorMessage(errorMessage);
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
            string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode));
            string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(agent.DeleteSRPAzureStorageAccount(resourceGroupName, accountName),
                        string.Format("Deleting stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;

                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                    Test.Assert(agent.DeleteAzureStorageAccount(accountName),
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
                Test.Assert(agent.DeleteSRPAzureStorageAccount(resourceGroupName, accountName),
                    string.Format("Deleting stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
            }
            else
            {
                Test.Assert(!agent.DeleteAzureStorageAccount(accountName),
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
                Test.Assert(!agent.DeleteSRPAzureStorageAccount(nonExsitingGroupName, accountName),
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
            string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode));
            string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(agent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                        string.Format("Showing stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;

                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                    Test.Assert(agent.ShowAzureStorageAccount(accountName),
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
                    accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode));
                    accountName = accountUtils.GenerateAccountName();
                    location = accountUtils.GenerateAccountLocation(accountType, isResourceMode);

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

                string message = "Listing all stoarge accounts in the subsription should succeed";
                if (isResourceMode)
                {
                    Test.Assert(agent.ShowSRPAzureStorageAccount(string.Empty, string.Empty), message);
                    Test.Assert(agent.ShowSRPAzureStorageAccount(resourceGroupName, string.Empty),
                        string.Format("Listing all stoarge accounts in the resource group {0} should succeed", resourceGroupName));
                }
                else
                {
                    Test.Assert(agent.ShowAzureStorageAccount(string.Empty), message);
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
                Test.Assert(!agent.ShowSRPAzureStorageAccount(nonExsitingGroupName, accountName),
                    string.Format("Showing non-existing stoarge account {0} in non-existing resource group {1} should fail", accountName, nonExsitingGroupName));

                Test.Assert(!agent.ShowSRPAzureStorageAccount(nonExsitingGroupName, string.Empty),
                    string.Format("Listing stoarge accounts in non-existing resource group {0} should fail", nonExsitingGroupName));

                Test.Assert(!agent.ShowSRPAzureStorageAccount(resourceGroupName, accountName),
                    string.Format("Showing non-existing stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
            }
            else
            {
                Test.Assert(!agent.ShowAzureStorageAccount(accountName),
                    string.Format("Showing stoarge accounts {0} should fail", accountName));
            }

            try
            {
                if (isResourceMode)
                {
                    string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode));
                    string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode);
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(!agent.ShowSRPAzureStorageAccount(nonExsitingGroupName, accountName),
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
            string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode));
            string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(agent.ShowSRPAzureStorageAccountKeys(resourceGroupName, accountName),
                        string.Format("Showing keys of the stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;

                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);
                    Test.Assert(agent.ShowAzureStorageAccountKeys(accountName),
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
                Test.Assert(!agent.ShowSRPAzureStorageAccountKeys(nonExsitingGroupName, accountName),
                    string.Format("Listing keys of the non-existing stoarge account {0} in non-existing resource group {1} should fail", accountName, nonExsitingGroupName));
                ExpectedContainErrorMessage(string.Format("Resource group '{0}' could not be found", nonExsitingGroupName));

                Test.Assert(!agent.ShowSRPAzureStorageAccountKeys(resourceGroupName, accountName),
                    string.Format("Listint keys of the non-existing stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
                ExpectedNotFoundErrorMessage();
            }
            else
            {
                Test.Assert(!agent.ShowAzureStorageAccountKeys(accountName),
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
            string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode));
            string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode);

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    Test.Assert(agent.RenewSRPAzureStorageAccountKeys(resourceGroupName, accountName, Constants.AccountKeyType.Primary),
                        string.Format("Renewing the primary key of the stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                    Test.Assert(agent.RenewSRPAzureStorageAccountKeys(resourceGroupName, accountName, Constants.AccountKeyType.Secondary),
                        string.Format("Renewing the secondary key of the stoarge account {0} in resource group {1} should succeed", accountName, resourceGroupName));
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;
                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);

                    Test.Assert(agent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Primary),
                        string.Format("Renewing the primary key of the stoarge account {0} should succeed", accountName));
                    Test.Assert(agent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Secondary),
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
                Test.Assert(!agent.RenewSRPAzureStorageAccountKeys(nonExsitingGroupName, accountName, Constants.AccountKeyType.Primary),
                    string.Format("Renewing the primary key of the non-existing stoarge account {0} in non-existing resource group {1} should fail", accountName, nonExsitingGroupName));
                ExpectedContainErrorMessage(string.Format("Resource group '{0}' could not be found", nonExsitingGroupName));

                Test.Assert(!agent.RenewSRPAzureStorageAccountKeys(resourceGroupName, accountName, Constants.AccountKeyType.Secondary),
                    string.Format("Renewing the secondary key of the non-existing stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
                ExpectedNotFoundErrorMessage();
            }
            else
            {
                Test.Assert(!agent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Primary),
                    string.Format("Renewing the primary key of the stoarge account {0} should fail", accountName));
                ExpectedContainErrorMessage(string.Format("The storage account '{0}' was not found", accountName));

                Test.Assert(!agent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Secondary),
                    string.Format("Renewing the secondary key of the stoarge account {0} should fail", accountName));
                ExpectedContainErrorMessage(string.Format("The storage account '{0}' was not found", accountName));
            }

            try
            {
                string accountType = accountUtils.mapAccountType(accountUtils.GenerateAccountType(isResourceMode));
                string location = accountUtils.GenerateAccountLocation(accountType, isResourceMode);
                accountName = accountUtils.GenerateAccountName();

                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);

                    bool succeeded = agent.RenewSRPAzureStorageAccountKeys(resourceGroupName, accountName, Constants.AccountKeyType.Invalid);

                    if (lang == Language.NodeJS)
                    {
                        Test.Assert(succeeded && agent.Output.Count == 0,
                            string.Format("Renewing an invalid key type of the stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
                    }
                    else
                    {
                        Test.Assert(!succeeded,
                            string.Format("Renewing an invalid key type of the stoarge account {0} in resource group {1} should fail", accountName, resourceGroupName));
                    }
                }
                else
                {
                    string label = "StorageAccountLabel";
                    string description = "Storage Account Test Set Type";
                    string affinityGroup = string.Empty;
                    CreateNewAccount(accountName, label, description, location, affinityGroup, accountType);

                    Test.Assert(!agent.RenewAzureStorageAccountKeys(accountName, Constants.AccountKeyType.Invalid) && agent.Output.Count == 0,
                        string.Format("Renewing an invalid key type of the stoarge account {0} should fail", accountName));
                }
            }
            finally
            {
                DeleteAccountWrapper(accountName);
            }
        }

        protected void SetActiveSubscription()
        {
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
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
        }

        private void CreateAndValidateAccount(string accountName, string label, string description, string location, string affinityGroup, string accountType, bool? geoReplication = null)
        {
            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, accountType);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, accountType);
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

        private void SetAndValidateAccount(string accountName, string originalAccountType, string newLabel, string newDescription, string newAccountType, bool? geoReplication = null)
        {
            string location = Constants.Location.EastAsia;
            string affinityGroup = null;
            string label = "StorageAccountLabel";
            string description = "Storage Account Test Setting";

            try
            {
                if (isResourceMode)
                {
                    CreateNewSRPAccount(accountName, location, originalAccountType);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, location, originalAccountType);

                    WaitForAccountAvailableToSet();

                    SetSRPAccount(accountName, newAccountType);
                    accountUtils.ValidateSRPAccount(resourceGroupName, accountName, null, newAccountType);
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

            Test.Assert(agent.CreateAzureStorageAccount(accountName, subscriptionId, label, description, location, affinityGroup, accountType, geoReplication),
                string.Format("Creating stoarge account {0} in location {1} should succeed", accountName, location));
        }

        private void SetAccount(string accountName, string newLabel, string newDescription, string newAccountType, bool? geoReplication = null)
        {
            StorageAccountGetResponse response = accountUtils.StorageClient.StorageAccounts.Get(accountName);
            Test.Assert(response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            Test.Assert(agent.SetAzureStorageAccount(accountName, newLabel, newDescription, newAccountType, geoReplication),
                string.Format("Creating stoarge account {0} with type {1} should succeed", accountName, newAccountType));
        }

        private void ValidateAccount(string accountName, string label, string description, string location, string affinityGroup, string accountType, bool? geoReplication = null)
        {
            StorageAccountGetResponse response = accountUtils.StorageClient.StorageAccounts.Get(accountName);
            Test.Assert(response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            StorageAccount account = response.StorageAccount;
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
        private void CreateNewSRPAccount(string accountName, string location, string accountType)
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

            Test.Assert(agent.CreateSRPAzureStorageAccount(resourceGroupName, accountName, accountType, location),
                string.Format("Creating storage account {0} in the resource group {1} at location {2} should succeed", accountName, resourceGroupName, location));
        }

        private void SetSRPAccount(string accountName, string newAccountType)
        {
            SRPModel.StorageAccountGetPropertiesResponse response = accountUtils.SRPStorageClient.StorageAccounts.GetPropertiesAsync(resourceGroupName, accountName, CancellationToken.None).Result;
            Test.Assert(response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            Test.Assert(agent.SetSRPAzureStorageAccount(resourceGroupName, accountName, newAccountType),
                string.Format("Setting storage account {0} in resource group {1} to type {2} should succeed", accountName, resourceGroupName, newAccountType));
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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;
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
            NodeJSAgent nodeAgent = (NodeJSAgent)agent;

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

        private enum ServiceType { Blob, Queue, Table, File }

        private enum ErrorType { Empty, Invalid, Unsupported }
    }
}

