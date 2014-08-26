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
using Management.Storage.ScenarioTest.Performance.Helper;

namespace Management.Storage.ScenarioTest
{
    [TestClass]
    public class CLIPerf_OneBlob : TestBase
    {
        #region Additional test attributes
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            BlobHelper = new CloudBlobHelper(StorageAccount);
            FileHelper = new CloudFileHelper(StorageAccount);
            ContainerName = Utility.GenNameString("perf");

            BlobHelper.CreateContainer(ContainerName);
            BlobHelper.CleanupContainer(ContainerName);

            FileHelper.CreateShare(ContainerName);

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
            FileHelper.CleanupShare(ContainerName);
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
        [Timeout(14400000)]
        public void UploadHttpBlock()
        {
            BlobHelper.CleanupContainer(ContainerName);
            var o = new BlockBlobUploadOperation(this.agent, BlobHelper);
            Run(o);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        [Timeout(14400000)]
        public void DownloadHttpBlock()
        {
            var o = new BlockBlobDownloadOperation(this.agent, BlobHelper);
            Run(o);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        [Timeout(14400000)]
        public void UploadHttpPage()
        {
            BlobHelper.CleanupContainer(ContainerName);
            var o = new PageBlobUploadOperation(this.agent, BlobHelper);
            Run(o);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        [Timeout(14400000)]
        public void DownloadHttpPage()
        {
            var o = new PageBlobDownloadOperation(this.agent, BlobHelper);
            Run(o);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        public void UploadHttpBlock_Max()
        {
            BlobHelper.CleanupContainer(ContainerName);

            //put the generating files here, because it will cost a few hours to generate very big files
            GenerateTestFiles_Max();
            var o = new BlockBlobUploadOperation(this.agent, BlobHelper);
            Run(o, true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        public void DownloadHttpBlock_Max()
        {
            var o = new BlockBlobDownloadOperation(this.agent, BlobHelper);
            Run(o, true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        public void UploadHttpPage_Max()
        {
            BlobHelper.CleanupContainer(ContainerName);
            GenerateTestFiles_Max();
            var o = new PageBlobUploadOperation(this.agent, BlobHelper);
            Run(o, true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        public void DownloadHttpPage_Max()
        {
            GenerateTestFiles_Max();
            var o = new PageBlobDownloadOperation(this.agent, BlobHelper);
            Run(o, true);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(PsTag.FilePerf)]
        [Timeout(14400000)]
        public void UploadFile()
        {
            var o = new FileUploadOperation(this.agent, FileHelper);
            Run(o);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(PsTag.FilePerf)]
        [Timeout(14400000)]
        public void DownloadFile()
        {
            var o = new FileDownloadOperation(this.agent, FileHelper);
            Run(o);
        }

        /// <summary>
        /// upload blob files
        /// the following two parameters are only useful for upload blob file with maximum size
        /// <param name="blobType"></param>
        /// <param name="bMax">indicates whether download a blob with the maximum size</param>
        /// </summary>
        public void Run(ICLIOperation operation, bool bMax = false)
        {
            Dictionary<long, double> fileSizeTime = new Dictionary<long, double>();
            Dictionary<long, double> fileSizeTimeSD = new Dictionary<long, double>();

            TransferTestFiles(fileSizeTime, fileSizeTimeSD, operation, bMax);

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
        public void TransferTestFiles(int initSize, int endSize, string unit, Dictionary<long, double> fileSizeTime,
            Dictionary<long, double> fileSizeTimeSD, ICLIOperation operation)
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

                for (int j = 0; j < Constants.Iterations; j++)
                {
                    operation.Before(ContainerName, fileName);

                    sw.Reset(); sw.Start();
                    var bSuccess = operation.Go(
                                        containerName: ContainerName,
                                        fileName: fileName);
                    
                    Test.Assert(bSuccess, operation.Name + " should succeed");


                    sw.Stop();
                    fileTimeList.Add(sw.ElapsedMilliseconds);

                    var error = string.Empty;
                    Test.Assert(operation.Validate(ContainerName, fileName, out error), error);

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
        public void TransferTestFiles(Dictionary<long, double> fileSizeTime,
            Dictionary<long, double> fileSizeTimeSD, ICLIOperation operation, bool bMax = false)
        {
            if (!bMax)
            {
                TransferTestFiles(2, 512, "K", fileSizeTime, fileSizeTimeSD, operation);
                TransferTestFiles(2, 512, "M", fileSizeTime, fileSizeTimeSD, operation);
                TransferTestFiles(2, 16, "G", fileSizeTime, fileSizeTimeSD, operation);
            }
            else
            {
                operation.CheckMD5 = false;

                TransferTestFiles(
                    initSize: operation.MaxSize,
                    endSize: operation.MaxSize,
                    unit: operation.Unit,
                    fileSizeTime: fileSizeTime,
                    fileSizeTimeSD: fileSizeTimeSD,
                    operation: operation);
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
            string filename = "testfile_" + Constants.MAX_BLOCK_BLOB_SIZE + Constants.BLOCK_BLOB_UNIT;
            Test.Info("Generating file: " + filename);
            GenerateBigFile(filename, Constants.MAX_BLOCK_BLOB_SIZE);

            filename = "testfile_" + Constants.MAX_PAGE_BLOB_SIZE + Constants.PAGE_BLOB_UNIT;
            Test.Info("Generating file: " + filename);
            GenerateBigFile(filename, Constants.MAX_PAGE_BLOB_SIZE);
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
        public static CloudFileHelper FileHelper;
        public static string ContainerName;
    }
}
