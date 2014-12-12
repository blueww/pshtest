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

namespace Management.Storage.ScenarioTest
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    internal static class Utility
    {
        public static List<string> LoggingOperationList = new List<string>() { "Read", "Write", "Delete" };
        public static List<string> ContainerPermission = new List<string>() { "r", "w", "d", "l" };
        public static List<string> BlobPermission = new List<string>() { "r", "w", "d" };
        public static List<string> TablePermissionPS = new List<string>() { "r", "q", "a", "u", "d" };
        public static List<string> TablePermissionNode = new List<string>() { "r", "a", "u", "d" };
        public static List<string> QueuePermission = new List<string>() { "r", "a", "u", "p" };

        internal static int RetryLimit = 6;

        /// <summary>
        /// Generate a random string for azure object name
        /// </summary> 
        /// <param name="prefix">usually it's a string of letters, to avoid naming rule breaking</param>
        /// <param name="len">the length of random string after the prefix</param>
        /// <returns>a random string for azure object name</returns>
        public static string GenNameString(string prefix, int len = 8)
        {
            return prefix + Guid.NewGuid().ToString().Replace("-", "").Substring(0, len);
        }

        /// <summary>
        /// Generate random base 64 string
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        public static string GenBase64String(string seed = "")
        {
            string randomKey = Utility.GenNameString(seed);
            return Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(randomKey));
        }

        /// <summary>
        /// Generate content MD5 string which is 128 bits and base64 encoded
        /// </summary>
        /// <returns></returns>
        public static string GenRandomMD5()
        {
            byte[] bytes = new byte[16];
            Random rnd = new Random();
            rnd.NextBytes(bytes);

            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Get Storage End Points
        /// </summary>
        /// <param name="storageAccountName">storage account name</param>
        /// <param name="useHttps">use https</param>
        /// <returns>A string array. 0 is blob endpoint, 1 is queue endpoint, 2 is table endpoint, 3 is file endpoint</returns>
        public static string[] GetStorageEndPoints(string storageAccountName, bool useHttps, string endPoint = "")
        {
            string protocol = "http";

            if (useHttps)
            {
                protocol = "https";
            }

            if (string.IsNullOrEmpty(endPoint))
            {
                string configEndPoint = Test.Data.Get("StorageEndPoint");
                if (string.IsNullOrEmpty(configEndPoint))
                {
                    endPoint = "core.windows.net";
                }
                else
                {
                    endPoint = configEndPoint;
                }
            }

            endPoint = endPoint.Trim();

            string[] storageEndPoints = new string[4]
                {
                    string.Format("{0}://{1}.blob.{2}/", protocol, storageAccountName, endPoint),
                    string.Format("{0}://{1}.queue.{2}/", protocol, storageAccountName, endPoint),
                    string.Format("{0}://{1}.table.{2}/", protocol, storageAccountName, endPoint),
                    string.Format("{0}://{1}.file.{2}/", protocol, storageAccountName, endPoint)
                };
            return storageEndPoints;
        }

        /// <summary>
        /// Get CloudStorageAccount with specified end point
        /// </summary>
        /// <param name="credential">StorageCredentials object</param>
        /// <param name="useHttps">use https</param>
        /// <param name="endPoint">end point</param>
        /// <returns>CloudStorageAccount object</returns>
        public static CloudStorageAccount GetStorageAccountWithEndPoint(StorageCredentials credential, bool useHttps,
            string endPoint = "", string accountName = "")
        {
            string storageAccountName = string.IsNullOrEmpty(accountName) ? credential.AccountName : accountName;
            string[] endPoints = GetStorageEndPoints(storageAccountName, useHttps, endPoint);

            var account = new CloudStorageAccount(
                credential,
                new Uri(endPoints[0]),
                new Uri(endPoints[1]),
                new Uri(endPoints[2]),
                new Uri(endPoints[3]));

            return account;
        }

        /// <summary>
        /// Constructs the storage account instance using the provided
        /// connection string from configuration file.
        /// </summary>
        /// <returns>
        /// Returns the constructed CloudStorageAccount instance.
        /// </returns>
        public static CloudStorageAccount ConstructStorageAccountFromConnectionString(string configurationName = "StorageConnectionString")
        {
            string connectionString = Test.Data.Get(configurationName);
            return CloudStorageAccount.Parse(connectionString);
        }

        public static List<string> GenNameLists(string prefix, int count = 1, int len = 8)
        {
            List<string> names = new List<string>();

            for (int i = 0; i < count; i++)
            {
                names.Add(Utility.GenNameString(prefix, len));
            }

            return names;
        }

        public static List<string> GenNameListsInSeqNum(string prefix, int count = 1)
        {
            List<string> names = new List<string>();

            for (int i = 0; i < count; i++)
            {
                names.Add(prefix + i.ToString());
            }

            return names;
        }

        /// <summary>
        /// Generate random string with 26 alphabet in upper case.
        /// </summary>
        /// <param name="size">String length</param>
        /// <returns>Random alphabet string</returns>
        public static string GenRandomAlphabetString(int size = 8)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            char ch;

            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(random.Next(0, 26) + 65);
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public static string GenConnectionString(string StorageAccountName, string StorageAccountKey)
        {
            return String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccountName, StorageAccountKey);
        }

        /// <summary>
        /// Generate the data for output comparison
        /// </summary> 
        public static Dictionary<string, object> GenComparisonData(StorageObjectType objType, string name)
        {
            Dictionary<string, object> dic = new Dictionary<string, object> { 
                {"Name", name },
                {"Context", null}
            };

            switch (objType)
            {
                case StorageObjectType.Container:
                    dic.Add("PublicAccess", BlobContainerPublicAccessType.Off);        // default value is Off
                    dic.Add("LastModified", null);
                    dic.Add("Permission", null);
                    break;
                case StorageObjectType.Blob:
                    dic.Add("BlobType", null);      // need to validate this later
                    dic.Add("Length", null);        // need to validate this later
                    dic.Add("ContentType", null);   // the return value of upload operation is always null
                    dic.Add("LastModified", null);  // need to validate this later
                    dic.Add("SnapshotTime", null);  // need to validate this later
                    break;
                case StorageObjectType.Queue:
                    dic.Add("ApproximateMessageCount", 0);
                    dic.Add("EncodeMessage", true);
                    break;
                case StorageObjectType.Table:
                    break;
                default:
                    throw new Exception(String.Format("Object type:{0} not identified!", objType));
            }

            return dic;
        }

        /// <summary>
        /// Generate the data for output comparison
        /// </summary> 
        public static string GenComparisonData(string FunctionName, bool Success)
        {
            return String.Format("{0} operation should {1}.", FunctionName, Success ? "succeed" : "fail");
        }

        /// <summary>
        /// Convert a string count to int array
        /// </summary>
        /// <param name="count">string e.g. 1,2,5,10,20,50,100,200,500,1000,2000,5000,6000</param>
        /// <returns>int array</returns>
        public static int[] ParseCount(string count)
        {
            string[] countArray = count.Split(',');
            int[] counts = new int[countArray.Count()];
            for (int i = 0; i < countArray.Count(); ++i)
            {
                counts[i] = int.Parse(countArray[i]);
            }
            return counts;
        }

        /// <summary>
        /// Convert a Hashtable to string
        /// The output would be "k1=v1;k2=v2"
        /// </summary>
        public static string ConvertTable(Hashtable table)
        {
            string result = string.Empty;
            foreach (string key in table.Keys)
            {
                if (!string.IsNullOrEmpty(result))
                {
                    result += ";";
                }
                result += key + "=" + table[key];
            }
            return "\"" + result + "\"";
        }

        /// <summary>
        /// Generate a random small int number for test
        /// </summary>
        /// <returns>Random int</returns>
        public static int GetRandomTestCount(int minCount = 1, int maxCount = 10)
        {
            return (new Random()).Next(minCount, maxCount);
        }

        /// <summary>
        /// Generate a random bool
        /// </summary>
        /// <returns>Random bool</returns>
        public static bool GetRandomBool()
        {
            int switchKey = 0;
            switchKey = (new Random()).Next(0, 2);
            return switchKey == 0;
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            try
            {
                for (int i = 0; i < NumberChars; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
            }
            catch (Exception e)
            {
                Test.Error(string.Format("returned md5 hex string : '{0}'", hex));
                throw e;
            }
            return bytes;
        }

        /// <summary>
        /// Get OS config from testdata.xml
        /// </summary>
        public static void GetOSConfig(TestConfig data, ref OSType osType, OSConfig osConfig)
        {
            string os = string.Empty;
            if (data.TestParams.ContainsKey("AgentOS"))
            {
                os = data.Get("AgentOS");
                switch (os.ToLower())
                {
                    case "windows":
                        osType = OSType.Windows;
                        break;
                    case "linux":
                        osType = OSType.Linux;
                        osConfig.PLinkPath = data.Get("PLinkPath");
                        osConfig.UserName = data.Get("UserName");
                        osConfig.HostName = data.Get("HostName");
                        osConfig.Port = data.Get("Port");
                        osConfig.PrivateKeyPath = data.Get("PrivateKeyPath");
                        break;
                    case "mac":
                        osType = OSType.Mac;
                        osConfig.PLinkPath = data.Get("PLinkPath");
                        osConfig.UserName = data.Get("UserName");
                        osConfig.HostName = data.Get("HostName");
                        osConfig.Port = data.Get("Port");
                        osConfig.PrivateKeyPath = data.Get("PrivateKeyPath");
                        break;
                }
            }
        }

        /// <summary>
        /// Generate random combination strings
        /// e.g. if list = "Read","Write","Delete" and seperator = ", "
        /// then the output would be one of the following combination:
        ///     "Read", "Write", "Delete", "Read, Write", "Read, Delete", "Write, Delete", "Read, Write, Delete"
        /// </summary>
        /// <param name="list">list of elements used for generate combination</param>
        /// <param name="seperator"></param>
        /// <returns></returns>
        public static string GenRandomCombination(List<string> list, string seperator = "")
        {
            if (list.Count == 0) return string.Empty;

            int seed = GetRandomTestCount(1, (int)Math.Pow(2, list.Count));
            int index = 0;
            List<string> retList = new List<string>();
            int tester = 1;
            while (index < list.Count)
            {
                if ((tester & seed) != 0)
                {
                    retList.Add(list[index]);
                }
                index++;
                tester = tester << 1;
            }
            string ret = string.Join(seperator, retList.ToArray());

            return ret;
        }

        public static void ValidateLoggingOperationProperty(string loggingOperations, bool? read, bool? write, bool? delete)
        {
            if (string.Compare(loggingOperations, "All", true) == 0)
            {
                loggingOperations = "read,write,delete";
            }

            if (loggingOperations.ToLower().Contains("read"))
            {
                Test.Assert((read.HasValue && read.Value),
                string.Format("expected LoggingOperations for reading is true, actually it's '{0}'", read));
            }
            else
            {
                Test.Assert((read.HasValue && !read.Value),
                string.Format("expected LoggingOperations for reading is false, actually it's '{0}'", read));
            }

            if (loggingOperations.ToLower().Contains("write"))
            {
                Test.Assert((write.HasValue && write.Value),
                string.Format("expected LoggingOperations for writing is true, actually it's '{0}'", write));
            }
            else
            {
                Test.Assert((write.HasValue && !write.Value),
                string.Format("expected LoggingOperations for writing is false, actually it's '{0}'", write));
            }

            if (loggingOperations.ToLower().Contains("delete"))
            {
                Test.Assert((delete.HasValue && delete.Value),
                string.Format("expected LoggingOperations for deleting is true, actually it's '{0}'", delete));
            }
            else
            {
                Test.Assert((delete.HasValue && !delete.Value),
                string.Format("expected LoggingOperations for deleting is false, actually it's '{0}'", delete));
            }
        }

        public static void ValidateLoggingProperties(CloudStorageAccount account, Constants.ServiceType serviceType, int? retentionDays, string loggingOperations)
        {
            ServiceProperties properties = WaitForLoggingPropertyTakingEffect(account, serviceType, retentionDays, loggingOperations);
            Test.Assert(properties.Logging.RetentionDays == retentionDays,
                String.Format("expected LoggingRetentionDays {0} = {1}", retentionDays, properties.Logging.RetentionDays));

            LoggingOperations current = (LoggingOperations)Enum.Parse(typeof(LoggingOperations), loggingOperations, true);
            Test.Assert(current.Equals(properties.Logging.LoggingOperations),
                String.Format("expected LoggingOperations {0} = {1}", loggingOperations, properties.Logging.LoggingOperations.ToString()));
        }

        public static void ValidateMetricsProperties(CloudStorageAccount account, Constants.ServiceType serviceType,
            Constants.MetricsType metricsType, int? retentionDays, string metricsLevel)
        {
            ServiceProperties properties = WaitForMetricsPropertyTakingEffect(account, serviceType, metricsType, retentionDays, metricsLevel);
            if (metricsType == Constants.MetricsType.Hour)
            {
                if (!metricsLevel.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    Test.Assert(properties.HourMetrics.RetentionDays == retentionDays,
                    String.Format("expected MetricsRetentionDays {0} = {1}", retentionDays, properties.HourMetrics.RetentionDays));
                }

                Test.Assert(metricsLevel.Equals(properties.HourMetrics.MetricsLevel.ToString(), StringComparison.OrdinalIgnoreCase),
                    String.Format("expected MetricsLevel {0} = {1}", metricsLevel, properties.HourMetrics.MetricsLevel.ToString()));
            }
            else if (metricsType == Constants.MetricsType.Minute)
            {
                if (!metricsLevel.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    Test.Assert(properties.MinuteMetrics.RetentionDays == retentionDays,
                        String.Format("expected MetricsRetentionDays {0} = {1}", retentionDays, properties.MinuteMetrics.RetentionDays));
                }

                Test.Assert(metricsLevel.Equals(properties.MinuteMetrics.MetricsLevel.ToString(), StringComparison.OrdinalIgnoreCase),
                    String.Format("expected MetricsLevel {0} = {1}", metricsLevel, properties.MinuteMetrics.MetricsLevel.ToString()));
            }
        }

        public static ServiceProperties GetServiceProperties(CloudStorageAccount account, Constants.ServiceType serviceType)
        {
            ServiceProperties properties = null;
            switch (serviceType)
            {
                case Constants.ServiceType.Blob:
                    properties = account.CreateCloudBlobClient().GetServiceProperties();
                    break;
                case Constants.ServiceType.Table:
                    properties = account.CreateCloudTableClient().GetServiceProperties();
                    break;
                case Constants.ServiceType.Queue:
                    properties = account.CreateCloudQueueClient().GetServiceProperties();
                    break;
            }
            return properties;
        }

        internal static ServiceProperties WaitForLoggingPropertyTakingEffect(CloudStorageAccount account, Constants.ServiceType serviceType, int? retentionDays, string loggingOperations)
        {
            int retry = 0;
            int wait = 1000;
            ServiceProperties properties;

            do
            {
                properties = GetServiceProperties(account, serviceType);

                LoggingOperations current = (LoggingOperations)Enum.Parse(typeof(LoggingOperations), loggingOperations, true);
                if (properties.Logging.RetentionDays == retentionDays && current.Equals(properties.Logging.LoggingOperations))
                {
                    break;
                }
                else
                {
                    Thread.Sleep(wait);
                    wait *= 2;
                    retry++;
                }
            }
            while (retry <= RetryLimit);

            return properties;
        }

        internal static ServiceProperties WaitForMetricsPropertyTakingEffect(CloudStorageAccount account, Constants.ServiceType serviceType,
            Constants.MetricsType metricsType, int? retentionDays, string metricsLevel)
        {
            int retry = 0;
            int wait = 1000;
            ServiceProperties properties;

            do
            {
                properties = GetServiceProperties(account, serviceType);

                if ((metricsType == Constants.MetricsType.Hour &&
                    metricsLevel.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                    metricsLevel.Equals(properties.HourMetrics.MetricsLevel.ToString(), StringComparison.OrdinalIgnoreCase))
                    ||
                    (metricsType == Constants.MetricsType.Hour &&
                    properties.HourMetrics.RetentionDays == retentionDays &&
                    metricsLevel.Equals(properties.HourMetrics.MetricsLevel.ToString(), StringComparison.OrdinalIgnoreCase))
                    ||
                    (metricsType == Constants.MetricsType.Minute &&
                    metricsLevel.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                    metricsLevel.Equals(properties.MinuteMetrics.MetricsLevel.ToString(), StringComparison.OrdinalIgnoreCase))
                    ||
                    (metricsType == Constants.MetricsType.Minute &&
                    properties.MinuteMetrics.RetentionDays == retentionDays &&
                    metricsLevel.Equals(properties.MinuteMetrics.MetricsLevel.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }
                else
                {
                    Thread.Sleep(wait);
                    wait *= 2;
                    retry++;
                }
            }
            while (retry <= RetryLimit);

            return properties;
        }

        public static string GenRandomMetricsLevel()
        {
            string[] levels = new string[] { "None", "Service", "ServiceAndApi" };
            int i = Utility.GetRandomTestCount(0, 3);
            return levels[i];
        }

        public static string GenRandomLoggingOperations()
        {
            string[] opertions = new string[] { "All", "None", Utility.GenRandomCombination(LoggingOperationList, ", ") };
            int i = Utility.GetRandomTestCount(0, 3);
            return opertions[i];
        }

        public static string GenRandomEntityPermission(StorageObjectType blobType)
        {
            switch (blobType)
            {
                case StorageObjectType.Container:
                    return Utility.GenRandomCombination(ContainerPermission);
                case StorageObjectType.Blob:
                    return Utility.GenRandomCombination(BlobPermission);
                case StorageObjectType.Table:
                    return Utility.GenRandomCombination(TablePermissionNode);
                case StorageObjectType.Queue:
                    return Utility.GenRandomCombination(QueuePermission);

                default:
                    throw new Exception("unkown blob type : " + blobType);
            }
        }

        public static string GetBufferMD5(byte[] buffer)
        {
            byte[] md5 = Helper.GetMD5(buffer);
            return Convert.ToBase64String(md5);
        }

        public static IEnumerable<T> RandomlySelect<T>(this IEnumerable<T> collection, int numberOfElementsToSelect, Random r = null)
        {
            if (r == null)
            {
                r = new Random();
            }

            var list = new List<T>(collection);
            for (int i = 0; i < numberOfElementsToSelect; i++)
            {
                int index = r.Next(list.Count);
                yield return list[index];
                list.RemoveAt(index);
            }
        }
    }
}
