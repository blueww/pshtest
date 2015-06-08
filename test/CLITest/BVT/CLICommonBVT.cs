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

namespace Management.Storage.ScenarioTest.BVT
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using CloudStorageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount;
    using Constants = Management.Storage.ScenarioTest.Constants;
    using StorageBlob = Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// this class contain all the bvt cases for` the full functional storage context such as local/connectionstring/namekey, anonymous and sas token are excluded.
    /// </summary>
    public abstract partial class CLICommonBVT : TestBase
    {
        private static CloudBlobHelper CommonBlobHelper;
        private static CloudStorageAccount CommonStorageAccount;
        private static string CommonBlockFilePath;
        private static string CommonPageFilePath;
        private static string CommonAppendFilePath;
        private static string CommonSmallFilePath;
        private static string CommonMediumFilePath;
        private static string SmallFileMD5;
        private static string MediumFileMD5;

        //env connection string
        private static string SavedEnvString;
        public static string EnvKey;

        /// <summary>
        /// the storage account which is used to set up the unit tests.
        /// </summary>
        protected static CloudStorageAccount SetUpStorageAccount
        {
            get
            {
                return CommonStorageAccount;
            }

            set
            {
                CommonStorageAccount = value;
            }
        }

        #region Additional test attributes

        /// <summary>
        /// Init test resources for bvt class
        /// </summary>
        /// <param name="testContext">TestContext object</param>
        public static void CLICommonBVTInitialize(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            //add the common initialization
            EnvKey = Test.Data.Get("EnvContextKey");

            //init the blob helper for blob related operations
            if (CommonStorageAccount == null)
            {
                CommonStorageAccount = TestBase.StorageAccount;
            }

            CommonBlobHelper = new CloudBlobHelper(CommonStorageAccount);

            //add the language specific initialization
            lang = AgentFactory.GetLanguage(testContext.Properties);
            if (lang == Language.PowerShell)
            {
                SaveAndCleanSubScriptionAndEnvConnectionString();

                //Clean Storage Context
                Test.Info("Clean storage context in PowerShell");
                PowerShellAgent.CleanStorageContext();
            }

            GenerateBvtTempFiles();

            // initialize file utility
            fileUtil = new CloudFileUtil(CommonStorageAccount);
        }

        /// <summary>
        /// Save azure subscription and env connection string. So the current settings can't impact our tests.
        /// </summary>
        //TODO move to TestBase
        public static void SaveAndCleanSubScriptionAndEnvConnectionString()
        {
            Test.Info("Clean Azure Subscription and save env connection string");

            PowerShellAgent.RemoveAzureSubscriptionIfExists();

            //set env connection string
            //TODO A little bit trivial, move to CLITestBase class
            if (string.IsNullOrEmpty(EnvKey))
            {
                EnvKey = Test.Data.Get("EnvContextKey");
            }

            SavedEnvString = System.Environment.GetEnvironmentVariable(EnvKey);
            System.Environment.SetEnvironmentVariable(EnvKey, string.Empty);
        }

        /// <summary>
        /// Restore the previous subscription and env connection string before testing.
        /// </summary>
        public static void RestoreSubScriptionAndEnvConnectionString()
        {
            Test.Info("Restore env connection string and skip restore subscription");
            if (EnvKey != null)
            {
                Environment.SetEnvironmentVariable(EnvKey, SavedEnvString);
            }
        }

        /// <summary>
        /// Generate temp files
        /// </summary>
        private static void GenerateBvtTempFiles()
        {
            bool AlwaysOperateOnWindows = (FileUtil.AgentOSType != OSType.Windows);

            CommonBlockFilePath = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());
            CommonPageFilePath = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());
            CommonAppendFilePath = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());
            CommonSmallFilePath = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());
            CommonMediumFilePath = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());

            FileUtil.CreateDirIfNotExits(Path.GetDirectoryName(CommonBlockFilePath), AlwaysOperateOnWindows);
            FileUtil.CreateDirIfNotExits(Path.GetDirectoryName(CommonPageFilePath), AlwaysOperateOnWindows);
            FileUtil.CreateDirIfNotExits(Path.GetDirectoryName(CommonAppendFilePath), AlwaysOperateOnWindows);
            FileUtil.CreateDirIfNotExits(Path.GetDirectoryName(CommonSmallFilePath), AlwaysOperateOnWindows);
            FileUtil.CreateDirIfNotExits(Path.GetDirectoryName(CommonMediumFilePath), AlwaysOperateOnWindows);

            FileUtil.CreateDirIfNotExits(Test.Data.Get("DownloadDir"), AlwaysOperateOnWindows);
            FileUtil.CreateDirIfNotExits(Test.Data.Get("UploadDir"), AlwaysOperateOnWindows);

            // Generate block file and page file which are used for uploading
            FileUtil.GenerateMediumFile(CommonBlockFilePath, Utility.GetRandomTestCount(1, 5), AlwaysOperateOnWindows);
            FileUtil.GenerateMediumFile(CommonPageFilePath, Utility.GetRandomTestCount(1, 5), AlwaysOperateOnWindows);
            FileUtil.GenerateMediumFile(CommonAppendFilePath, Utility.GetRandomTestCount(1, 5), AlwaysOperateOnWindows);
            FileUtil.GenerateMediumFile(CommonMediumFilePath, Utility.GetRandomTestCount(5, 10), AlwaysOperateOnWindows);
            FileUtil.GenerateSmallFile(CommonSmallFilePath, Utility.GetRandomTestCount(1, 10), AlwaysOperateOnWindows);
            MediumFileMD5 = FileUtil.GetFileContentMD5(CommonMediumFilePath);
            SmallFileMD5 = FileUtil.GetFileContentMD5(CommonSmallFilePath);
        }

        /// <summary>
        /// Clean up test resources of  bvt class
        /// </summary>
        [ClassCleanup()]
        public static void CLICommonBVTCleanup()
        {
            Test.Info(string.Format("BVT Test Class Cleanup"));
            RestoreSubScriptionAndEnvConnectionString();
        }

        /// <summary>
        /// init test resources for one single unit test.
        /// </summary>
        [TestInitialize()]
        public void UnitTestInitialize()
        {
            Trace.WriteLine("Unit Test Initialize");
            agent = AgentFactory.CreateAgent(TestContext.Properties);
        }

        /// <summary>
        /// clean up the test resources for one single unit test.
        /// </summary>
        [TestCleanup()]
        public void UnitTestCleanup()
        {
            Trace.WriteLine("Unit Test Cleanup");
        }

        #endregion

        /// <summary>
        /// BVT case : for New-AzureStorageContainer
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void NewContainerTest()
        {
            NewContainerTest(agent);
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageContainer
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void GetContainerTest()
        {
            GetContainerTest(agent);
        }

        /// <summary>
        /// BVT case : for Remove-AzureStorageContainer
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void RemoveContainerTest()
        {
            RemoveContainerTest(agent);
        }

        /// <summary>
        /// BVT case : for Set-AzureStorageContainerACL
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void SetContainerACLTest()
        {
            SetContainerACLTest(agent);
        }

        /// <summary>
        /// BVT case : for New-AzureStorageTable
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.Table)]
        [TestCategory(CLITag.NewTable)]
        public void NewTableTest()
        {
            NewTableTest(agent);
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageTable
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.Table)]
        [TestCategory(CLITag.GetTable)]
        public void GetTableTest()
        {
            GetTableTest(agent);
        }

        /// <summary>
        /// BVT case : for Remove-AzureStorageTable
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.Table)]
        [TestCategory(CLITag.RemoveTable)]
        public void RemoveTableTest()
        {
            RemoveTableTest(agent);
        }

        /// <summary>
        /// BVT case : for New-AzureStorageQueue
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.Queue)]
        [TestCategory(CLITag.NewQueue)]
        public void NewQueueTest()
        {
            NewQueueTest(agent);
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageQueue
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.Queue)]
        [TestCategory(CLITag.GetQueue)]
        public void GetQueueTest()
        {
            GetQueueTest(agent);
        }

        /// <summary>
        /// BVT case : for Remove-AzureStorageQueue
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.Queue)]
        [TestCategory(CLITag.RemoveQueue)]
        public void RemoveQueueTest()
        {
            RemoveQueueTest(agent);
        }

        /// <summary>
        /// BVT case : for Set-AzureStorageBlobContent
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void UploadBlobTest()
        {
            UploadBlobTest(agent, CommonBlockFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            UploadBlobTest(agent, CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
            UploadBlobTest(agent, CommonAppendFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob);
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageBlob
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void GetBlobTest()
        {
            GetBlobTest(agent, CommonBlockFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            GetBlobTest(agent, CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
            GetBlobTest(agent, CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob);
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageBlobContent
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void DownloadBlobTest()
        {
            string downloadDirPath = Test.Data.Get("DownloadDir");
            DownloadBlobTest(agent, CommonBlockFilePath, downloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            DownloadBlobTest(agent, CommonPageFilePath, downloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
            DownloadBlobTest(agent, CommonPageFilePath, downloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob);
        }

        /// <summary>
        /// BVT case : for Remove-AzureStorageBlob
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void RemoveBlobTest()
        {
            RemoveBlobTest(agent, CommonBlockFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            RemoveBlobTest(agent, CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
            RemoveBlobTest(agent, CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob);
        }

        /// <summary>
        /// BVT case : for Start-AzureStorageBlobCopy
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void StartCopyBlobUsingName()
        {
            StartCopyBlobTest(false);
        }

        /// <summary>
        /// BVT case : for Start-AzureStorageBlobCopy
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void StartCopyBlobUsingUri()
        {
            StartCopyBlobTest(true);
        }

        /// <summary>
        /// BVT case : for Set-AzureStorageServiceLogging
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.ServiceLogging)]
        public void SetServiceLoggingTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip this case as Azure emulator does not support Get/Set ServiceProperties currently");
                return;
            }

            foreach (Constants.ServiceType serviceType in Enum.GetValues(typeof(Constants.ServiceType)))
            {
                int retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
                string loggingOperations = Utility.GenRandomLoggingOperations();

                ServiceProperties propertiesBeforeSet = Utility.GetServiceProperties(CommonStorageAccount, serviceType);

                // set ServiceProperties(logging)
                Test.Assert(agent.SetAzureStorageServiceLogging(serviceType, loggingOperations, retentionDays.ToString()),
                    Utility.GenComparisonData("SetAzureStorageServiceLogging", true));

                Utility.ValidateLoggingProperties(CommonStorageAccount, serviceType, retentionDays, loggingOperations);

                Utility.ValidateMetricsProperties(CommonStorageAccount, serviceType, Constants.MetricsType.Hour, propertiesBeforeSet.HourMetrics.RetentionDays,
                    propertiesBeforeSet.HourMetrics.MetricsLevel.ToString());
            }
        }

        /// <summary>
        /// BVT case : for Set-AzureStorageServiceHourMetrics
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.ServiceMetrics)]
        public void SetServiceMetricsTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip this case as Azure emulator does not support Get/Set ServiceProperties currently");
                return;
            }

            foreach (Constants.MetricsType metricsType in Enum.GetValues(typeof(Constants.MetricsType)))
            {
                foreach (Constants.ServiceType serviceType in Enum.GetValues(typeof(Constants.ServiceType)))
                {
                    ServiceProperties propertiesBeforeSet = Utility.GetServiceProperties(CommonStorageAccount, serviceType);
                    int retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
                    string metricsLevel = Utility.GenRandomMetricsLevel();
                    // set ServiceProperties(metrics)
                    Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, metricsType, metricsLevel, retentionDays.ToString()),
                        Utility.GenComparisonData("SetAzureStorageServiceHourMetrics", true));

                    Utility.ValidateLoggingProperties(CommonStorageAccount, serviceType, propertiesBeforeSet.Logging.RetentionDays,
                        propertiesBeforeSet.Logging.LoggingOperations.ToString());

                    Utility.ValidateMetricsProperties(CommonStorageAccount, serviceType, metricsType, retentionDays, metricsLevel);
                }
            }
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageServiceLogging
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.ServiceLogging)]
        public void GetServiceLoggingTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip this case as Azure emulator does not support Get/Set ServiceProperties currently");
                return;
            }

            foreach (Constants.ServiceType serviceType in Enum.GetValues(typeof(Constants.ServiceType)))
            {
                Test.Assert(agent.GetAzureStorageServiceLogging(serviceType), Utility.GenComparisonData("GetAzureStorageServiceLogging", true));
                ServiceProperties properties = Utility.GetServiceProperties(CommonStorageAccount, serviceType);
                agent.OutputValidation(properties, "logging");
            }
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageServiceHourMetrics
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.ServiceMetrics)]
        public void GetServiceMetricsTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip this case as Azure emulator does not support Get/Set ServiceProperties currently");
                return;
            }

            foreach (Constants.ServiceType serviceType in Enum.GetValues(typeof(Constants.ServiceType)))
            {
                Test.Assert(agent.GetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour), Utility.GenComparisonData("GetAzureStorageServiceHourMetrics", true));
                ServiceProperties properties = Utility.GetServiceProperties(CommonStorageAccount, serviceType);
                agent.OutputValidation(properties, "HourMetrics");

                Test.Assert(agent.GetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute), Utility.GenComparisonData("GetAzureStorageServiceMinuteMetrics", true));
                properties = Utility.GetServiceProperties(CommonStorageAccount, serviceType);
                agent.OutputValidation(properties, "MinuteMetrics");
            }
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageBlobCopyState
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void GetBlobCopyStateTest()
        {
            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            if (lang == Language.PowerShell)
            {
                blobUtil.SetupTestContainerAndBlob();
            }
            else
            {
                blobUtil.SetupTestContainerAndBlob(blobNamePrefix: "blob");
            }
            CloudBlob destBlob = CopyBlobAndWaitForComplete(blobUtil);

            try
            {
                Test.Assert(destBlob.CopyState.Status == CopyStatus.Success, String.Format("The blob copy using storage client should be success, actually it's {0}", destBlob.CopyState.Status));

                Test.Assert(agent.GetAzureStorageBlobCopyState(blobUtil.ContainerName, destBlob.Name, false), "Get copy state should be success");
                int expectedStateCount = 1;
                Test.Assert(agent.Output.Count == expectedStateCount, String.Format("Expected to get {0} copy state, actually it's {1}", expectedStateCount, agent.Output.Count));

                if (lang == Language.PowerShell)
                {
                    CopyStatus copyStatus = (CopyStatus)agent.Output[0]["Status"];
                    Test.Assert(copyStatus == CopyStatus.Success, String.Format("The blob copy should be success, actually it's {0}", copyStatus));

                    Uri sourceUri = (Uri)agent.Output[0]["Source"];
                    string expectedUri = CloudBlobUtil.ConvertCopySourceUri(blobUtil.Blob.Uri.ToString());
                    Test.Assert(sourceUri.ToString() == expectedUri, String.Format("Expected source uri is {0}, actually it's {1}", expectedUri, sourceUri.ToString()));
                }
                else
                {
                    string copyStatus = (string)agent.Output[0]["copyStatus"];
                    Test.Assert(copyStatus == "success", String.Format("The blob copy should be success, actually it's {0}", copyStatus));

                    string container = (string)agent.Output[0]["container"];
                    string blob = (string)agent.Output[0]["blob"];
                    Test.Assert(container == blobUtil.ContainerName, String.Format("Expected container is {0}, actually it's {1}", blobUtil.ContainerName, container));
                    Test.Assert(blob == destBlob.Name, String.Format("Expected blob is {0}, actually it's {1}", destBlob.Name, blob));
                }

                Test.Assert(!agent.GetAzureStorageBlobCopyState(blobUtil.ContainerName, blobUtil.BlobName, false), "Get copy state should be fail since the specified blob don't have any copy operation");
                Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");
                string errorMessage = "Can not find copy task on specified blob";
                Test.Assert(agent.ErrorMessages[0].StartsWith(errorMessage), String.Format("Error message should start with {0}, and actually it's {1}", errorMessage, agent.ErrorMessages[0]));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// BVT case : for Stop-AzureStorageBlobCopy
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void StopCopyBlobTest()
        {
            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            if (lang == Language.PowerShell)
            {
                blobUtil.SetupTestContainerAndBlob();
            }
            else
            {
                blobUtil.SetupTestContainerAndBlob(blobNamePrefix: "blob");
            }
            CloudBlob destBlob = CopyBlobAndWaitForComplete(blobUtil);

            try
            {
                string copyId = Guid.NewGuid().ToString();
                Test.Assert(!agent.StopAzureStorageBlobCopy(blobUtil.ContainerName, blobUtil.BlobName, copyId, true), "Stop copy operation should be fail since the specified blob don't have any copy operation");
                Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");

                string errorMessage;
                if (lang == Language.PowerShell)
                {
                    errorMessage = String.Format("Can not find copy task on specified blob '{0}' in container '{1}'", blobUtil.BlobName, blobUtil.ContainerName);
                    Test.Assert(agent.ErrorMessages[0].IndexOf(errorMessage) != -1, String.Format("Error message should contain {0}, and actually it's {1}", errorMessage, agent.ErrorMessages[0]));
                }
                else
                {
                    errorMessage = "There is currently no pending copy operation.";
                    Test.Assert(agent.ErrorMessages[0].IndexOf(errorMessage) != -1, String.Format("Error message should contain {0}, and actually it's {1}", errorMessage, agent.ErrorMessages[0]));
                }

                errorMessage = "There is currently no pending copy operation.";
                Test.Assert(!agent.StopAzureStorageBlobCopy(blobUtil.ContainerName, destBlob.Name, copyId, true), "Stop copy operation should be fail since the specified copy operation has finished");
                Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");
                Test.Assert(agent.ErrorMessages[0].IndexOf(errorMessage) != -1, String.Format("Error message should contain {0}, and actually it's {1}", errorMessage, agent.ErrorMessages[0]));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// BVT case : for contaienr show command
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSBVT)]
        public void ShowContainerTest()
        {
            string NewContainerName = Utility.GenNameString("astoria-");

            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, NewContainerName);

            // create container if it does not exist
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NewContainerName);
            container.CreateIfNotExists();

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            NodeJSAgent agent = new NodeJSAgent();
            try
            {
                //--------------Show operation--------------
                Test.Assert(agent.ShowAzureStorageContainer(NewContainerName), Utility.GenComparisonData("ShowAzureStorageContainer", true));
                // Verification for returned values
                container.FetchAttributes();
                dic.Add("ShowContainer", container);
                CloudBlobUtil.PackContainerCompareData(container, dic);

                agent.OutputValidation(comp);
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// BVT case : for blob show command
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSBVT)]
        public void ShowBlobTest()
        {
            ShowBlobTest(CommonBlockFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            ShowBlobTest(CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
            ShowBlobTest(CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob);
        }

        internal void ShowBlobTest(string UploadFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            string NewContainerName = Utility.GenNameString("upload-");
            string BlobName = Path.GetFileName(UploadFilePath);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, BlobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NewContainerName);
            container.CreateIfNotExists();

            NodeJSAgent agent = new NodeJSAgent();
            try
            {
                bool bSuccess = false;
                // upload the blob file
                if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob)
                {
                    bSuccess = CommonBlobHelper.UploadFileToBlockBlob(NewContainerName, BlobName, UploadFilePath);
                }
                else if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob)
                {
                    bSuccess = CommonBlobHelper.UploadFileToPageBlob(NewContainerName, BlobName, UploadFilePath);
                }
                else if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob)
                {
                    bSuccess = CommonBlobHelper.UploadFileToAppendBlob(NewContainerName, BlobName, UploadFilePath);
                }
                Test.Assert(bSuccess, "upload file {0} to container {1} should succeed", UploadFilePath, NewContainerName);

                //--------------Show operation--------------
                Test.Assert(agent.ShowAzureStorageBlob(BlobName, NewContainerName), Utility.GenComparisonData("ShowAzureStorageBlob", true));

                // Verification for returned values
                // get blob object using XSCL 
                CloudBlob blob = CommonBlobHelper.QueryBlob(NewContainerName, BlobName);
                blob.FetchAttributes();
                CloudBlobUtil.PackBlobCompareData(blob, dic);
                dic.Add("ShowBlob", blob);

                agent.OutputValidation(comp);
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        internal CloudBlob CopyBlobAndWaitForComplete(CloudBlobUtil blobUtil)
        {
            string destBlobName = Utility.GenNameString("copystate");

            CloudBlob destBlob = null;

            Test.Info("Copy Blob using storage client");

            switch (blobUtil.Blob.BlobType)
            {
                case StorageBlob.BlobType.BlockBlob:
                    CloudBlockBlob blockBlob = blobUtil.Container.GetBlockBlobReference(destBlobName);
                    blockBlob.StartCopyFromBlob((CloudBlockBlob)blobUtil.Blob);
                    destBlob = blockBlob;
                    break;
                case StorageBlob.BlobType.PageBlob:
                    CloudPageBlob pageBlob = blobUtil.Container.GetPageBlobReference(destBlobName);
                    pageBlob.StartCopyFromBlob((CloudPageBlob)blobUtil.Blob);
                    destBlob = pageBlob;
                    break;
                case StorageBlob.BlobType.AppendBlob:
                    CloudAppendBlob appendBlob = blobUtil.Container.GetAppendBlobReference(destBlobName);
                    appendBlob.StartCopy((CloudAppendBlob)blobUtil.Blob);
                    destBlob = appendBlob;
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Invalid blob type: {0}", blobUtil.Blob.BlobType));
            }

            CloudBlobUtil.WaitForCopyOperationComplete(destBlob);

            Test.Assert(destBlob.CopyState.Status == CopyStatus.Success, String.Format("The blob copy using storage client should be success, actually it's {0}", destBlob.CopyState.Status));

            return destBlob;
        }

        internal void StartCopyBlobTest(bool useUri)
        {
            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            if (lang == Language.PowerShell)
            {
                blobUtil.SetupTestContainerAndBlob();
            }
            else
            {
                blobUtil.SetupTestContainerAndBlob(blobNamePrefix: "blob");
            }
            string copiedName = Utility.GenNameString("copied");

            if (useUri)
            {
                //Set the blob permission, so the copy task could directly copy by uri
                BlobContainerPermissions permission = new BlobContainerPermissions();
                permission.PublicAccess = BlobContainerPublicAccessType.Blob;
                blobUtil.Container.SetPermissions(permission);
            }

            try
            {
                if (useUri)
                {
                    Test.Assert(agent.StartAzureStorageBlobCopy(blobUtil.Blob.Uri.ToString(), blobUtil.ContainerName, copiedName, PowerShellAgent.Context), Utility.GenComparisonData("Start copy blob using source uri", true));
                }
                else
                {
                    Test.Assert(agent.StartAzureStorageBlobCopy(blobUtil.ContainerName, blobUtil.BlobName, blobUtil.ContainerName, copiedName), Utility.GenComparisonData("Start copy blob using blob name", true));
                }

                Test.Info("Get destination blob in copy task");
                CloudBlob blob = StorageExtensions.GetBlobReferenceFromServer(blobUtil.Container, copiedName);
                Test.Assert(blob != null, "Destination blob should exist after start copy. If not, please check it's a test issue or dev issue.");

                string sourceUri = CloudBlobUtil.ConvertCopySourceUri(blobUtil.Blob.Uri.ToString());

                Test.Assert(blob.BlobType == blobUtil.Blob.BlobType, String.Format("The destination blob type should be {0}, actually {1}.", blobUtil.Blob.BlobType, blob.BlobType));

                Test.Assert(blob.CopyState.Source.ToString().StartsWith(sourceUri), String.Format("The source of destination blob should start with {0}, and actually it's {1}", sourceUri, blob.CopyState.Source.ToString()));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        internal void NewContainerTest(Agent agent)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("astoria-");

            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, NEW_CONTAINER_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            // delete container if it exists
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.DeleteIfExists();

            try
            {
                //--------------New operation--------------
                Test.Assert(agent.NewAzureStorageContainer(NEW_CONTAINER_NAME), Utility.GenComparisonData("NewAzureStorageContainer", true));
                // Verification for returned values
                CloudBlobUtil.PackContainerCompareData(container, dic);
                agent.OutputValidation(comp);
                Test.Assert(container.Exists(), "container {0} should exist!", NEW_CONTAINER_NAME);
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        internal void GetContainerTest(Agent agent)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("astoria-");

            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, NEW_CONTAINER_NAME);

            // create container if it does not exist
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            try
            {
                //--------------Get operation--------------
                Test.Assert(agent.GetAzureStorageContainer(NEW_CONTAINER_NAME), Utility.GenComparisonData("GetAzureStorageContainer", true));
                // Verification for returned values
                container.FetchAttributes();
                dic.Add("CloudBlobContainer", container);
                CloudBlobUtil.PackContainerCompareData(container, dic);

                agent.OutputValidation(comp);
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        internal void RemoveContainerTest(Agent agent)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("astoria-");

            // create container if it does not exist
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                //--------------Remove operation--------------
                Test.Assert(agent.RemoveAzureStorageContainer(NEW_CONTAINER_NAME), Utility.GenComparisonData("RemoveAzureStorageContainer", true));
                Test.Assert(!container.Exists(), "container {0} should not exist!", NEW_CONTAINER_NAME);
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        internal void SetContainerACLTest(Agent agent)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("astoria-");

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, NEW_CONTAINER_NAME);
            comp.Add(dic);

            // create container if it does not exist
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                BlobContainerPublicAccessType[] accessTypes = new BlobContainerPublicAccessType[] { 
                    BlobContainerPublicAccessType.Blob,
                    BlobContainerPublicAccessType.Container,
                    BlobContainerPublicAccessType.Off
                };

                // set PublicAccess as one value respetively
                foreach (var accessType in accessTypes)
                {
                    //--------------Set operation-------------- 
                    Test.Assert(agent.SetAzureStorageContainerACL(NEW_CONTAINER_NAME, accessType),
                        "SetAzureStorageContainerACL operation should succeed");
                    // Verification for returned values
                    dic["PublicAccess"] = accessType;
                    CloudBlobUtil.PackContainerCompareData(container, dic);
                    agent.OutputValidation(comp);

                    Test.Assert(container.GetPermissions().PublicAccess == accessType,
                        "PublicAccess should be equal: {0} = {1}", container.GetPermissions().PublicAccess, accessType);
                }
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        internal void NewTableTest(Agent agent)
        {
            string NEW_TABLE_NAME = Utility.GenNameString("Washington");
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Table, NEW_TABLE_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            // delete table if it exists
            CloudTable table = CommonStorageAccount.CreateCloudTableClient().GetTableReference(NEW_TABLE_NAME);
            table.DeleteIfExists();

            try
            {
                //--------------New operation--------------
                Test.Assert(agent.NewAzureStorageTable(NEW_TABLE_NAME), Utility.GenComparisonData("NewAzureStorageTable", true));
                // Verification for returned values
                dic.Add("CloudTable", table);
                agent.OutputValidation(comp);
                Test.Assert(table.Exists(), "table {0} should exist!", NEW_TABLE_NAME);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        internal void GetTableTest(Agent agent)
        {
            string NEW_TABLE_NAME = Utility.GenNameString("Washington");
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Table, NEW_TABLE_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            // create table if it does not exist
            CloudTable table = CommonStorageAccount.CreateCloudTableClient().GetTableReference(NEW_TABLE_NAME);
            table.CreateIfNotExists();

            dic.Add("CloudTable", table);

            try
            {
                //--------------Get operation--------------
                Test.Assert(agent.GetAzureStorageTable(NEW_TABLE_NAME), Utility.GenComparisonData("GetAzureStorageTable", true));
                // Verification for returned values
                agent.OutputValidation(comp);
            }
            finally
            {
                // clean up
                table.DeleteIfExists();
            }
        }

        internal void RemoveTableTest(Agent agent)
        {
            string NEW_TABLE_NAME = Utility.GenNameString("Washington");

            // create table if it does not exist
            CloudTable table = CommonStorageAccount.CreateCloudTableClient().GetTableReference(NEW_TABLE_NAME);
            table.CreateIfNotExists();

            try
            {
                //--------------Remove operation--------------
                Test.Assert(agent.RemoveAzureStorageTable(NEW_TABLE_NAME), Utility.GenComparisonData("RemoveAzureStorageTable", true));
                Test.Assert(!table.Exists(), "queue {0} should not exist!", NEW_TABLE_NAME);
            }
            finally
            {
                // clean up
                table.DeleteIfExists();
            }
        }

        internal void NewQueueTest(Agent agent)
        {
            string NEW_QUEUE_NAME = Utility.GenNameString("redmond-");
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Queue, NEW_QUEUE_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            CloudQueue queue = CommonStorageAccount.CreateCloudQueueClient().GetQueueReference(NEW_QUEUE_NAME);
            // delete queue if it exists
            queue.DeleteIfExists();

            try
            {
                //--------------New operation--------------
                Test.Assert(agent.NewAzureStorageQueue(NEW_QUEUE_NAME), Utility.GenComparisonData("NewAzureStorageQueue", true));
                dic.Add("CloudQueue", queue);
                dic["ApproximateMessageCount"] = null;

                // Verification for returned values               
                agent.OutputValidation(comp);
                Test.Assert(queue.Exists(), "queue {0} should exist!", NEW_QUEUE_NAME);
            }
            finally
            {
                // clean up
                queue.DeleteIfExists();
            }
        }

        internal void GetQueueTest(Agent agent)
        {
            string NEW_QUEUE_NAME = Utility.GenNameString("redmond-");
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Queue, NEW_QUEUE_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            CloudQueue queue = CommonStorageAccount.CreateCloudQueueClient().GetQueueReference(NEW_QUEUE_NAME);
            // create queue if it does exist
            queue.CreateIfNotExists();

            dic.Add("CloudQueue", queue);
            try
            {
                //--------------Get operation--------------
                Test.Assert(agent.GetAzureStorageQueue(NEW_QUEUE_NAME), Utility.GenComparisonData("GetAzureStorageQueue", true));
                // Verification for returned values
                queue.FetchAttributes();
                agent.OutputValidation(comp);
            }
            finally
            {
                // clean up
                queue.DeleteIfExists();
            }
        }

        internal void RemoveQueueTest(Agent agent)
        {
            string NEW_QUEUE_NAME = Utility.GenNameString("redmond-");

            // create queue if it does exist
            CloudQueue queue = CommonStorageAccount.CreateCloudQueueClient().GetQueueReference(NEW_QUEUE_NAME);
            queue.CreateIfNotExists();

            try
            {
                //--------------Remove operation--------------
                Test.Assert(agent.RemoveAzureStorageQueue(NEW_QUEUE_NAME), Utility.GenComparisonData("RemoveAzureStorageQueue", true));
                Test.Assert(!queue.Exists(), "queue {0} should not exist!", NEW_QUEUE_NAME);
            }
            finally
            {
                // clean up
                queue.DeleteIfExists();
            }
        }

        /// <summary>
        /// Parameters:
        ///     Type:
        ///         Blob Type: BlockBlob, PageBlob, AppendBlob
        /// </summary>
        internal void UploadBlobTest(Agent agent, string UploadFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("upload-");
            string blobName = Path.GetFileName(UploadFilePath);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, blobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                //--------------Upload operation--------------
                Test.Assert(agent.SetAzureStorageBlobContent(UploadFilePath, NEW_CONTAINER_NAME, Type), Utility.GenComparisonData("SendAzureStorageBlob", true));

                CloudBlob blob = CommonBlobHelper.QueryBlob(NEW_CONTAINER_NAME, blobName);
                CloudBlobUtil.PackBlobCompareData(blob, dic);
                // Verification for returned values
                agent.OutputValidation(comp);
                Test.Assert(blob != null && blob.Exists(), "blob " + blobName + " should exist!");
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Parameters:             
        ///     Type:
        ///         Blob Type: BlockBlob, PageBlob, AppendBlob
        /// </summary>
        internal void GetBlobTest(Agent agent, string UploadFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("upload-");
            string blobName = Path.GetFileName(UploadFilePath);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, blobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                bool bSuccess = false;
                // upload the blob file
                if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob)
                    bSuccess = CommonBlobHelper.UploadFileToBlockBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                else if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob)
                    bSuccess = CommonBlobHelper.UploadFileToPageBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                else if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob)
                    bSuccess = CommonBlobHelper.UploadFileToAppendBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);

                Test.Assert(bSuccess, "upload file {0} to container {1} should succeed", UploadFilePath, NEW_CONTAINER_NAME);

                //--------------Get operation--------------
                Test.Assert(agent.GetAzureStorageBlob(blobName, NEW_CONTAINER_NAME), Utility.GenComparisonData("GetAzureStorageBlob", true));

                // Verification for returned values
                // get blob object using XSCL 
                CloudBlob blob = CommonBlobHelper.QueryBlob(NEW_CONTAINER_NAME, blobName);
                blob.FetchAttributes();
                CloudBlobUtil.PackBlobCompareData(blob, dic);
                dic.Add("CloudBlob", blob);

                agent.OutputValidation(comp);
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Parameters:
        ///     Type:
        ///         Blob Type: BlockBlob, PageBlob, AppendBlob
        /// </summary>
        internal void DownloadBlobTest(Agent agent, string UploadFilePath, string DownloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            string ContainerName = Utility.GenNameString("upload-");
            string blobName = Path.GetFileName(UploadFilePath);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, blobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(ContainerName);
            container.CreateIfNotExists();

            try
            {
                // upload the blob file
                bool success = agent.SetAzureStorageBlobContent(UploadFilePath, ContainerName, Type, blobName);
                Test.Assert(success, "upload file {0} to container {1} should succeed", UploadFilePath, ContainerName);

                //--------------Download operation--------------
                string downloadFilePath = Path.Combine(DownloadDirPath, blobName);
                Test.Assert(agent.GetAzureStorageBlobContent(blobName, downloadFilePath, ContainerName),
                    Utility.GenComparisonData("GetAzureStorageBlobContent", true));
                CloudBlob blob = CommonBlobHelper.QueryBlob(ContainerName, blobName);
                CloudBlobUtil.PackBlobCompareData(blob, dic);
                // Verification for returned values
                agent.OutputValidation(comp);

                Test.Assert(FileUtil.CompareTwoFiles(downloadFilePath, UploadFilePath),
                    String.Format("File '{0}' should be bit-wise identicial to '{1}'", downloadFilePath, UploadFilePath));
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Parameters:
        ///     Type:
        ///         Blob Type: BlockBlob, PageBlob, AppendBlob
        /// </summary>
        internal void RemoveBlobTest(Agent agent, string UploadFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("upload-");
            string blobName = Path.GetFileName(UploadFilePath);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, blobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                bool bSuccess = false;
                // upload the blob file
                if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob)
                    bSuccess = CommonBlobHelper.UploadFileToBlockBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                else if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob)
                    bSuccess = CommonBlobHelper.UploadFileToPageBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                else if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob)
                    bSuccess = CommonBlobHelper.UploadFileToAppendBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                Test.Assert(bSuccess, "upload file {0} to container {1} should succeed", UploadFilePath, NEW_CONTAINER_NAME);

                //--------------Remove operation--------------
                Test.Assert(agent.RemoveAzureStorageBlob(blobName, NEW_CONTAINER_NAME), Utility.GenComparisonData("RemoveAzureStorageBlob", true));
                CloudBlob blob = CommonBlobHelper.QueryBlob(NEW_CONTAINER_NAME, blobName);
                Test.Assert(blob == null, "blob {0} should not exist!", blobName);
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Create a container and then get it using powershell cmdlet
        /// </summary>
        /// <returns>A CloudBlobContainer object which is returned by PowerShell</returns>
        protected CloudBlobContainer CreateAndPsGetARandomContainer()
        {
            string containerName = Utility.GenNameString("bvt");
            CloudBlobContainer container = SetUpStorageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
            container.CreateIfNotExists();

            try
            {
                PowerShellAgent agent = new PowerShellAgent();
                Test.Assert(agent.GetAzureStorageContainer(containerName), Utility.GenComparisonData("GetAzureStorageContainer", true));
                int count = 1;
                Test.Assert(agent.Output.Count == count, string.Format("get container should return only 1 container, actually it's {0}", agent.Output.Count));
                return (CloudBlobContainer)agent.Output[0]["CloudBlobContainer"];
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.NewContainerSas)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NewContainerSas)]
        public void NewContainerSasTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip NewContainerSasTest as Azure emulator does not support sas token");
                return;
            }

            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            blobUtil.SetupTestContainerAndBlob();

            try
            {
                string key = lang == Language.PowerShell ? Constants.SASTokenKey : Constants.SASTokenKeyNode;
                string fullContainerPermission = "rwdl";
                Test.Assert(agent.NewAzureStorageContainerSAS(blobUtil.Container.Name, string.Empty, fullContainerPermission),
                    "Generate container sas token should succeed");
                string sastoken = agent.Output[0][key].ToString();
                Test.Info("Generated sas token:{0}", sastoken);
                blobUtil.ValidateContainerListableWithSasToken(blobUtil.Container, sastoken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.NewBlobSas)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NewBlobSas)]
        public void NewBlobSasTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip NewBlobSasTest as Azure emulator does not support sas token");
                return;
            }

            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            blobUtil.SetupTestContainerAndBlob();

            try
            {
                string key = lang == Language.PowerShell ? Constants.SASTokenKey : Constants.SASTokenKeyNode;
                string fullBlobPermission = "rwd";
                Test.Assert(agent.NewAzureStorageBlobSAS(blobUtil.Container.Name, blobUtil.Blob.Name, string.Empty, fullBlobPermission),
                    "Generate container sas token should succeed");
                string sastoken = agent.Output[0][key].ToString();
                Test.Info("Generated sas token:{0}", sastoken);
                blobUtil.ValidateBlobReadableWithSasToken(blobUtil.Blob, sastoken);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.NewQueueSas)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NewQueueSas)]
        public void NewQueueSasTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip NewQueueSasTest as Azure emulator does not support sas token");
                return;
            }

            CloudQueueUtil queueUtil = new CloudQueueUtil(CommonStorageAccount);
            CloudQueue queue = queueUtil.CreateQueue();

            try
            {
                string key = lang == Language.PowerShell ? Constants.SASTokenKey : Constants.SASTokenKeyNode;
                string permission = "raup";
                Test.Assert(agent.NewAzureStorageQueueSAS(queue.Name, string.Empty, permission),
                    "Generate queue sas token should succeed");
                string sastoken = agent.Output[0][key].ToString();
                Test.Info("Generated sas token:{0}", sastoken);
                queueUtil.ValidateQueueAddableWithSasToken(queue, sastoken);
            }
            finally
            {
                queueUtil.RemoveQueue(queue.Name);
            }
        }

        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip NewTableSasTest as Azure emulator does not support sas token");
                return;
            }

            CloudTableUtil tableUtil = new CloudTableUtil(CommonStorageAccount);
            CloudTable table = tableUtil.CreateTable();

            try
            {
                string key = lang == Language.PowerShell ? Constants.SASTokenKey : Constants.SASTokenKeyNode;
                string permission = "raud";
                Test.Assert(agent.NewAzureStorageTableSAS(table.Name, string.Empty, permission),
                    "Generate table sas token should succeed");
                string sastoken = agent.Output[0][key].ToString();
                Test.Info("Generated sas token:{0}", sastoken);
                tableUtil.ValidateTableAddableWithSasToken(table, sastoken);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Test Plan 8.38 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewTableStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip NewTableStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudTableUtil tableUtil = new CloudTableUtil(CommonStorageAccount);
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessTablePolicy>(lang == Language.NodeJS)[0];

            try
            {
                Test.Assert(agent.NewAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in table should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                SharedAccessTablePolicies expectedPolicies = new SharedAccessTablePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission));

                Utility.WaitForPolicyBecomeValid<CloudTable>(table, samplePolicy);

                Utility.ValidateStoredAccessPolicies<SharedAccessTablePolicy>(table.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }


        /// <summary>
        /// Test Plan 8.39 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetTableStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip GetTableStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudTableUtil tableUtil = new CloudTableUtil(CommonStorageAccount);
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessTablePolicy>(lang == Language.NodeJS)[0];

            try
            {
                Test.Assert(agent.NewAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in table should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudTable>(table, samplePolicy);

                Test.Assert(agent.GetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName),
                "Get stored access policy in table should succeed");
                Test.Info("Get stored access policy:{0}", samplePolicy.PolicyName);

                SharedAccessTablePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessTablePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Test plan, BVT 8.40.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void RemoveTableStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip RemoveTableStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudTableUtil tableUtil = new CloudTableUtil(CommonStorageAccount);
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessTablePolicy>(lang == Language.NodeJS)[0];

            try
            {
                Test.Assert(agent.NewAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in table should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                Test.Assert(agent.RemoveAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy.PolicyName),
                    "Remove stored access policy in table should succeed");
                Test.Info("Remove stored access policy:{0}", samplePolicy.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudTable>(table, expectedCount: 0);

                int count = table.GetPermissions().SharedAccessPolicies.Count;
                Test.Assert(count == 0, string.Format("Policy should be removed. Current policy count is {0}", count));
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Test plan, BVT 8.41.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetTableStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip SetTableStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudTableUtil tableUtil = new CloudTableUtil(CommonStorageAccount);
            CloudTable table = tableUtil.CreateTable();
            Utility.ClearStoredAccessPolicy<CloudTable>(table);
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessTablePolicy>(lang == Language.NodeJS);
            Utility.RawStoredAccessPolicy samplePolicy1 = samplePolicies[0];
            Utility.RawStoredAccessPolicy samplePolicy2 = samplePolicies[1];
            samplePolicy2.PolicyName = samplePolicy1.PolicyName;
            samplePolicy2.ExpiryTime = DateTime.Today.AddDays(3);

            try
            {
                Test.Assert(agent.NewAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy1.PolicyName, samplePolicy1.Permission, samplePolicy1.StartTime, samplePolicy1.ExpiryTime),
                    "Create stored access policy in table should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy1.PolicyName);
                Test.Assert(agent.SetAzureStorageTableStoredAccessPolicy(table.Name, samplePolicy2.PolicyName, samplePolicy2.Permission, samplePolicy2.StartTime, samplePolicy2.ExpiryTime),
                "Set stored access policy in table should succeed");
                Test.Info("Set stored access policy:{0}", samplePolicy2.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudTable>(table, samplePolicy2);

                //get the policy and validate
                SharedAccessTablePolicies expectedPolicies = new SharedAccessTablePolicies();
                expectedPolicies.Add(samplePolicy2.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(samplePolicy2.StartTime, samplePolicy2.ExpiryTime, samplePolicy2.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessTablePolicy>(table.GetPermissions().SharedAccessPolicies, expectedPolicies);

                //validate the output
                SharedAccessTablePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessTablePolicy>(samplePolicy2.StartTime, samplePolicy2.ExpiryTime, samplePolicy2.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessTablePolicy>(policy, samplePolicy2.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// test plan BVT 8.42
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewQueueStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip NewQueueStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudQueueUtil queueUtil = new CloudQueueUtil(CommonStorageAccount);
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessQueuePolicy>()[0];

            try
            {
                Test.Assert(agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in queue should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                SharedAccessQueuePolicies expectedPolicies = new SharedAccessQueuePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission));

                Utility.WaitForPolicyBecomeValid<CloudQueue>(queue, samplePolicy);

                Utility.ValidateStoredAccessPolicies<SharedAccessQueuePolicy>(queue.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }


        /// <summary>
        /// Test Plan 8.43 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetQueueStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip GetQueueStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudQueueUtil queueUtil = new CloudQueueUtil(CommonStorageAccount);
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessQueuePolicy>()[0];

            try
            {
                Test.Assert(agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in queue should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudQueue>(queue, samplePolicy);

                Test.Assert(agent.GetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName),
                "Get stored access policy in queue should succeed");
                Test.Info("Get stored access policy:{0}", samplePolicy.PolicyName);

                SharedAccessQueuePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessQueuePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// Test plan, BVT 8.44.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void RemoveQueueStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip RemoveQueueStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudQueueUtil queueUtil = new CloudQueueUtil(CommonStorageAccount);
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessQueuePolicy>()[0];

            try
            {
                Test.Assert(agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in queue should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                Test.Assert(agent.RemoveAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy.PolicyName),
                    "Remove stored access policy in queue should succeed");
                Test.Info("Remove stored access policy:{0}", samplePolicy.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudQueue>(queue, expectedCount: 0);

                int count = queue.GetPermissions().SharedAccessPolicies.Count;
                Test.Assert(count == 0, string.Format("Policy should be removed. Current policy count is {0}", count));
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }

        /// <summary>
        /// Test plan, BVT 8.45.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetQueueStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip SetQueueStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudQueueUtil queueUtil = new CloudQueueUtil(CommonStorageAccount);
            CloudQueue queue = queueUtil.CreateQueue();
            Utility.ClearStoredAccessPolicy<CloudQueue>(queue); ;
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessQueuePolicy>();
            Utility.RawStoredAccessPolicy samplePolicy1 = samplePolicies[0];
            Utility.RawStoredAccessPolicy samplePolicy2 = samplePolicies[1];
            samplePolicy2.PolicyName = samplePolicy1.PolicyName;
            samplePolicy2.ExpiryTime = DateTime.Today.AddDays(3);

            try
            {
                Test.Assert(agent.NewAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy1.PolicyName, samplePolicy1.Permission, samplePolicy1.StartTime, samplePolicy1.ExpiryTime),
                    "Create stored access policy in queue should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy1.PolicyName);

                Test.Assert(agent.SetAzureStorageQueueStoredAccessPolicy(queue.Name, samplePolicy2.PolicyName, samplePolicy2.Permission, samplePolicy2.StartTime, samplePolicy2.ExpiryTime),
                "Set stored access policy in queue should succeed");
                Test.Info("Set stored access policy:{0}", samplePolicy2.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudQueue>(queue, samplePolicy2);

                //get the policy and validate
                SharedAccessQueuePolicies expectedPolicies = new SharedAccessQueuePolicies();
                expectedPolicies.Add(samplePolicy2.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(samplePolicy2.StartTime, samplePolicy2.ExpiryTime, samplePolicy2.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessQueuePolicy>(queue.GetPermissions().SharedAccessPolicies, expectedPolicies);

                //validate the output
                SharedAccessQueuePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessQueuePolicy>(samplePolicy2.StartTime, samplePolicy2.ExpiryTime, samplePolicy2.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessQueuePolicy>(policy, samplePolicy2.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                queueUtil.RemoveQueue(queue);
            }
        }


        /// <summary>
        /// Test Plan 8.34 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewContainerStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip NewContainerStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>()[0];

            try
            {
                Test.Assert(agent.NewAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in container should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                //get the policy and validate
                SharedAccessBlobPolicies expectedPolicies = new SharedAccessBlobPolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission));

                Utility.WaitForPolicyBecomeValid<CloudBlobContainer>(container, samplePolicy);

                Utility.ValidateStoredAccessPolicies<SharedAccessBlobPolicy>(container.GetPermissions().SharedAccessPolicies, expectedPolicies);
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// Test Plan 8.35 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetContainerStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip GetContainerStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>()[0];

            try
            {
                Test.Assert(agent.NewAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in container should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudBlobContainer>(container, samplePolicy);

                Test.Assert(agent.GetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName),
                "Get stored access policy in container should succeed");
                Test.Info("Get stored access policy:{0}", samplePolicy.PolicyName);

                SharedAccessBlobPolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessBlobPolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// Test plan, BVT 8.36.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void RemoveContainerStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip RemoveContainerStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            Utility.RawStoredAccessPolicy samplePolicy = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>()[0];

            try
            {
                Test.Assert(agent.NewAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in container should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                Test.Assert(agent.RemoveAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy.PolicyName),
                    "Remove stored access policy in container should succeed");
                Test.Info("Remove stored access policy:{0}", samplePolicy.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudBlobContainer>(container, expectedCount: 0);

                int count = container.GetPermissions().SharedAccessPolicies.Count;
                Test.Assert(count == 0, string.Format("Policy should be removed. Current policy count is {0}", count));
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

        /// <summary>
        /// Test plan, BVT 8.37.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetContainerStoredPolicyTest()
        {
            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip SetContainerStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            CloudBlobContainer container = blobUtil.CreateContainer();
            Utility.ClearStoredAccessPolicy<CloudBlobContainer>(container);
            List<Utility.RawStoredAccessPolicy> samplePolicies = Utility.SetUpStoredAccessPolicyData<SharedAccessBlobPolicy>();
            Utility.RawStoredAccessPolicy samplePolicy1 = samplePolicies[0];
            Utility.RawStoredAccessPolicy samplePolicy2 = samplePolicies[1];
            samplePolicy2.PolicyName = samplePolicy1.PolicyName;
            samplePolicy2.ExpiryTime = DateTime.Today.AddDays(3);

            try
            {
                Test.Assert(agent.NewAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy1.PolicyName, samplePolicy1.Permission, samplePolicy1.StartTime, samplePolicy1.ExpiryTime),
                    "Create stored access policy in container should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy1.PolicyName);

                Test.Assert(agent.SetAzureStorageContainerStoredAccessPolicy(container.Name, samplePolicy2.PolicyName, samplePolicy2.Permission, samplePolicy2.StartTime, samplePolicy2.ExpiryTime),
                "Set stored access policy in container should succeed");
                Test.Info("Set stored access policy:{0}", samplePolicy2.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudBlobContainer>(container, samplePolicy2);

                //get the policy and validate
                SharedAccessBlobPolicies expectedPolicies = new SharedAccessBlobPolicies();
                expectedPolicies.Add(samplePolicy2.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(samplePolicy2.StartTime, samplePolicy2.ExpiryTime, samplePolicy2.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessBlobPolicy>(container.GetPermissions().SharedAccessPolicies, expectedPolicies);

                //validate the output
                SharedAccessBlobPolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessBlobPolicy>(samplePolicy2.StartTime, samplePolicy2.ExpiryTime, samplePolicy2.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessBlobPolicy>(policy, samplePolicy2.PolicyName));
                agent.OutputValidation(comp);
            }
            finally
            {
                blobUtil.RemoveContainer(container);
            }
        }

    }
}
