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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Commands.Storage.Model.ResourceModel;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using MS.Test.Common.MsTestLib;
using Newtonsoft.Json.Linq;

namespace Management.Storage.ScenarioTest.Common
{
    /// <summary>
    /// general settings for container related tests
    /// </summary>
    public abstract class TestBase
    {
        protected static bool useHttps = false;
        protected static CloudBlobUtil blobUtil;
        protected static CloudQueueUtil queueUtil;
        protected static CloudTableUtil tableUtil;
        protected static CloudFileUtil fileUtil;
        protected static CloudStorageAccount StorageAccount;
        protected static Random random;
        ////private static int ContainerInitCount = 0;
        ////private static int QueueInitCount = 0;
        ////private static int TableInitCount = 0;

        public const string ConfirmExceptionMessage = "The host was attempting to request confirmation";

        protected Agent agent;
        protected static Language lang;
        protected static bool isResourceMode = false;
        protected static bool isMooncake = false;
        private bool isLogin = false;
        private bool accountImported = false;

        private TestContext testContextInstance;

        private static string specialChars = null;
        public static string SpecialChars
        {
            get
            {
                if (string.IsNullOrEmpty(specialChars))
                {
                    try
                    {
                        specialChars = Test.Data.Get("SpecialChars");
                    }
                    catch (ArgumentException)
                    {
                        // nothing to do if cannot find it
                    }
                    if (string.IsNullOrEmpty(specialChars))
                    {
                        specialChars = @"~!@#$%^&*()_+[]'";
                    }
                }
                return specialChars;
            }
        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes

        /// <summary>
        /// Use ClassInitialize to run code before running the first test in the class
        /// the derived class should use it's custom class initialize
        /// first init common bvt
        /// second set storage context in powershell
        /// </summary>
        /// <param name="testContext">Test context object</param>
        [ClassInitialize()]
        public static void TestClassInitialize(TestContext testContext)
        {
            Test.Info(string.Format("{0} Class Initialize", testContext.FullyQualifiedTestClassName));
            Test.FullClassName = testContext.FullyQualifiedTestClassName;

            //add the common initialization
            if (StorageAccount == null)
            {
                StorageAccount = GetCloudStorageAccountFromConfig(useHttps: useHttps);
                Test.Info("Got storage account from config: {0}", StorageAccount.ToString(true));
            }

            //init the blob helper for blob related operations
            blobUtil = new CloudBlobUtil(StorageAccount);
            queueUtil = new CloudQueueUtil(StorageAccount);
            tableUtil = new CloudTableUtil(StorageAccount);
            fileUtil = new CloudFileUtil(StorageAccount);
            random = new Random();

            SetCLIEnv(testContext);
        }

        protected static void SetCLIEnv(TestContext testContext)
        {
            //add the language specific initialization
            lang = AgentFactory.GetLanguage(testContext.Properties);

            isMooncake = Utility.GetTargetEnvironment().Name == "AzureChinaCloud";

            string mode = Test.Data.Get("IsResourceMode");

            if (!string.IsNullOrEmpty(mode))
            {
                isResourceMode = bool.Parse(mode);
            }

            if (lang == Language.PowerShell)
            {
                if (!bool.Parse(Test.Data.Get("IsPowerShellGet")))
                {
                    string moduleFileFolder = Test.Data.Get("ModuleFileFolder");

                    if (!string.IsNullOrWhiteSpace(moduleFileFolder))
                    {
                        if (isResourceMode)
                        {
                            foreach (var resourceModulePath in Constants.ResourceModulePaths)
                            {
                                PowerShellAgent.ImportModule(Path.Combine(moduleFileFolder, resourceModulePath));
                            }
                        }
                        else
                        {
                            PowerShellAgent.ImportModule(Path.Combine(moduleFileFolder, Constants.ServiceModulePath));
                        }
                    }
                }
                else
                {
                    if (isResourceMode)
                    {
                        PowerShellAgent.ImportModule("AzureRm.Profile");
                        PowerShellAgent.ImportModule("Azure.Storage");
                        PowerShellAgent.ImportModule("AzureRm.Storage");
                    }
                    else
                    {
                        PowerShellAgent.ImportModule("Azure");
                    }
                }

                string snapInName = Test.Data.Get("PSSnapInName");
                if (!string.IsNullOrWhiteSpace(snapInName))
                {
                    PowerShellAgent.AddSnapIn(snapInName);
                }

                //set the default storage context
                PowerShellAgent.SetStorageContext(StorageAccount.ToString(true));
            }
            else if (lang == Language.NodeJS)
            {
                NodeJSAgent.GetOSConfig(Test.Data);

                // use ConnectionString parameter by default in function test
                NodeJSAgent.AgentConfig.ConnectionString = StorageAccount.ToString(true);

                FileUtil.GetOSConfig(Test.Data);
            }
        }

        //
        //Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void TestClassCleanup()
        {
            int count = blobUtil.GetExistingContainerCount();

            // FIXME: For now, the new storage account could not work against
            // normal storage account. So comment these operations which has
            // nothing to do with cloud file service and will certainly fail.
            ////string message = string.Format("there are {0} containers before running mutiple unit tests, after is {1}", ContainerInitCount, count);
            ////AssertCleanupOnStorageObject("containers", ContainerInitCount, count);

            ////count = queueUtil.GetExistingQueueCount();
            ////AssertCleanupOnStorageObject("queues", QueueInitCount, count);

            ////count = tableUtil.GetExistingTableCount();

            ////AssertCleanupOnStorageObject("tables", TableInitCount, count);

            Test.Info("Test Class Cleanup");
        }

        private static void AssertCleanupOnStorageObject(string name, int initCount, int cleanUpCount)
        {
            string message = string.Format("there are {0} {1} before running mutiple unit tests, after is {2}", initCount, name, cleanUpCount);

            if (initCount == cleanUpCount)
            {
                Test.Info(message);
            }
            else
            {
                Test.Warn(message);
            }
        }

        /// <summary>
        /// Get Cloud storage account from Test.xml
        /// </summary>
        /// <param name="configKey">Config key. Will return the default storage account when it's empty.</param>
        /// <param name="useHttps">Use https or not</param>
        /// <returns>Cloud Storage Account with specified end point</returns>
        public static CloudStorageAccount GetCloudStorageAccountFromConfig(string configKey = "", bool useHttps = true)
        {
            string StorageAccountName = Test.Data.Get(string.Format("{0}StorageAccountName", configKey));
            string StorageAccountKey = Test.Data.Get(string.Format("{0}StorageAccountKey", configKey));
            string StorageEndPoint = Test.Data.Get(string.Format("{0}StorageEndPoint", configKey));
            StorageCredentials credential = new StorageCredentials(StorageAccountName, StorageAccountKey);
            return Utility.GetStorageAccountWithEndPoint(credential, useHttps, StorageEndPoint);
        }

        /// <summary>
        /// on test setup
        /// the derived class could use it to run it owned set up settings.
        /// </summary>
        public virtual void OnTestSetup()
        {
            if (isResourceMode)
            {
                if (!isLogin)
                {
                    if (Utility.GetAutoLogin())
                    {
                        int retry = 0;
                        do
                        {
                            if (agent.HadErrors)
                            {
                                Thread.Sleep(5000);
                                Test.Info(string.Format("Retry login... Count:{0}", retry));
                            }
                            if (!TestContext.FullyQualifiedTestClassName.Contains("SubScriptionBVT")) //For SubScriptionBVT, we already login and set current account, don't need re-login
                            {
                                agent.Logout();
                                agent.Login();
                            }
                        }
                        while (agent.HadErrors && retry++ < 5);
                    }

                    if (lang == Language.NodeJS)
                    {
                        SetActiveSubscription();
                        agent.ChangeCLIMode(Constants.Mode.arm);
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
                        nodeAgent.Logout();
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

        /// <summary>
        /// on test clean up
        /// the derived class could use it to run it owned clean up settings.
        /// </summary>
        public virtual void OnTestCleanUp()
        {
            if (lang == Language.NodeJS)
            {
                NodeJSAgent.AgentConfig.SAS = string.Empty;

                if (string.IsNullOrEmpty(NodeJSAgent.AgentConfig.AccountKey))
                {
                    NodeJSAgent.AgentConfig.AccountName = string.Empty;
                }
            }
        }

        /// <summary>
        /// test initialize
        /// </summary>
        [TestInitialize()]
        public virtual void InitAgent()
        {
            agent = AgentFactory.CreateAgent(TestContext.Properties);
            if (Agent.Context == null)
            {
                Agent.Context = StorageAccount;
            }
            Test.Start(TestContext.FullyQualifiedTestClassName, TestContext.TestName);
            OnTestSetup();
        }

        /// <summary>
        /// test clean up
        /// </summary>
        [TestCleanup()]
        public void CleanAgent()
        {
            OnTestCleanUp();
            agent = null;
            Test.End(TestContext.FullyQualifiedTestClassName, TestContext.TestName);
        }

        #endregion

        public delegate void Validator(string s);

        public void ExpectedNotFoundErrorMessage()
        {
            ExpectedContainErrorMessage("Resource not found");
        }

        public void ExpectedAccoutNotFoundErrorMessage(string groupName, string accountName)
        {
            ExpectedContainErrorMessage(string.Format("The Resource 'Microsoft.Storage/storageAccounts/{0}' under resource group '{1}' was not found", accountName, groupName));
        }

        /// <summary>
        /// Expect returned error message is the specified error message
        /// </summary>
        /// <param name="expectErrorMessage">Expect error message</param>
        public void ExpectedEqualErrorMessage(string expectErrorMessage)
        {
            Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");

            if (agent.ErrorMessages.Count == 0)
            {
                return;
            }

            Test.Assert(expectErrorMessage == agent.ErrorMessages[0], String.Format("Expected error message: {0}, and actually it's {1}", expectErrorMessage, agent.ErrorMessages[0]));
        }

        /// <summary>
        /// Expect returned error message starts with the specified error message
        /// </summary>
        /// <param name="expectErrorMessage">Expect error message</param>
        public void ExpectedStartsWithErrorMessage(string errorMessage)
        {
            Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");

            if (agent.ErrorMessages.Count == 0)
            {
                return;
            }

            Test.Assert(agent.ErrorMessages[0].StartsWith(errorMessage), String.Format("Expected error message should start with {0}, and actually it's {1}", errorMessage, agent.ErrorMessages[0]));
        }

        /// <summary>
        /// Expect returned error message contain the specified error message
        /// </summary>
        /// <param name="errorMessage">Expected error message</param>
        public void ExpectedContainErrorMessage(string errorMessage)
        {
            Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");

            if (agent.ErrorMessages.Count == 0)
            {
                return;
            }

            Test.Assert(agent.ErrorMessages[0].IndexOf(errorMessage) != -1, String.Format("Expected error message should contain '{0}', and actually it's '{1}'", errorMessage, agent.ErrorMessages[0]));
        }

        /// <summary>
        /// Expect returned error message contain the specified error message
        /// </summary>
        /// <param name="errorMessages">list of expect error message</param>
        public void ExpectedContainErrorMessage(string[] errorMessages)
        {
            Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");

            if (agent.ErrorMessages.Count == 0)
            {
                return;
            }

            bool expected = false;
            foreach (var errorMsg in errorMessages)
            {
                if (agent.ErrorMessages[0].IndexOf(errorMsg) != -1)
                {
                    expected = true;
                    break;
                }
            }
            Test.Assert(expected, String.Format("Current error msg '{0}' should be in the expected msg list.", agent.ErrorMessages[0]));
        }

        protected PSCorsRule[] GetCORSRulesFromOutput()
        {
            if (lang == Language.PowerShell)
            {
                return agent.Output[0][PowerShellAgent.BaseObject] as PSCorsRule[];
            }
            else
            {
                List<PSCorsRule> rules = new List<PSCorsRule>();
                for (int i = 0; i < agent.Output.Count; i++)
                {
                    PSCorsRule rule = new PSCorsRule();

                    bool hasValue = false;
                    JArray categories;
                    string key = "AllowedMethods";
                    if (agent.Output[i].ContainsKey(key))
                    {
                        categories = (JArray)agent.Output[i][key];
                        rule.AllowedMethods = categories.Select(c => (string)c).ToArray();
                        hasValue = true;
                    }

                    key = "AllowedOrigins";
                    if (agent.Output[i].ContainsKey(key))
                    {
                        categories = (JArray)agent.Output[i][key];
                        rule.AllowedOrigins = categories.Select(c => (string)c).ToArray();
                        hasValue = true;
                    }

                    key = "AllowedHeaders";
                    if (agent.Output[i].ContainsKey(key))
                    {
                        categories = (JArray)agent.Output[i][key];
                        rule.AllowedHeaders = categories.Select(c => (string)c).ToArray();
                        hasValue = true;
                    }

                    key = "ExposedHeaders";
                    if (agent.Output[i].ContainsKey(key))
                    {
                        categories = (JArray)agent.Output[i][key];
                        rule.ExposedHeaders = categories.Select(c => (string)c).ToArray();
                        hasValue = true;
                    }

                    key = "MaxAgeInSeconds";
                    if (agent.Output[i].ContainsKey(key))
                    {
                        rule.MaxAgeInSeconds = (int)(agent.Output[i][key] as long?);
                        hasValue = true;
                    }

                    if (hasValue)
                    {
                        rules.Add(rule);
                    }
                }

                return rules.ToArray();
            }
        }

        /// <summary>
        /// Expect two string are equal
        /// </summary>
        /// <param name="expect">expect string</param>
        /// <param name="actually">returned string</param>
        /// <param name="name">Compare name</param>
        public static void ExpectEqual(string expect, string actually, string name)
        {
            Test.Assert(expect == actually, string.Format("{0} should be {1}, and actually it's {2}", name, expect, actually));
        }

        /// <summary>
        /// Expect two double are equal
        /// </summary>
        /// <param name="expect">expect double</param>
        /// <param name="actually">returned double</param>
        /// <param name="name">Compare name</param>
        public static void ExpectEqual(double expect, double actually, string name)
        {
            Test.Assert(expect == actually, string.Format("{0} should be {1}, and actually it's {2}", name, expect, actually));
        }

        /// <summary>
        /// Expect two string are not equal
        /// </summary>
        /// <param name="expect">expect string</param>
        /// <param name="actually">returned string</param>
        /// <param name="name">Compare name</param>
        public static void ExpectNotEqual(string expect, string actually, string name)
        {
            Test.Assert(expect != actually, string.Format("{0} should not be {1}, and actually it's {2}", name, expect, actually));
        }

        /// <summary>
        /// Expect two double are not equal
        /// </summary>
        /// <param name="expect">expect double</param>
        /// <param name="actually">returned double</param>
        /// <param name="name">Compare name</param>
        public static void ExpectNotEqual(double expect, double actually, string name)
        {
            Test.Assert(expect != actually, string.Format("{0} should not be {1}, and actually it's {2}", name, expect, actually));
        }

        public static CloudStorageAccount GetStorageAccountWithSasToken(string accountName, string sastoken,
            bool useHttps = true, string endpoint = "")
        {
            StorageCredentials credentials = new StorageCredentials(sastoken);
            return Utility.GetStorageAccountWithEndPoint(credentials, useHttps, endpoint, accountName);
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
    }
}