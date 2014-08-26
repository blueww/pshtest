using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using QueueListingDetails = Microsoft.WindowsAzure.Storage.Queue.Protocol.QueueListingDetails;

namespace Management.Storage.ScenarioTest
{
    //[TestClass]
    public class CLIPerf : TestBase
    {
        // Save the perf data of time cost: [operation, count, avg]
        private static Dictionary<string, Dictionary<int, double>> OperationTimeDic = new Dictionary<string, Dictionary<int, double>>();

        // Save the perf data of time cost standard deviation: [operation, count, sd]
        private static Dictionary<string, Dictionary<int, double>> OperationTimeSDDics = new Dictionary<string, Dictionary<int, double>>();

        //used to store file share name across different cmdlets.
        private static object sharedObject;

        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);

            // import module
            string moduleFilePath = Test.Data.Get("ModuleFilePath");
            PowerShellAgent.ImportModule(moduleFilePath);

            //set the default storage context
            PowerShellAgent.SetStorageContext(StorageAccount.ToString(true));

            //set the ConcurrentTaskCount field
            PowerShellAgent.ConcurrentTaskCount = Environment.ProcessorCount * 8;
        }

        internal void InitPerfData(string[] operations)
        {
            // initialize the dictionary for saving perf data
            foreach (string operation in operations)
            {
                OperationTimeDic.Add(operation, new Dictionary<int, double>());
                OperationTimeSDDics.Add(operation, new Dictionary<int, double>());
            }
        }

        internal void WritePerfData(string[] operations)
        {
            foreach (string operation in operations)
            {
                WriteResults(operation, OperationTimeDic[operation], OperationTimeSDDics[operation]);
            }
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        [TestCategory(CLITag.NodeJSPerf)]
        public void TestContainerPerf()
        {
            string count = Test.Data.Get("ContainerCount");
            string[] operations = new string[] { PsTag.NewContainer, PsTag.GetContainer, PsTag.SetContainerAcl, PsTag.RemoveContainer };
            InitPerfData(operations);
            GetPerfData(count, operations);
            WritePerfData(operations);
        }

        //[TestMethod]
        //[TestCategory(PsTag.Perf)]
        //[TestCategory(PsTag.FilePerf)]
        public void TestFilePerf()
        {
            string count = Test.Data.Get("ShareCount");
            string[] operations = new string[] { PsTag.NewShare, PsTag.GetShare, PsTag.RemoveShare, PsTag.NewDirectory, PsTag.GetDirectory, PsTag.RemoveDirectory };
            InitPerfData(operations);
            GetPerfData(count, operations);
            WritePerfData(operations);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        public void TestQueuePerf()
        {
            string count = Test.Data.Get("QueueCount");
            string[] operations = new string[] { PsTag.NewQueue, PsTag.GetQueue, PsTag.RemoveQueue };
            InitPerfData(operations);
            GetPerfData(count, operations);
            WritePerfData(operations);
        }

        [TestMethod]
        [TestCategory(PsTag.Perf)]
        public void TestTablePerf()
        {
            string count = Test.Data.Get("TableCount");
            string[] operations = new string[] { PsTag.NewTable, PsTag.GetTable, PsTag.RemoveTable };
            InitPerfData(operations);
            GetPerfData(count, operations);
            WritePerfData(operations);
        }

        private void GetPerfData(string count, string[] operations)
        {
            int[] counts = Utility.ParseCount(count);
            PerfTesting(counts, operations);
        }

        private void PerfTesting(int[] counts, string[] operations)
        {
            for (int i = 0; i < counts.Count(); i++)
            {
                Test.Info("**********************Test Perf, count = {0} ******************", counts[i]);
                PerfTesting(counts[i], operations);
            }
        }

        private void PerfTesting(int nCount, string[] operations)
        {
            int nTimes = 5;
            
            // initialize the lists for storing perf data
            Dictionary<string, List<long>> timeDic = new Dictionary<string, List<long>>();
            
            for (int i = 0; i < operations.Count(); ++i)
            {
                timeDic.Add(operations[i], new List<long>());
            }

            // run new/get/remove cmdlets and get the perf data
            for (int iter = 1; iter <= nTimes; iter++)
            {
                // initialize the name list
                string prefix = string.Format("c{0}", DateTime.Now.ToString("yyyyMMddhhmmss")) + "a";
                string[] names = Utility.GenNameListsInSeqNum(prefix, nCount).ToArray();

                // run cmdlets
                foreach (string operation in operations)
                {
                    long timeCost = DoPerfOperation(operation, names, prefix);
                    timeDic[operation].Add(timeCost);
                    Test.Info("\n****{0} (count = {1}) cost time {2} seconds (iter: {3}/{4})****\n", operation, nCount, timeCost / 1000, iter, nTimes);
                }
            }

            // parse the perf data 
            string sizes = string.Empty;
            string times = string.Empty;
            string sds = string.Empty;

            foreach (string operation in operations)
            {
                double average = timeDic[operation].Average();
                var deviation = timeDic[operation].Select(num => Math.Pow(num - average, 2));
                double sd = Math.Sqrt(deviation.Average());

                Test.Info("{0} operation average time : {1}", operation, average);
                Test.Info("{0} operation standard deviation : {1}", operation, sd);

                sizes += nCount + ",";
                times += average + ",";
                sds += sd + ",";

                Test.Info("[count] {0}", nCount);
                Test.Info("[operation_times] {0}", times);
                Test.Info("[operation_timeSDs] {0}", sds);

                OperationTimeDic[operation].Add(nCount, average);
                OperationTimeSDDics[operation].Add(nCount, sd);
            }
        }

        /// <summary>
        /// Run one powershell cmdlet and return the time cost in millisecond
        /// </summary>
        /// <param name="methodType">cmdlet type</param>
        /// <param name="sNames">a list of names(by pipeline)</param>
        /// <param name="prefix">the preffix of the names</param>
        /// <returns>The milliseconds of running the cmdlets</returns>
        private long DoPerfOperation(string methodType, string[] sNames, string prefix)
        {
            DoInit(methodType, sNames, prefix, ref CLIPerf.sharedObject);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            switch (methodType)
            {
                case PsTag.NewContainer:
                    {
                        Test.Assert(agent.NewAzureStorageContainer(sNames), Utility.GenComparisonData("NewAzureStorageContainer", true));
                    }
                    break;
                case PsTag.NewQueue:
                    {
                        Test.Assert(agent.NewAzureStorageQueue(sNames), Utility.GenComparisonData("NewAzureStorageQueue", true));
                    }
                    break;
                case PsTag.NewTable:
                    {
                        Test.Assert(agent.NewAzureStorageTable(sNames), Utility.GenComparisonData("NewAzureStorageTable", true));
                    }
                    break;
                case PsTag.NewShare:
                    {
                        Test.Assert(agent.NewFileShares(sNames), Utility.GenComparisonData("NewFileShare", true));
                    }
                    break;
                case PsTag.NewDirectory:
                    {
                        Test.Assert(agent.NewDirectories(CLIPerf.sharedObject as string, sNames), Utility.GenComparisonData("NewDirectory", true));
                    }
                    break;
                case PsTag.GetDirectory:
                    {
                        Test.Assert(agent.ListDirectories(CLIPerf.sharedObject as string), Utility.GenComparisonData("ListDirectory", true));
                    }
                    break;
                case PsTag.RemoveDirectory:
                    {
                        Test.Assert(agent.ListDirectories(CLIPerf.sharedObject as string), Utility.GenComparisonData("ListDirectory", true));
                    }
                    break;
                case PsTag.GetContainer:
                    {
                        Test.Assert(agent.GetAzureStorageContainerByPrefix(prefix), Utility.GenComparisonData("GetAzureStorageContainer", true));
                    }
                    break;
                case PsTag.GetShare:
                    {
                        Test.Assert(agent.GetFileSharesByPrefix(prefix), Utility.GenComparisonData("GetFileShare", true));
                    }
                    break;
                case PsTag.GetQueue:
                    {
                        Test.Assert(agent.GetAzureStorageQueueByPrefix(prefix), Utility.GenComparisonData("GetAzureStorageQueue", true));
                    }
                    break;
                case PsTag.GetTable:
                    {
                        Test.Assert(agent.GetAzureStorageTableByPrefix(prefix), Utility.GenComparisonData("GetAzureStorageTable", true));
                    }
                    break;
                case PsTag.RemoveContainer:
                    {
                        // use PowerShellAgent to remove a large number of containers in order to save time
                        Test.Assert((new PowerShellAgent()).RemoveAzureStorageContainer(sNames), Utility.GenComparisonData("RemoveAzureStorageContainer", true));
                    }
                    break;
                case PsTag.RemoveShare:
                    {
                        Test.Assert(agent.RemoveFileShares(sNames), Utility.GenComparisonData("RemoveFileShare", true));
                    }
                    break;
                case PsTag.RemoveQueue:
                    {
                        Test.Assert(agent.RemoveAzureStorageQueue(sNames), Utility.GenComparisonData("RemoveAzureStorageQueue", true));
                    }
                    break;
                case PsTag.RemoveTable:
                    {
                        Test.Assert(agent.RemoveAzureStorageTable(sNames), Utility.GenComparisonData("RemoveAzureStorageTable", true));
                    }
                    break;
                case PsTag.SetContainerAcl:
                    {
                        Test.Assert(agent.SetAzureStorageContainerACL(sNames, BlobContainerPublicAccessType.Container), Utility.GenComparisonData("SetAzureStorageContainerACL", true));
                    }
                    break;
                default:
                    Test.Assert(false, "Wrong method type!");
                    break;
            }

            sw.Stop();

            DoCleanup(methodType, sNames, prefix, CLIPerf.sharedObject);

            // Verification for returned values
            int nCount = sNames.Length;
            switch (methodType)
            {
                case PsTag.NewContainer:
                case PsTag.NewQueue:
                case PsTag.NewTable:
                case PsTag.NewShare:
                case PsTag.NewDirectory:
                case PsTag.SetContainerAcl:
                    {
                        if (!(agent is NodeJSAgent))
                        {
                            // no need to check for Node.js commands as it does not support pipeline
                            Test.Assert(agent.Output.Count == nCount, "{0} row returned : {1}", nCount, agent.Output.Count);
                            if (agent.Output.Count != nCount)
                            {
                                //only for debug
                                Test.Assert(false, "error found! terminate instantly for debug!");
                                Environment.Exit(0);
                            }
                        }
                    }
                    break;
                case PsTag.GetContainer:                
                    {
                        agent.OutputValidation(StorageAccount.CreateCloudBlobClient().ListContainers(prefix, ContainerListingDetails.All));
                    }
                    break;
                case PsTag.GetShare:
                    {
                        agent.OutputValidation(StorageAccount.CreateCloudFileClient().ListShares(
                            prefix: prefix, 
                            detailsIncluded: Microsoft.WindowsAzure.Storage.File.ShareListingDetails.All));
                    }
                    break;
                case PsTag.GetDirectory:
                    {
                        agent.OutputValidation(StorageAccount.CreateCloudFileClient().GetShareReference(CLIPerf.sharedObject as string).GetRootDirectoryReference().ListFilesAndDirectories());
                    }
                    break;
                case PsTag.GetQueue:
                    {
                        agent.OutputValidation(StorageAccount.CreateCloudQueueClient().ListQueues(prefix, QueueListingDetails.All));
                    }
                    break;
                case PsTag.GetTable:
                    {
                        agent.OutputValidation(StorageAccount.CreateCloudTableClient().ListTables(prefix));
                    }
                    break;
                case PsTag.RemoveContainer:
                case PsTag.RemoveQueue:
                case PsTag.RemoveShare:
                case PsTag.RemoveDirectory:
                case PsTag.RemoveTable:                
                    {
                        // No need to check the return object count now as it won't return any object for remove cmdlets
                        //Test.Assert(agent.Output.Count == nContainerCount, "{0} row returned : {1}", nContainerCount, agent.Output.Count);
                    }
                    break;
                default:
                    Test.Assert(false, "Wrong method type!");
                    break;
            }

            return sw.ElapsedMilliseconds;
        }

        private void DoInit(string methodType, string[] sNames, string prefix, ref object obj)
        {
            switch (methodType)
            {
                case PsTag.NewDirectory:
                    {
                        var shareName = "sh" + (new Random()).Next(100000);
                        if (agent.NewFileShares(new string[] {shareName})) {
                            obj = shareName;
                            return;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void DoCleanup(string methodType, string[] sNames, string prefix, object o)
        {

            switch (methodType)
            {
                case PsTag.RemoveDirectory:
                    {
                        var shareName = o as string;
                        if (shareName != null)
                        {
                            agent.RemoveFileShares(new string[] { shareName });
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// write the results to the log file
        /// </summary>
        internal void WriteResults(string operation, Dictionary<int, double> timeDic, Dictionary<int, double> TimeSDDic)
        {
            string sizes = string.Empty;
            string times = string.Empty;
            string sds = string.Empty;
            foreach (KeyValuePair<int, double> d in timeDic)
            {
                sizes += d.Key + ",";
                times += d.Value + ",";
            }

            foreach (KeyValuePair<int, double> d in TimeSDDic)
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
    }
}
