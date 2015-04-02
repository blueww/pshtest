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
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using MS.Test.Common.MsTestLib;

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
            if (lang == Language.PowerShell)
            {
                // import module
                string moduleFilePath = Test.Data.Get("ModuleFilePath");
                if (!string.IsNullOrWhiteSpace(moduleFilePath))
                {
                    PowerShellAgent.ImportModule(moduleFilePath);
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
    }
}