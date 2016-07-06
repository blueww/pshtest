using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using Microsoft.WindowsAzure.Storage.Blob;
using BlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;

namespace Management.Storage.ScenarioTest
{
    //[TestClass]
    public class CLIPerf_BlobCmdlets
    {
        private TestContext testContextInstance;

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
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Test.FullClassName = testContext.FullyQualifiedTestClassName;

            srcConnectionString = Test.Data.Get("StorageConnectionString");
            destConnectionString = Test.Data.Get("StorageConnectionString2");    
            srcBlobHelper = new CloudBlobHelper(CloudStorageAccount.Parse(srcConnectionString));
            destBlobHelper = new CloudBlobHelper(CloudStorageAccount.Parse(destConnectionString));

            destStorageContext = PowerShellAgent.GetStorageContext(destConnectionString);

            FolderName = Test.Data.Get("FolderName");

            //delete perf files
            Helper.DeletePattern(FolderName + "_*");

            // import module
            PowerShellAgent.ImportModules(Constants.ServiceModulePaths);

            //set the default storage context
            PowerShellAgent.SetStorageContext(srcConnectionString);

            //set the ConcurrentTaskCount field
            PowerShellAgent.ConcurrentTaskCount = Environment.ProcessorCount * 8;

            // initialize the dictionary for saving perf data
            foreach (string operation in operations)
            {
                fileNumTimes.Add(operation, new Dictionary<int, double>());
                fileNumTimeSDs.Add(operation, new Dictionary<int, double>());
            }

            Trace.WriteLine("ClassInit");
        }

        //
        //Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            Trace.WriteLine("ClasssCleanup");

            Helper.DeletePattern(FolderName + "-*");
        }

        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            Trace.WriteLine("TestInit");
            Test.Start(TestContext.FullyQualifiedTestClassName, TestContext.TestName);

        }

        //Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            Helper.DeletePattern(FolderName + "-*");
            Trace.WriteLine("TestCleanup");
            Test.End(TestContext.FullyQualifiedTestClassName, TestContext.TestName);

        }
        #endregion

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        public void CmldetsTestBlock()
        {
            CleanPerfData();
            BlobCmldetsTest(BlobType.BlockBlob);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        public void CmldetsTestPage()
        {
            CleanPerfData();
            BlobCmldetsTest(BlobType.PageBlob);
        }

        public void BlobCmldetsTest(BlobType blobType)
        {
            string count = Test.Data.Get("BlobCount");

            // create temporary source & destination containers
            string srcContainer = Utility.GenNameString("src-");
            string destContainer = Utility.GenNameString("dest-");
            srcBlobHelper.CreateContainer(srcContainer);
            destBlobHelper.CreateContainer(destContainer);

            int[] counts = Utility.ParseCount(count);
            foreach (int fileNum in counts)
            {
                // prepare blob data
                GenerateTestFiles(fileNum);
                UploadBlobs(@".\" + FolderName + "-" + fileNum, srcContainer, fileNum, blobType);

                foreach (string operation in operations)
                {
                    RunBlobCmdlet(operation, srcContainer, destContainer, fileNum, blobType);
                }
                // clean generate files
                Helper.DeletePattern(FolderName + "-*");

                srcBlobHelper.CleanupContainer(srcContainer);
                destBlobHelper.CleanupContainer(destContainer);
            }

            foreach (string operation in operations)
            {
                WriteResults(operation, fileNumTimes[operation], fileNumTimeSDs[operation]);
            }

            srcBlobHelper.DeleteContainer(srcContainer);
            destBlobHelper.DeleteContainer(destContainer);
        }

        internal static void UploadBlobs(string folderPath, string containerName, int fileNum, BlobType blobType)
        {
            Stopwatch sw = new Stopwatch();
            PowerShellAgent agent = new PowerShellAgent();
            agent.AddPipelineScript(string.Format("ls -File -Path {0}", folderPath));

            sw.Start();
            Test.Assert(agent.SetAzureStorageBlobContent(string.Empty, containerName, blobType), "upload multiple files should succeed");
            sw.Stop();

            Test.Info("file number : {0}, upload time(ms) : {1}", fileNum, sw.ElapsedMilliseconds);
        }

        internal static void RunBlobCmdlet(string operation, string srcContainer, string destContainer, int fileNum, BlobType blobType)
        {
            List<long> fileTimeList = new List<long>();

            Stopwatch sw = new Stopwatch();

            for (int j = 0; j < 5; j++)
            {
                destBlobHelper.CleanupContainer(destContainer);
                PowerShellAgent agent = new PowerShellAgent();
                sw.Reset(); 

                switch (operation)
                {
                    case GetBlobs:
                        sw.Start();
                        Test.Assert(agent.GetAzureStorageBlob("*", srcContainer), "get blob list should succeed");
                        break;
                    case StartCopyBlobs:
                        agent.AddPipelineScript(string.Format("Get-AzureStorageBlob -Container {0}", srcContainer));
                        sw.Start();
                        Test.Assert(agent.StartAzureStorageBlobCopy(string.Empty, string.Empty, destContainer, string.Empty, destContext: destStorageContext), "start copy blob should be successful");
                        break;
                    case GetCopyBlobState:
                        PowerShellAgent.SetStorageContext(destConnectionString);
                        agent.AddPipelineScript(string.Format("Get-AzureStorageBlob -Container {0}", destContainer));
                        sw.Start();
                        Test.Assert(agent.GetAzureStorageBlobCopyState(string.Empty, string.Empty, false), "Get copy state should be success");                        
                        break;
                    case StopCopyBlobs:
                        PowerShellAgent.SetStorageContext(destConnectionString);
                        agent.AddPipelineScript(String.Format("Get-AzureStorageBlob -Container {0}", destContainer));
                        sw.Start();
                        Test.Assert(agent.StopAzureStorageBlobCopy(string.Empty, string.Empty, "*", true), "Stop multiple copy operations using blob pipeline should be successful");
                        break;
                    case RemoveBlobs:
                        agent.AddPipelineScript(string.Format("Get-AzureStorageBlob -Container {0}", srcContainer));
                        sw.Start();
                        Test.Assert(agent.RemoveAzureStorageBlob(string.Empty, string.Empty), "remove blob list should succeed");
                        break;
                    default:
                        throw new Exception("unknown operation : " + operation);
                }

                sw.Stop();
                fileTimeList.Add(sw.ElapsedMilliseconds);
               
                // Verification for returned values
                switch (operation)
                {
                    case GetBlobs:                   
                        Test.Assert(agent.Output.Count == fileNum, "{0} row returned : {1}", fileNum, agent.Output.Count);
                        // compare the blob entities    
                        List<CloudBlob> blobList = new List<CloudBlob>();
                        srcBlobHelper.ListBlobs(srcContainer, out blobList);
                        agent.OutputValidation(blobList);
                        break;

                    case StartCopyBlobs:
                        Test.Assert(agent.Output.Count == fileNum, "{0} row returned : {1}", fileNum, agent.Output.Count);
                        break;
                }

                // set srcConnectionString as default ConnectionString
                PowerShellAgent.SetStorageContext(srcConnectionString);

                Test.Info("file number : {0} round : {1} {2} time(ms) : {3}", fileNum, j + 1, operation, sw.ElapsedMilliseconds);
            }

            double average = fileTimeList.Average();
            var deviation = fileTimeList.Select(num => Math.Pow(num - average, 2));
            double sd = Math.Sqrt(deviation.Average());
            fileNumTimes[operation].Add(fileNum, average);
            fileNumTimeSDs[operation].Add(fileNum, sd);

            Test.Info("file number : {0} average time : {1}", fileNum, average);
            Test.Info("file number : {0} standard dev : {1}", fileNum, sd);
        }

        /// <summary>
        /// write the results to the log file
        /// </summary>
        internal void WriteResults(string operation, Dictionary<int, double> fileNumTime, Dictionary<int, double> fileNumTimeSD)
        {
            string sizes = string.Empty;
            string times = string.Empty;
            string sds = string.Empty;
            foreach (KeyValuePair<int, double> d in fileNumTime)
            {
                sizes += d.Key + ",";
                times += d.Value + ",";
            }

            foreach (KeyValuePair<int, double> d in fileNumTimeSD)
            {
                sds += d.Value + ",";
            }

            Test.Info("[file_number] {0}", sizes);
            Test.Info("[file_times] {0}", times);
            Test.Info("[file_timeSDs] {0}", sds);

            Helper.writePerfLog(TestContext.TestName + " : " + operation);
            Helper.writePerfLog(sizes);
            Helper.writePerfLog(times);
            Helper.writePerfLog(sds);
        }

        public static void GenerateTestFiles(int fileNum)
        {
            Helper.CreateNewFolder(FolderName + "-" + fileNum);
            for (int i = 0; i < fileNum; i++)
            {
                string fileName = string.Format("{0}-{1}\\testfile_1K_{2}", FolderName, fileNum, i);
                if (!File.Exists(fileName))
                {
                    Test.Info("Generating file: " + fileName);
                    Helper.GenerateSmallFile(fileName, 1);
                }
            }
        }

        private void CleanPerfData()
        {
            CleanDictionary(fileNumTimes);
            CleanDictionary(fileNumTimeSDs);
        }

        private static void CleanDictionary(Dictionary<string, Dictionary<int, double>> dic)
        {
            foreach (KeyValuePair<string, Dictionary<int, double>> pair in dic)
            {
                pair.Value.Clear();
            }
        }

        private static string srcConnectionString;
        private static string destConnectionString;
        private static CloudBlobHelper srcBlobHelper;
        private static CloudBlobHelper destBlobHelper;  //only used for copy blob 
        private static object destStorageContext;       //only used for copy blob 
        private static string FolderName;

        private const string GetBlobs = "GetBlobs";
        private const string StartCopyBlobs = "StartCopyBlobs";
        private const string GetCopyBlobState = "GetCopyBlobState";
        private const string StopCopyBlobs = "StopCopyBlobs";
        private const string RemoveBlobs = "RemoveBlobs";

        private static Dictionary<string, Dictionary<int, double>> fileNumTimes = new Dictionary<string, Dictionary<int, double>>();
        private static Dictionary<string, Dictionary<int, double>> fileNumTimeSDs = new Dictionary<string, Dictionary<int, double>>();
        private static string[] operations = { GetBlobs, StartCopyBlobs, GetCopyBlobState, StopCopyBlobs, RemoveBlobs };
    }
}
