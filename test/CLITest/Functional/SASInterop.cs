﻿namespace Management.Storage.ScenarioTest.Functional.SAS
{
    using System.IO;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Queue;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using BlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;

    /// <summary>
    /// This class would cover all the cases that would use SAS token(also generated by PS cmdlet) as credentials in PS cmdlets
    /// </summary>
    [TestClass]
    public class SASInterop : TestBase
    {
        private static string commonBlockFilePath;
        private static string commonPageFilePath;
        private static string downloadDirPath;

        //In PowerShell cmdlet, we do not allow "*[]'" for blob name because it's for wild card patten
        private const string SpecialCharsPrefix = @"~!@#$%^&()_+";

        [ClassInitialize()]
        public static void SASInteropClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
            GenerateBvtTempFiles();
            downloadDirPath = Test.Data.Get("DownloadDir");
        }

        [ClassCleanup()]
        public static void SASInteropClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// test initialize
        /// </summary>
        [TestInitialize()]
        public override void InitAgent()
        {
            agent = AgentFactory.CreateAgent(TestContext.Properties);

            SetCLIEnv(TestContext);

            Test.Start(TestContext.FullyQualifiedTestClassName, TestContext.TestName);
            OnTestSetup();
        }

        /// <summary>
        /// Generate temp files
        /// </summary>
        private static void GenerateBvtTempFiles()
        {
            bool AlwaysOperateOnWindows = (FileUtil.AgentOSType != OSType.Windows);

            commonBlockFilePath = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());
            commonPageFilePath = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());
            FileUtil.CreateDirIfNotExits(Path.GetDirectoryName(commonBlockFilePath), AlwaysOperateOnWindows);
            FileUtil.CreateDirIfNotExits(Path.GetDirectoryName(commonPageFilePath), AlwaysOperateOnWindows);
            // Generate block file and page file which are used for uploading
            FileUtil.GenerateMediumFile(commonBlockFilePath, Utility.GetRandomTestCount(1, 5), AlwaysOperateOnWindows);
            FileUtil.GenerateMediumFile(commonPageFilePath, Utility.GetRandomTestCount(1, 5), AlwaysOperateOnWindows);
        }

        /// <summary>
        /// 1. Generate SAS of a Blob with read permission
        /// 2. Get blob with the generated SAS token
        /// 3. Download blob with the generated SAS token
        /// 4. Copy blob with the generated SAS token(as source)
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SASInterop)]
        public void BlobWithReadPermission()
        {
            BlobOrContainerWithReadPermission(StorageObjectType.Container, BlobType.BlockBlob);
            SetCLIEnv(TestContext); //reset agent env 
            BlobOrContainerWithReadPermission(StorageObjectType.Container, BlobType.PageBlob);
        }

        /// <summary>
        /// 1. Generate SAS of a Container with read permission
        /// 2. Get blob with the generated SAS token
        /// 3. Download blob with the generated SAS token
        /// 4. Copy blob with the generated SAS token(as source)
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Container)]
        [TestCategory(PsTag.SASInterop)]
        public void ContainerWithReadPermission()
        {
            BlobOrContainerWithReadPermission(StorageObjectType.Container, BlobType.BlockBlob);
            SetCLIEnv(TestContext); //reset agent env 
            BlobOrContainerWithReadPermission(StorageObjectType.Container, BlobType.PageBlob);
        }

        public void BlobOrContainerWithReadPermission(StorageObjectType objectType, BlobType blobType)
        {
            blobUtil.SetupTestContainerAndBlob(blobType, SpecialCharsPrefix);

            try
            {
                ((PowerShellAgent)agent).SetContextWithSASToken(StorageAccount.Credentials.AccountName, blobUtil, objectType, string.Empty, "r");

                // Get blob with the generated SAS token
                Test.Assert(agent.GetAzureStorageBlob(blobUtil.Blob.Name, blobUtil.ContainerName),
                    string.Format("Get existing blob {0} in container {1} should succeed", blobUtil.Blob.Name, blobUtil.ContainerName));

                // Download blob with the generated SAS token
                string downloadFilePath = Path.Combine(downloadDirPath, blobUtil.Blob.Name);
                Test.Assert(agent.GetAzureStorageBlobContent(blobUtil.Blob.Name, downloadFilePath, blobUtil.ContainerName),
                    string.Format("Download blob {0} in container {1} to File {2} should succeed", blobUtil.Blob.Name, blobUtil.ContainerName, downloadFilePath));

                // Copy blob with the generated SAS token(as source)
                string copiedName = Utility.GenNameString("copied");
                object destContext = PowerShellAgent.GetStorageContext(StorageAccount.ToString(true));
                Test.Assert(agent.StartAzureStorageBlobCopy(blobUtil.ContainerName, blobUtil.Blob.Name, blobUtil.ContainerName, copiedName, destContext),
                    string.Format("Copy blob {0} in container {1} to blob {2} in container {3} should succeed",
                    blobUtil.Blob.Name, blobUtil.ContainerName, blobUtil.ContainerName, copiedName));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1. Generate SAS of a Container with write permission
        /// 2. Upload blob with the generated SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Container)]
        [TestCategory(PsTag.SASInterop)]
        public void ContainerWithWritePermission()
        {
            BlobOrContainerWithWritePermission(commonBlockFilePath, StorageObjectType.Container, BlobType.BlockBlob);
            SetCLIEnv(TestContext); //reset agent env 
            BlobOrContainerWithWritePermission(commonPageFilePath, StorageObjectType.Container, BlobType.PageBlob);
        }

        /// <summary>
        /// 1. Generate SAS of a Blob with write permission
        /// 2. Upload blob with the generated SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SASInterop)]
        public void BlobWithWritePermission()
        {
            BlobOrContainerWithWritePermission(commonBlockFilePath, StorageObjectType.Blob, BlobType.BlockBlob);
            SetCLIEnv(TestContext); //reset agent env 
            BlobOrContainerWithWritePermission(commonPageFilePath, StorageObjectType.Blob, BlobType.PageBlob);
        }

        public void BlobOrContainerWithWritePermission(string uploadFilePath, StorageObjectType objectType, BlobType type)
        {
            blobUtil.SetupTestContainerAndBlob(type);
            try
            {
                ((PowerShellAgent)agent).SetContextWithSASToken(StorageAccount.Credentials.AccountName, blobUtil, objectType, string.Empty, "w");

                // Upload blob with the generated SAS token
                Test.Assert(agent.SetAzureStorageBlobContent(uploadFilePath, blobUtil.ContainerName, type, blobUtil.Blob.Name),
                    string.Format("Overwriting existing blob {0} in container {1} should succeed", blobUtil.Blob.Name, blobUtil.ContainerName));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1. Generate SAS of a Container with delete permission
        /// 2. Delete blob with the generated SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Container)]
        [TestCategory(PsTag.SASInterop)]
        public void ContainerWithDeletePermission()
        {
            BlobOrContainerWithDeletePermission(StorageObjectType.Container);
        }

        /// <summary>
        /// 1. Generate SAS of a Blob with delete permission
        /// 2. Delete blob with the generated SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SASInterop)]
        public void BlobWithDeletePermission()
        {
            BlobOrContainerWithDeletePermission(StorageObjectType.Blob);
        }

        public void BlobOrContainerWithDeletePermission(StorageObjectType objectType)
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                ((PowerShellAgent)agent).SetContextWithSASToken(StorageAccount.Credentials.AccountName, blobUtil, objectType, string.Empty, "d");

                // Delete blob with the generated SAS token
                Test.Assert(agent.RemoveAzureStorageBlob(blobUtil.Blob.Name, blobUtil.ContainerName),
                    string.Format("Remove blob {0} in container {1} should succeed", blobUtil.Blob.Name, blobUtil.ContainerName));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1. Generate SAS of a Container with list permission
        /// 2. List blobs with the generated SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Container)]
        [TestCategory(PsTag.SASInterop)]
        public void ContainerWithListPermission()
        {
            blobUtil.SetupTestContainerAndBlob();
            try
            {
                string sastoken = agent.GetContainerSasFromCmd(blobUtil.ContainerName, string.Empty, "l");
                PowerShellAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sastoken);

                // List blobs with the generated SAS token
                Test.Assert(agent.GetAzureStorageBlob(string.Empty, blobUtil.ContainerName),
                    string.Format("List blobs in container {0} should succeed", blobUtil.ContainerName));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// 1. Generate SAS of a Queue with read permission
        /// 2. Get queue with the generated SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Queue)]
        [TestCategory(PsTag.SASInterop)]
        public void QueueWithReadPermission()
        {
            CloudQueue queue = queueUtil.CreateQueue();
            try
            {
                string sastoken = agent.GetQueueSasFromCmd(queue.Name, string.Empty, "r");
                PowerShellAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sastoken);

                //list specified queue with properties and meta data
                Test.Assert(agent.GetAzureStorageQueue(queue.Name), Utility.GenComparisonData("GetAzureStorageQueue", true));
            }
            finally
            {
                queueUtil.RemoveQueue(queue.Name);
            }
        }
    }
}
