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
using CloudBlob = Microsoft.WindowsAzure.Storage.Blob.CloudBlob;
using Management.Storage.ScenarioTest.Performance.Helper;

namespace Management.Storage.ScenarioTest
{
    [TestClass]
    public class CLIPerf_OneBlob : CLIPerfBase
    {
        #region Additional test attributes
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            BlobHelper = new CloudBlobHelper(StorageAccount);
            FileHelper = new CloudFileHelper(StorageAccount);

            //set the ConcurrentTaskCount field
            PowerShellAgent.ConcurrentTaskCount = Environment.ProcessorCount * 8;
        }

        //
        //Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            Trace.WriteLine("ClasssCleanup");
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
            var ro = new BlockBlobUploadOperation(this.agent, BlobHelper);
            Run(o, ro);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        [Timeout(14400000)]
        public void UploadHttpPage()
        {
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
            var ro = new PageBlobUploadOperation(this.agent, BlobHelper);
            Run(o, ro);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        [Timeout(144000000)]
        public void UploadHttpBlock_Max()
        {
            var o = new BlockBlobUploadOperation(this.agent, BlobHelper);
            Run(o, max: true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        [Timeout(144000000)]
        public void DownloadHttpBlock_Max()
        {
            var o = new BlockBlobDownloadOperation(this.agent, BlobHelper);
            var ro = new BlockBlobUploadOperation(this.agent, BlobHelper);
            Run(o, ro, max: true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        [Timeout(144000000)]
        public void UploadHttpPage_Max()
        {
            var o = new PageBlobUploadOperation(this.agent, BlobHelper);
            Run(o, max: true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        [Timeout(144000000)]
        public void DownloadHttpPage_Max()
        {
            var o = new PageBlobDownloadOperation(this.agent, BlobHelper);
            var ro = new PageBlobUploadOperation(this.agent, BlobHelper);
            Run(o, ro, max: true);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(PsTag.FilePerf)]
        [TestCategory(CLITag.NodeJSPerf)]
        [Timeout(14400000)]
        public void UploadFile()
        {
            var o = new FileUploadOperation(this.agent, FileHelper);
            Run(o);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(PsTag.FilePerf)]
        [TestCategory(CLITag.NodeJSPerf)]
        [Timeout(14400000)]
        public void DownloadFile()
        {
            var o = new FileDownloadOperation(this.agent, FileHelper);
            var ro = new FileUploadOperation(this.agent, FileHelper);
            Run(o, ro);
        }


        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        [Timeout(200000000)]
        public void UploadFile_Max()
        {
            var o = new FileUploadOperation(this.agent, FileHelper);
            Run(o, max: true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        [Timeout(200000000)]
        public void DownloadFile_Max()
        {
            var o = new FileDownloadOperation(this.agent, FileHelper);
            var ro = new FileUploadOperation(this.agent, FileHelper);
            Run(o, ro, max: true);
        }

        #region append blob
        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        [Timeout(14400000)]
        public void UploadHttpAppend()
        {
            BlobHelper.CleanupContainer(ContainerName);
            GenerateTestFiles();
            var o = new AppendBlobUploadOperation(this.agent, BlobHelper);
            Run(o);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        [Timeout(14400000)]
        public void DownloadHttpAppend()
        {
            var o = new AppendBlobDownloadOperation(this.agent, BlobHelper);
            Run(o);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        [Timeout(144000000)]
        public void UploadHttpAppend_Max()
        {
            BlobHelper.CleanupContainer(ContainerName);

            //put the generating files here, because it will cost a few hours to generate very big files
            var o = new AppendBlobUploadOperation(this.agent, BlobHelper);
            GenerateTestFiles_Max(o);
            Run(o, true);
        }

        [TestMethod]
        [TestCategory(PsTag.Scale)]
        [TestCategory(CLITag.NodeJSScale)]
        [Timeout(144000000)]
        public void DownloadHttpAppend_Max()
        {
            var o = new AppendBlobDownloadOperation(this.agent, BlobHelper);
            Run(o, true);
        }

        #endregion

        /// <summary>
        /// upload blob files
        /// the following two parameters are only useful for upload blob file with maximum size
        /// <param name="blobType"></param>
        /// <param name="max">indicates whether download a blob with the maximum size</param>
        /// </summary>
        public void Run(ICLIOperation operation, ICLIOperation reverseOperation = null, bool max = false)
        {
            if (operation.IsUploadTest || (this.GenerateDataBeforeDownload && reverseOperation != null)) 
            {
                if (max)
                {
                    GenerateTestFiles_Max(operation);
                }
                else
                {
                    GenerateTestFiles();
                }
            }

            Dictionary<long, double> fileSizeTime = new Dictionary<long, double>();
            Dictionary<long, double> fileSizeTimeSD = new Dictionary<long, double>();

            TransferTestFiles(fileSizeTime, fileSizeTimeSD, operation, max, reverseOperation);

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
            Dictionary<long, double> fileSizeTimeSD, ICLIOperation operation, ICLIOperation reverseOperation = null, int? iteration = null)
        {
            var containerName = DownloadContainerPrefix;
            if (operation.IsUploadTest)
            {
                containerName = UploadContainerPrefix;
            }

            for (int i = initSize; i <= endSize; i *= 4)
            {
                string fileName = "testfile_" + i + unit;

                long fileSize = 0L;
                if (operation.IsUploadTest)
                {
                    if (!FileUtil.FileExists(fileName))
                    {
                        throw new Exception("file not found, path: " + fileName);
                    }
                    else
                    {
                        fileSize = FileUtil.GetFileSize(fileName);
                    }
                }
                else if (this.GenerateDataBeforeDownload && reverseOperation != null)
                {
                    reverseOperation.Before(containerName, fileName);
                    reverseOperation.Go(containerName, fileName);
                }

                List<long> fileTimeList = new List<long>();

                Stopwatch sw = new Stopwatch();

                var iterations = iteration.HasValue ? iteration.Value : Constants.Iterations;
                for (int j = 0; j < iterations; j++)
                {
                    operation.Before(containerName, fileName);

                    sw.Reset(); sw.Start();
                    var bSuccess = operation.Go(
                                        containerName: containerName,
                                        fileName: fileName);
                    
                    Test.Assert(bSuccess, operation.Name + " should succeed");


                    sw.Stop();
                    fileTimeList.Add(sw.ElapsedMilliseconds);

                    var error = string.Empty;
                    Test.Assert(operation.Validate(containerName, fileName, out error), error);

                    Test.Info("file name : {0} round : {1} time(ms) : {2}", fileName, j + 1, sw.ElapsedMilliseconds);
                }
                double average = fileTimeList.Average();

                if (!operation.IsUploadTest)
                {
                    fileSize = FileUtil.GetFileSize(fileName);
                }

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
            Dictionary<long, double> fileSizeTimeSD, ICLIOperation operation, bool bMax = false, ICLIOperation reverseOperation = null)
        {
            if (!bMax)
            {
                TransferTestFiles(2, 512, "K", fileSizeTime, fileSizeTimeSD, operation, reverseOperation);
                TransferTestFiles(2, 512, "M", fileSizeTime, fileSizeTimeSD, operation, reverseOperation);
                TransferTestFiles(2, 16, "G", fileSizeTime, fileSizeTimeSD, operation, reverseOperation);
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
                    operation: operation,
                    reverseOperation: reverseOperation,
                    iteration: 1); //for large scale testing, we only needs 1 iteration
            }
        }

        public static void GenerateTestFiles()
        {
            Test.Info("Generating small files(KB)...");
            for (int i = 2; i <= 512; i *= 4)
            {
                string fileName = "testfile_" + i + "K";

                //generate the file only when same file of same length already exists.
                if (!File.Exists(fileName) || new FileInfo(fileName).Length != i * 1024)
                {
                    Test.Info("Generating file: " + fileName);
                    FileUtil.GenerateSmallFile(fileName, i);
                }
            }

            Test.Info("Generating medium files(MB)...");
            for (int i = 2; i <= 512; i *= 4)
            {
                string fileName = "testfile_" + i + "M";

                //generate the file only when same file of same length already exists.
                if (!File.Exists(fileName) || new FileInfo(fileName).Length != i * 1024 * 1024)
                {
                    Test.Info("Generating file: " + fileName);
                    FileUtil.GenerateMediumFile(fileName, i);
                }
            }

            Test.Info("Generating big files(GB)...");
            for (int i = 2; i <= 16; i *= 4)
            {
                string fileName = "testfile_" + i + "G";

                //generate the file only when same file of same length already exists.
                if (!File.Exists(fileName) || new FileInfo(fileName).Length != i * 1024 * 1024 * 1024)
                {
                    Test.Info("Generating file: " + fileName);
                    FileUtil.GenerateMediumFile(fileName, i * 1024);
                }
            }
        }

        internal static void CheckMD5(string containerName, string filePath)
        {
            string blobName = Path.GetFileName(filePath);
            CloudBlob blob = BlobHelper.QueryBlob(containerName, blobName);
            string localMd5 = FileUtil.GetFileContentMD5(filePath);
            Test.Assert(localMd5 == blob.Properties.ContentMD5,
                string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, blob.Properties.ContentMD5));
        }

        /// <summary>
        /// Generate blob file with maximum size
        /// <param name="bMax">indicates whether download a blob with the maximum size</param>
        /// </summary>
        public static void GenerateTestFiles_Max(ICLIOperation o)
        {
            string filename = "testfile_" + o.MaxSize + o.Unit;
            
            //generate the file only when same file of same length already exists.
            if (!File.Exists(filename) || new FileInfo(filename).Length != o.MaxSize * 1024 * 1024 * 1024)
            {
                Test.Info("Generating file: " + filename);
                GenerateBigFile(filename, o.MaxSize);
            }
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
    }
}
