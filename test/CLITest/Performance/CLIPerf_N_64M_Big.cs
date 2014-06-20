using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using BlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;

namespace Management.Storage.ScenarioTest
{
    [TestClass]
    public class CLIPerf_N_64M_Big
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
            //Test.FullClassName = testContext.FullyQualifiedTestClassName;
            Test.FullClassName = testContext.FullyQualifiedTestClassName;

            string ConnectionString = Test.Data.Get("StorageConnectionString");
            BlobHelper = new CloudBlobHelper(CloudStorageAccount.Parse(ConnectionString));

            ContainerName = Utility.GenNameString("perf");
            BlobHelper.CleanupContainer(ContainerName);
            FileName = Test.Data.Get("FileName");
            FolderName = Test.Data.Get("FolderName");
            
            //delete perf files
            Helper.DeletePattern(FolderName + "_*");

            // import module
            string moduleFilePath = Test.Data.Get("ModuleFilePath");
            PowerShellAgent.ImportModule(moduleFilePath);

            // $context = New-AzureStorageContext -ConnectionString ...
            PowerShellAgent.SetStorageContext(ConnectionString);

            //set the ConcurrentTaskCount field
            PowerShellAgent.ConcurrentTaskCount = Environment.ProcessorCount * 8;

            Trace.WriteLine("ClassInit");
        }

        //
        //Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            Trace.WriteLine("ClasssCleanup");

            Helper.DeletePattern(FolderName + "_*");
            BlobHelper.CleanupContainer(ContainerName);
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
            Helper.DeletePattern(FolderName + "_*");
            Trace.WriteLine("TestCleanup");
            Test.End(TestContext.FullyQualifiedTestClassName, TestContext.TestName);

        }
        #endregion

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        public void UploadHttpBlock()
        {
            Upload(BlobType.BlockBlob);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        public void DownloadHttpBlock()
        {
            Download();
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        public void UploadHttpPage()
        {
            Upload(BlobType.PageBlob);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        public void DownloadHttpPage()
        {
            Download();
        }

        public void Upload(BlobType blobType)
        {
            Dictionary<int, double> fileNumTime = new Dictionary<int, double>();
            Dictionary<int, double> fileNumTimeSD = new Dictionary<int, double>();
            string src = ".";
            string dest = ContainerName;

            int fileNum = 2;
            while (fileNum <= 128)  //change to a smaller number(2048-->128) as we upload/download blobs in sequence now
            {
                string folderName = FolderName + "-" + fileNum;
                GenerateTestFiles(fileNum);
                TransferTestFiles(src + "\\" + folderName, folderName, dest + "-" + folderName, true, fileNum, fileNumTime, fileNumTimeSD, blobType);
                fileNum *= 4;
            }
            
            //print the results
            string sizes = string.Empty;
            string times = string.Empty;
            string sds = string.Empty;
            foreach (KeyValuePair<int, double> d in fileNumTime)
            {
                sizes += d.Key + " ";
                times += d.Value + " ";
            }

            foreach (KeyValuePair<int, double> d in fileNumTimeSD)
            {
                sds += d.Value + " ";
            }

            Test.Info("[file_number] {0}", sizes);
            Test.Info("[file_times] {0}", times);
            Test.Info("[file_timeSDs] {0}", sds);

            Helper.writePerfLog(TestContext.FullyQualifiedTestClassName + "." + TestContext.TestName);
            Helper.writePerfLog(sizes.Replace(' ', ','));
            Helper.writePerfLog(times.Replace(' ', ','));
            Helper.writePerfLog(sds.Replace(' ', ','));
        }

        public void Download()
        {
            Dictionary<int, double> fileNumTime = new Dictionary<int, double>();
            Dictionary<int, double> fileNumTimeSD = new Dictionary<int, double>();
            string src = ContainerName;
            string dest = ".";

            int fileNum = 2;
            while (fileNum <= 128) //change to a smaller number(2048-->128) as we upload/download blobs in sequence now
            {
                Helper.CreateNewFolder(FolderName + "-" + fileNum);
                TransferTestFiles(src + "-" + FolderName + "-" + fileNum, "", dest + "\\" + FolderName + "-" + fileNum, false, 
                    fileNum, fileNumTime, fileNumTimeSD);
                fileNum *= 4;
            }
          
            //print the results
            string sizes = string.Empty;
            string times = string.Empty;
            string sds = string.Empty;

            foreach (KeyValuePair<int, double> d in fileNumTime)
            {
                sizes += d.Key + " ";
                times += d.Value + " ";
            }
            foreach (KeyValuePair<int, double> d in fileNumTimeSD)
            {
                sds += d.Value + " ";
            }

            Test.Info("[file_number] {0}", sizes);
            Test.Info("[file_times] {0}", times);
            Test.Info("[file_timeSDs] {0}", sds);

            Helper.writePerfLog(TestContext.FullyQualifiedTestClassName + "." + TestContext.TestName);
            Helper.writePerfLog(sizes.Replace(' ', ','));
            Helper.writePerfLog(times.Replace(' ', ','));
            Helper.writePerfLog(sds.Replace(' ', ','));
        }

        public static void TransferTestFiles(string src, string virtualFolderName, string dest, bool upload, int fileNum,
            Dictionary<int, double> fileNumTime, Dictionary<int, double> fileNumTimeSD, BlobType blobType = BlobType.Unspecified)
        {
            PowerShellAgent agent = new PowerShellAgent();

            //for small files KB
            List<long> fileTimeList = new List<long>();

            Stopwatch sw = new Stopwatch();

            for (int j = 0; j < 5; j++)
            {
                if (upload)
                {
                    BlobHelper.CreateContainer(dest);
                    BlobHelper.CleanupContainer(dest);
                }
                sw.Reset(); sw.Start();

                if (upload)
                {
                    agent.AddPipelineScript(string.Format("ls -File -Path {0}", src));
                    Test.Info("Upload files...");
                    Test.Assert(agent.SetAzureStorageBlobContent(string.Empty, dest, blobType), "upload multiple files should succeed");
                    Test.Info("Upload finished...");
                }
                else
                {
                    agent.AddPipelineScript(string.Format("Get-AzureStorageContainer {0}", src));
                    agent.AddPipelineScript("Get-AzureStorageBlob");
                    Test.Assert(agent.GetAzureStorageBlobContent(string.Empty, dest, string.Empty, true), "download blob should be successful");
                }

                sw.Stop();
                fileTimeList.Add(sw.ElapsedMilliseconds);

                Test.Info("file number : {0} round : {1} time(ms) : {2}", fileNum, j + 1, sw.ElapsedMilliseconds);

                ValidateBlob(src, dest, upload, fileNum);
            }
            double average = fileTimeList.Average();
            var deviation = fileTimeList.Select(num => Math.Pow(num - average, 2));
            double sd = Math.Sqrt(deviation.Average());
            fileNumTime.Add(fileNum, average);
            fileNumTimeSD.Add(fileNum, sd);

            Test.Info("file number : {0} average time : {1}", fileNum, average);
            Test.Info("file number : {0} standard dev : {1}", fileNum, sd);
        }

        internal static void ValidateBlob(string src, string dest, bool upload, int fileNum)
        {
            //check blob
            string folderName = string.Empty;
            List<ICloudBlob> bloblist;
            if (upload)
            {
                folderName = src;
                BlobHelper.ListBlobs(dest, out bloblist);
            }
            else
            {
                folderName = dest;
                BlobHelper.ListBlobs(src, out bloblist);
            }
            // check file num first
            Test.Assert(bloblist.Count == fileNum, "The copied file count is {0}, it should be {1}.", bloblist.Count, fileNum);

            Test.Info("Begin Compare every blob.");
            foreach (ICloudBlob blob in bloblist)
            {
                string filePath = folderName + "\\" + blob.Name;
                FileInfo fb = new FileInfo(filePath);
                if (fb.Exists)
                {
                    blob.FetchAttributes();
                    if (blob.Properties.ContentMD5 != Helper.GetFileContentMD5(filePath))
                        Test.Error("{3}:{0}=={1}", blob.Properties.ContentMD5, Helper.GetFileContentMD5(filePath), filePath);
                }
                else
                {
                    Test.Error("The file {0} should exist", filePath);
                }
            }
            Test.Info("End Compare every blob.");
        }

        public static void GenerateTestFiles(int fileNum)
        {
            Helper.CreateNewFolder(FolderName + "-" + fileNum);
            for (int i = 0; i < fileNum; i++)
            {
                string fileName = string.Format("{0}-{1}\\testfile_64M_{2}", FolderName, fileNum, i);
                if (!File.Exists(fileName))
                {
                    Test.Info("Generating file: " + fileName);
                    Helper.GenerateMediumFile(fileName, 64);
                }
            }
        }

        public static CloudBlobHelper BlobHelper;
        public static string ContainerName;
        public static string FileName;
        public static string FolderName;
    }
}
