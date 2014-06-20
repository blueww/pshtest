using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using BlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;
using ICloudBlob = Microsoft.WindowsAzure.Storage.Blob.ICloudBlob;

namespace Management.Storage.ScenarioTest
{
    [TestClass]
    public class CLIPerf_OneBlob : TestBase
    {
        private static int MAX_BLOCK_BLOB_SIZE = 195;   //GB
        private static int MAX_PAGE_BLOB_SIZE = 1024;   //GB
        private static string BLOCK_BLOB_UNIT = "G_BLOCK";
        private static string PAGE_BLOB_UNIT = "G_PAGE";

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            BlobHelper = new CloudBlobHelper(StorageAccount);
            ContainerName = Utility.GenNameString("perf");

            BlobHelper.CreateContainer(ContainerName);
            BlobHelper.CleanupContainer(ContainerName);

            //set the ConcurrentTaskCount field
            PowerShellAgent.ConcurrentTaskCount = Environment.ProcessorCount * 8;

            GenerateTestFiles();
        }

        //
        //Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            Trace.WriteLine("ClasssCleanup");

            // disable this as sometimes we would want to use the generated files to 
            //Helper.DeletePattern("testfile_*");
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
            Trace.WriteLine("TestCleanup");
            Test.End(TestContext.FullyQualifiedTestClassName, TestContext.TestName);

        }
        #endregion

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        public void UploadHttpBlock()
        {
            Upload(BlobType.BlockBlob);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        public void DownloadHttpBlock()
        {
            Download();
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        public void UploadHttpPage()
        {
            Upload(BlobType.PageBlob);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        public void DownloadHttpPage()
        {
            Download();
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        public void UploadHttpBlock_Max()
        {
            //put the generating files here, because it will cost a few hours to generate very big files
            GenerateTestFiles_Max();
            Upload(BlobType.BlockBlob, true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        public void DownloadHttpBlock_Max()
        {
            GenerateTestFiles_Max();
            Download(BlobType.BlockBlob, true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        public void UploadHttpPage_Max()
        {
            GenerateTestFiles_Max();
            Upload(BlobType.PageBlob, true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        public void DownloadHttpPage_Max()
        {
            GenerateTestFiles_Max();
            Download(BlobType.PageBlob, true);
        }

        /// <summary>
        /// upload blob files
        /// the following two parameters are only useful for upload blob file with maximum size
        /// <param name="blobType"></param>
        /// <param name="bMax">indicates whether download a blob with the maximum size</param>
        /// </summary>
        public void Upload(BlobType blobType, bool bMax = false)
        {
            Dictionary<long, double> fileSizeTime = new Dictionary<long, double>();
            Dictionary<long, double> fileSizeTimeSD = new Dictionary<long, double>();

            TransferTestFiles(true, fileSizeTime, fileSizeTimeSD, blobType, bMax);

            //print the results
            string sizes = string.Empty;
            string times = string.Empty;
            string sds = string.Empty;
            foreach (KeyValuePair<long, double> d in fileSizeTime)
            {
                sizes += d.Key + " ";
                times += d.Value + " ";
            }
            foreach (KeyValuePair<long, double> d in fileSizeTimeSD)
            {
                sds += d.Value + " ";
            }

            Test.Info("[file_sizes] {0}", sizes);
            Test.Info("[file_times] {0}", times);
            Test.Info("[file_timeSDs] {0}", sds);

            Helper.writePerfLog(TestContext.FullyQualifiedTestClassName + "." + TestContext.TestName);
            Helper.writePerfLog(sizes.Replace(' ', ','));
            Helper.writePerfLog(times.Replace(' ', ','));
            Helper.writePerfLog(sds.Replace(' ', ','));
        }

        /// <summary>
        /// download blob files
        /// the following two parameters are only useful for download blob file with maximum size
        /// <param name="blobType"></param>
        /// <param name="bMax">indicates whether download a blob with the maximum size</param>
        /// </summary>
        public void Download(BlobType blobType = BlobType.Unspecified, bool bMax = false)
        {
            Dictionary<long, double> fileSizeTime = new Dictionary<long, double>();
            Dictionary<long, double> fileSizeTimeSD = new Dictionary<long, double>();
            string dest = ".\\downloadtemp";
            FileUtil.CreateNewFolder(dest);

            TransferTestFiles(false, fileSizeTime, fileSizeTimeSD, blobType, bMax);

            //print the results
            string sizes = string.Empty;
            string times = string.Empty;
            string sds = string.Empty;
            foreach (KeyValuePair<long, double> d in fileSizeTime)
            {
                sizes += d.Key + " ";
                times += d.Value + " ";
            }
            foreach (KeyValuePair<long, double> d in fileSizeTimeSD)
            {
                sds += d.Value + " ";
            }            

            Test.Info("[file_sizes] {0}", sizes);
            Test.Info("[file_times] {0}", times);
            Test.Info("[file_timeSDs] {0}", sds);

            Helper.writePerfLog(TestContext.FullyQualifiedTestClassName + "." + TestContext.TestName);
            Helper.writePerfLog(sizes.Replace(' ', ','));
            Helper.writePerfLog(times.Replace(' ', ','));
            Helper.writePerfLog(sds.Replace(' ', ','));
        }

        /// <summary>
        /// Upload/download blob files
        /// <param name="initSize"></param>
        /// <param name="endSize"></param>
        /// <param name="unit">"K", "M", "G", "G_BLOCK", "G_PAGE"</param>
        /// </summary>
        public void TransferTestFiles(bool upload, int initSize, int endSize, string unit, Dictionary<long, double> fileSizeTime,
            Dictionary<long, double> fileSizeTimeSD, BlobType blobType = BlobType.Unspecified, bool checkMD5 = true)
        {
            for (int i = initSize; i <= endSize; i *= 4)
            {
                string fileName = "testfile_" + i + unit;
                if (!FileUtil.FileExists(fileName))
                {
                    throw new Exception("file not found, path: " + fileName);
                }
                long fileSize = FileUtil.GetFileSize(fileName);
                List<long> fileTimeList = new List<long>();

                Stopwatch sw = new Stopwatch();

                for (int j = 0; j < 5; j++)
                {
                    if (upload)
                    {
                        // we need to clean the blob as we cannot overwrite block blob with page blob file
                        BlobHelper.DeleteBlob(ContainerName, fileName);
                    }

                    sw.Reset(); sw.Start();
                    if (upload)
                    {
                        bool bSuccess = agent.SetAzureStorageBlobContent(fileName, ContainerName, blobType, Force: true);
                        Test.Assert(bSuccess, "Set-AzureStorageBlobContent should succeed");
                    }
                    else
                    {
                        bool bSuccess = agent.GetAzureStorageBlobContent(fileName, fileName, ContainerName, Force: true);
                        Test.Assert(bSuccess, "Get-AzureStorageBlobContent should succeed");
                    }

                    sw.Stop();
                    fileTimeList.Add(sw.ElapsedMilliseconds);

                    if (checkMD5)
                    {
                        CheckMD5(ContainerName, fileName);
                    }

                    Test.Info("file name : {0} round : {1} time(ms) : {2}", fileName, j + 1, sw.ElapsedMilliseconds);
                }
                double average = fileTimeList.Average();
                fileSizeTime.Add(fileSize, average);
                var deviation = fileTimeList.Select(num => Math.Pow(num - average, 2));
                double sd = Math.Sqrt(deviation.Average());
                fileSizeTimeSD.Add(fileSize, sd);
                Test.Info("file name : {0} average time : {1}", fileName, average);
                Test.Info("file name : {0} standard dev : {1}", fileName, sd);
            }
        }

        /// <summary>
        /// Upload/download blob files
        /// <param name="bMax">indicates whether download a blob with the maximum size</param>
        /// </summary>
        public void TransferTestFiles(bool upload, Dictionary<long, double> fileSizeTime,
            Dictionary<long, double> fileSizeTimeSD, BlobType blobType = BlobType.Unspecified, bool bMax = false)
        {
            if (!bMax)
            {
                TransferTestFiles(upload, 2, 512, "K", fileSizeTime, fileSizeTimeSD, blobType);
                TransferTestFiles(upload, 2, 512, "M", fileSizeTime, fileSizeTimeSD, blobType);
                TransferTestFiles(upload, 2, 16, "G", fileSizeTime, fileSizeTimeSD, blobType);
            }
            else
            {
                // Upload/download a blob file with maximum size
                if (blobType == BlobType.BlockBlob)
                {
                    TransferTestFiles(upload, MAX_BLOCK_BLOB_SIZE, MAX_BLOCK_BLOB_SIZE, BLOCK_BLOB_UNIT, fileSizeTime, fileSizeTimeSD, blobType, false);
                }
                else if (blobType == BlobType.PageBlob)
                {
                    TransferTestFiles(upload, MAX_PAGE_BLOB_SIZE, MAX_PAGE_BLOB_SIZE, PAGE_BLOB_UNIT, fileSizeTime, fileSizeTimeSD, blobType, false);
                }
                else
                {
                    throw new Exception("No blob type specified for upload blob cmdlet");
                }
            }
        }

        public static void GenerateTestFiles()
        {
            Test.Info("Generating small files(KB)...");
            for (int i = 2; i <= 512; i *= 4)
            {
                string fileName = "testfile_" + i + "K";
                Test.Info("Generating file: " + fileName);
                FileUtil.GenerateSmallFile(fileName, i);
            }

            Test.Info("Generating medium files(MB)...");
            for (int i = 2; i <= 512; i *= 4)
            {
                string fileName = "testfile_" + i + "M";
                Test.Info("Generating file: " + fileName);
                FileUtil.GenerateMediumFile(fileName, i);
            }

            Test.Info("Generating big files(GB)...");
            for (int i = 2; i <= 16; i *= 4)
            {
                string fileName = "testfile_" + i + "G";
                Test.Info("Generating file: " + fileName);
                FileUtil.GenerateMediumFile(fileName, i * 1024);
            }

        }

        internal static void CheckMD5(string containerName, string filePath)
        {
            string blobName = Path.GetFileName(filePath);
            ICloudBlob blob = BlobHelper.QueryBlob(containerName, blobName);
            string localMd5 = FileUtil.GetFileContentMD5(filePath);
            Test.Assert(localMd5 == blob.Properties.ContentMD5,
                string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, blob.Properties.ContentMD5));
        }

        /// <summary>
        /// Generate blob file with maximum size
        /// <param name="bMax">indicates whether download a blob with the maximum size</param>
        /// </summary>
        public static void GenerateTestFiles_Max()
        {
            string filename = "testfile_" + MAX_BLOCK_BLOB_SIZE + BLOCK_BLOB_UNIT;
            Test.Info("Generating file: " + filename);
            GenerateBigFile(filename, MAX_BLOCK_BLOB_SIZE);

            filename = "testfile_" + MAX_PAGE_BLOB_SIZE + PAGE_BLOB_UNIT;
            Test.Info("Generating file: " + filename);
            GenerateBigFile(filename, MAX_PAGE_BLOB_SIZE);
        }

        internal static void GenerateBigFile(string filename, int sizeGB)
        {
            const int Giga = 1024 * 1024 * 1024;
            byte[] data = new byte[Giga];
            // make the writing data not all 0
            Random r = new Random();
            r.NextBytes(data);
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                for (int i = 0; i < sizeGB; i++)
                {
                    // Comment this because we don't need random data for very big data
                    //r.NextBytes(data);
                    stream.Write(data, 0, data.Length);
                }
            }
            return;
        }

        public static CloudBlobHelper BlobHelper;
        public static string ContainerName;
    }
}
