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
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.Azure;
    using Microsoft.Azure.Common.Authentication;
    using Microsoft.Azure.Common.Authentication.Factories;
    using Microsoft.Azure.Common.Authentication.Models;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.File;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Queue.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.Azure.Subscriptions;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using StorageBlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;

    internal static class Utility
    {
        public static List<string> LoggingOperationList = new List<string>() { "Read", "Write", "Delete" };
        public static List<string> ContainerPermission = new List<string>() { "r", "w", "d", "l" };
        public static List<string> BlobPermission = new List<string>() { "r", "w", "d" };
        public static List<string> TablePermissionPS = new List<string>() { "r", "q", "a", "u", "d" };
        public static List<string> TablePermissionNode = new List<string>() { "r", "a", "u", "d" };
        public static List<string> QueuePermission = new List<string>() { "r", "a", "u", "p" };
        public static List<string> SharePermission = new List<string>() { "r", "w", "d", "l" };
        public static List<string> FilePermission = new List<string>() { "r", "w", "d" };

        internal static int RetryLimit = 7;

        /// <summary>
        /// Generate a random string for azure object name
        /// </summary> 
        /// <param name="prefix">usually it's a string of letters, to avoid naming rule breaking</param>
        /// <param name="len">the length of random string after the prefix</param>
        /// <returns>a random string for azure object name</returns>
        public static string GenNameString(string prefix, int len = 8)
        {
            string guidString = Guid.NewGuid().ToString().Replace("-", "");

            string nameString = prefix;

            while (len > 0)
            {
                nameString = nameString + guidString.Substring(0, Math.Min(len, guidString.Length));
                len -= Math.Min(len, guidString.Length);
            }

            return nameString;
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

        public static string GenConnectionString(string storageAccountName, string storageAccountKey, bool useHttps = true, string endPoint = "")
        {
            if (string.IsNullOrEmpty(endPoint))
            {
                return String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", storageAccountName, storageAccountKey);
            }

            string[] endpoints = GetStorageEndPoints(storageAccountName, useHttps, endPoint);
            return String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};BlobEndpoint={2};QueueEndpoint={3};TableEndpoint={4};FileEndpoint={5}",
                storageAccountName,
                storageAccountKey,
                endpoints[0],
                endpoints[1],
                endpoints[2],
                endpoints[3]);
        }

        public static AzureRMProfile GetProfile()
        {
            AzureRMProfile profile = null;

            if (GetAutoLogin())
            {
                AzureSubscription newSubscription = null;
                AzureTenant newTenant = null;
                string passwd = Test.Data.Get("AADPassword");
                SecureString securePassword = null;
                profile = new AzureRMProfile();

                unsafe
                {
                    fixed (char* ppw = passwd.ToCharArray())
                    {
                        securePassword = new SecureString(ppw, passwd.Length);
                    }
                }

                ShowDialog promptBehavior = securePassword == null ? ShowDialog.Always : ShowDialog.Never;

                AzureAccount account = new AzureAccount()
                        {
                            Id = Test.Data.Get("AADUser"),
                            Type = AzureAccount.AccountType.User
                        };

                AzureEnvironment environment = AzureEnvironment.PublicEnvironments[EnvironmentName.AzureCloud];

                // (tenant is not provided and subscription is present) OR
                // (tenant is not provided and subscription is not provided)
                foreach (var tenant in ListAccountTenants(account, environment, securePassword, promptBehavior))
                {
                    if (TryGetTenantSubscription(account, environment, tenant.Id.ToString(), Test.Data.Get("AzureSubscriptionID"), securePassword, ShowDialog.Auto, out newSubscription, out newTenant))
                    {
                        break;
                    }
                }

                if (newSubscription == null)
                {
                    throw new InvalidOperationException("Subscription was not found.");
                }

                profile.Context = new AzureContext(newSubscription, account, environment, newTenant);
                profile.Context.TokenCache = TokenCache.DefaultShared.Serialize();
            }
            else
            {
                AzureSession.DataStore = new DiskDataStore();
                profile = new AzureRMProfile(Test.Data.Get("ProfilePath"));
            }

            return profile;
        }

        private static List<AzureTenant> ListAccountTenants(AzureAccount account, AzureEnvironment environment, SecureString password, ShowDialog promptBehavior)
        {
            var commonTenantToken = AzureSession.AuthenticationFactory.Authenticate(
                account,
                environment,
                AuthenticationFactory.CommonAdTenant,
                password,
                promptBehavior,
                TokenCache.DefaultShared);

            using (var subscriptionClient = AzureSession.ClientFactory.CreateCustomClient<SubscriptionClient>(
                    new TokenCloudCredentials(commonTenantToken.AccessToken),
                    environment.GetEndpointAsUri(AzureEnvironment.Endpoint.ResourceManager)))
            {
                return subscriptionClient.Tenants.List().TenantIds
                    .Select(ti => new AzureTenant() { Id = new Guid(ti.TenantId), Domain = GetDomain(commonTenantToken) })
                    .ToList();
            }
        }

        private static bool TryGetTenantSubscription(
            AzureAccount account,
            AzureEnvironment environment,
            string tenantId,
            string subscriptionId,
            SecureString password,
            ShowDialog promptBehavior,
            out AzureSubscription subscription,
            out AzureTenant tenant)
        {
            var accessToken = AzureSession.AuthenticationFactory.Authenticate(
                    account,
                    environment,
                    tenantId,
                    password,
                    promptBehavior,
                    TokenCache.DefaultShared);
            using (var subscriptionClient = AzureSession.ClientFactory.CreateCustomClient<SubscriptionClient>(
                new TokenCloudCredentials(accessToken.AccessToken),
                environment.GetEndpointAsUri(AzureEnvironment.Endpoint.ResourceManager)))
            {
                Microsoft.Azure.Subscriptions.Models.Subscription subscriptionFromServer = null;

                subscriptionFromServer = subscriptionClient.Subscriptions.Get(subscriptionId).Subscription;

                if (subscriptionFromServer != null)
                {
                    subscription = new AzureSubscription
                    {
                        Id = new Guid(subscriptionFromServer.SubscriptionId),
                        Account = accessToken.UserId,
                        Environment = environment.Name,
                        Name = subscriptionFromServer.DisplayName,
                        Properties = new Dictionary<AzureSubscription.Property, string> { { AzureSubscription.Property.Tenants, accessToken.TenantId } }
                    };

                    account.Properties[AzureAccount.Property.Tenants] = accessToken.TenantId;
                    tenant = new AzureTenant();
                    tenant.Id = new Guid(accessToken.TenantId);
                    tenant.Domain = GetDomain(accessToken);
                    return true;
                }

                subscription = null;
                tenant = null;
                return false;
            }
        }

        private static string GetDomain(IAccessToken token)
        {
            if (token != null && token.UserId != null && token.UserId.Contains('@'))
            {
                return token.UserId.Split(
                    new[] { '@' },
                    StringSplitOptions.RemoveEmptyEntries).Last();
            }

            return null;
        }

        public static CertificateCloudCredentials GetCertificateCloudCredential()
        {
            string certFile = Test.Data.Get("ManagementCert");
            string certPassword = Test.Data.Get("CertPassword");
            X509Certificate2 cert = new X509Certificate2(certFile, certPassword);
            return new CertificateCloudCredentials(Test.Data.Get("AzureSubscriptionID"), cert);
        }

        public static TokenCloudCredentials GetTokenCloudCredential()
        {
            AuthenticationContext context = new AuthenticationContext(string.Format("https://login.windows.net/{0}", Test.Data.Get("AADRealm")));
            ClientCredential clientCred = new ClientCredential(Test.Data.Get("AADClient"), Test.Data.Get("AADPassword"));
            AuthenticationResult result = context.AcquireToken("https://management.core.windows.net/", clientCred);
            return new TokenCloudCredentials(Test.Data.Get("AzureSubscriptionID"), result.AccessToken);
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
        /// Convert Hashtables to string
        /// The output would be "k1=v1;k2=v2"
        /// </summary>
        public static string ConvertTables(Hashtable[] tables)
        {
            string result = string.Empty;
            foreach (Hashtable table in tables)
            {    
                if (!string.IsNullOrEmpty(result))
                {
                    result += ";";
                }
                 
                if (table.ContainsKey("Name") && table.ContainsKey("Value"))
                {
                    result += table["Name"] + "=" + table["Value"];
                }
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

        public static bool GetAutoLogin()
        {
            string autoLogin = Test.Data.Get("AutoLogin");

            if (!string.IsNullOrEmpty(autoLogin))
            {
                return bool.Parse(autoLogin);
            }

            return false;
        }

        public static AzureEnvironment GetTargetEnvironment()
        {
            AzureEnvironment environment = null;
            string storageEndpoint = Test.Data.Get("StorageEndPoint");
            foreach (string key in AzureEnvironment.PublicEnvironments.Keys)
            {
                if (AzureEnvironment.PublicEnvironments[key].Endpoints[AzureEnvironment.Endpoint.StorageEndpointSuffix] == storageEndpoint)
                {
                    environment = AzureEnvironment.PublicEnvironments[key];
                    break;
                }
            }

            return environment;
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

        public static void VerifyCopyState(CopyState expectedCopyState, CLICopyState actualCopyState)
        {
            Test.Assert(string.Equals(actualCopyState.CopyId, expectedCopyState.CopyId), "Copy Id should be the same, {0} == {1}", actualCopyState.CopyId, expectedCopyState.CopyId);
            Test.Assert(string.Equals(actualCopyState.StatusDescription, expectedCopyState.StatusDescription), "StatusDescription should be the same, {0} == {1}", actualCopyState.StatusDescription, expectedCopyState.StatusDescription);
            Test.Assert(actualCopyState.Status == expectedCopyState.Status, "Status should be the same, {0} == {1}", actualCopyState.Status, expectedCopyState.Status);
            Test.Assert(actualCopyState.Source == expectedCopyState.Source, "Source should be the same, {0} == {1}", actualCopyState.Source.ToString(), expectedCopyState.Source.ToString());
        }

        public static void WaitCopyToFinish(Func<CopyState> getCopyState)
        {
            while (true)
            {
                CopyState copyState = getCopyState();

                if (copyState == null)
                {
                    Test.Error("Copy state doesn't exist, starting copying may not work.");
                    return;
                }

                if (copyState.Status != CopyStatus.Pending)
                {
                    return;
                }

                Thread.Sleep(2000);
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

            if (retry > RetryLimit)
            {
                Test.Warn("Has been up to retry limit, this case may fail due to setting has not taken effect yet.");
            }

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

            if (retry > RetryLimit)
            {
                Test.Warn("Has been up to retry limit, this case may fail due to setting has not taken effect yet.");
            }

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

        public static List<RawStoredAccessPolicy> SetUpStoredAccessPolicyData<T>(bool skipTableQPermission = false)
        {
            List<RawStoredAccessPolicy> sampleStordAccessPolicies = new List<RawStoredAccessPolicy>();
            string permission1 = null;
            string permission2 = null;
            string permission3 = null;

            if (typeof(T) == typeof(SharedAccessTablePolicy))
            {
                permission1 = skipTableQPermission ? "raud" : "rqaud";
                permission2 = "aud";
                permission3 = "ud";
            }
            else if ((typeof(T) == typeof(SharedAccessBlobPolicy))
                || (typeof(T) == typeof(SharedAccessFilePolicy)))
            {
                permission1 = "rwdl";
                permission2 = "wdl";
                permission3 = "dl";
            }
            else if (typeof(T) == typeof(SharedAccessQueuePolicy))
            {
                permission1 = "raup";
                permission2 = "aup";
                permission3 = "up";
            }
            else
            {
                throw new Exception("Unknown Policy Type!");
            }

            //normal one
            string policy1 = GenNameString("p", 1);
            DateTime? startTime1 = DateTime.Today.AddDays(-1);
            DateTime? expiryTime1 = DateTime.Today.AddDays(4);
            sampleStordAccessPolicies.Add(new RawStoredAccessPolicy(policy1, startTime1, expiryTime1, permission1));

            //StartTime in the future, permission is subset, ExpiryTime null
            string policy2 = GenNameString("p", 2);
            DateTime? startTime2 = DateTime.Today.AddDays(2);
            DateTime? expiryTime2 = null;
            sampleStordAccessPolicies.Add(new RawStoredAccessPolicy(policy2, startTime2, expiryTime2, permission2));

            //StartTime null, permission subset, ExpirtyTime in the past
            string policy3 = GenNameString("p", 3);
            DateTime? startTime3 = null;
            DateTime? expiryTime3 = DateTime.Today.AddDays(-3);
            sampleStordAccessPolicies.Add(new RawStoredAccessPolicy(policy3, startTime3, expiryTime3, permission3));

            //Permission empty, StartTime null, ExpirtyTime null
            string policy4 = GenNameString("p", 4);
            DateTime? startTime4 = null;
            DateTime? expiryTime4 = null;
            string permission4 = string.Empty;
            sampleStordAccessPolicies.Add(new RawStoredAccessPolicy(policy4, startTime4, expiryTime4, permission4));

            //All null
            string policy5 = GenNameString("p", 5);
            DateTime? expiryTime5 = null;
            DateTime? startTime5 = null;
            string permission5 = null;
            sampleStordAccessPolicies.Add(new RawStoredAccessPolicy(policy5, startTime5, expiryTime5, permission5));

            return sampleStordAccessPolicies;
        }

        public static RawStoredAccessPolicy GetExpectedStoredAccessPolicy(RawStoredAccessPolicy originPolicy, RawStoredAccessPolicy newPolicy)
        {
            return new RawStoredAccessPolicy(
                originPolicy.PolicyName,
                newPolicy.StartTime ?? originPolicy.StartTime,
                newPolicy.ExpiryTime ?? originPolicy.ExpiryTime,
                newPolicy.Permission ?? originPolicy.Permission);
        }

        public static void ClearStoredAccessPolicy<T>(T serviceRef)
        {
            if (typeof(T) == typeof(CloudTable))
            {
                TablePermissions permissions = ((CloudTable)(Object)serviceRef).GetPermissions();
                permissions.SharedAccessPolicies.Clear();
                ((CloudTable)(Object)serviceRef).SetPermissions(permissions);
            }
            else if (typeof(T) == typeof(CloudBlobContainer))
            {
                BlobContainerPermissions permissions = ((CloudBlobContainer)(Object)serviceRef).GetPermissions();
                permissions.SharedAccessPolicies.Clear();
                ((CloudBlobContainer)(Object)serviceRef).SetPermissions(permissions);
            }
            else if (typeof(T) == typeof(CloudQueue))
            {
                QueuePermissions permissions = ((CloudQueue)(Object)serviceRef).GetPermissions();
                permissions.SharedAccessPolicies.Clear();
                ((CloudQueue)(Object)serviceRef).SetPermissions(permissions);
            }
            else if (typeof(T) == typeof(CloudFileShare))
            {
                FileSharePermissions permissions = ((CloudFileShare)(Object)serviceRef).GetPermissions();
                permissions.SharedAccessPolicies.Clear();
                ((CloudFileShare)(Object)serviceRef).SetPermissions(permissions);
            }
            else
            {
                throw new Exception("Unknown Service Type!");
            }
        }

        public static T SetupSharedAccessPolicy<T>(DateTime? startTime, DateTime? expiryTime, string permission)
        {
            if (!(typeof(T) == typeof(SharedAccessTablePolicy) ||
               typeof(T) == typeof(SharedAccessBlobPolicy) ||
               typeof(T) == typeof(SharedAccessQueuePolicy) ||
               typeof(T) == typeof(SharedAccessFilePolicy)))
            {
                throw new Exception("Unknown Policy Type!");
            }

            T policy = (T)Activator.CreateInstance(typeof(T));
            if (startTime != null)
            {
                ((dynamic)policy).SharedAccessStartTime = (DateTimeOffset?)startTime.Value.ToUniversalTime();
            }

            if (expiryTime != null)
            {
                ((dynamic)policy).SharedAccessExpiryTime = (DateTimeOffset?)expiryTime.Value.ToUniversalTime();
            }
            Utility.SetupAccessPolicyPermission(policy, permission);
            return policy;
        }

        public static Dictionary<string, object> ConstructGetPolicyOutput<T>(T policy, string policyName)
        {
            if (!(typeof(T) == typeof(SharedAccessTablePolicy) ||
               typeof(T) == typeof(SharedAccessBlobPolicy) ||
               typeof(T) == typeof(SharedAccessQueuePolicy) ||
               typeof(T) == typeof(SharedAccessFilePolicy)))
            {
                throw new Exception("Unknown Policy Type!");
            }

            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("Policy", policyName);
            dic.Add("Permissions", ((dynamic)policy).Permissions);
            dic.Add("StartTime", ((dynamic)policy).SharedAccessStartTime);
            dic.Add("ExpiryTime", ((dynamic)policy).SharedAccessExpiryTime);

            return dic;
        }


        public static void ValidateStoredAccessPolicies<T>(IDictionary<string, T> actualSharedPolicies, IDictionary<string, T> expectedSharedPolicies)
        {
            foreach (string policyName in expectedSharedPolicies.Keys)
            {
                Test.Info("Validate stored access policy:{0}", policyName);
                Test.Assert(actualSharedPolicies.Keys.Contains(policyName), string.Format("It should contain policy {0}.", policyName));
                T actualPolicy = actualSharedPolicies[policyName];
                Test.Assert(actualPolicy != null, "Actual policy should not be null.");
                T expectedPolicy = expectedSharedPolicies[policyName];
                Test.Assert(expectedPolicy != null, "Expected policy should not be null.");
                ValidateStoredAccessPolicy<T>(actualPolicy, expectedPolicy);
            }
            Test.Assert((int)(expectedSharedPolicies.Count) == (int)(actualSharedPolicies.Count),
                string.Format("Expected count is {0} and the actual count is {1}.", expectedSharedPolicies.Count, actualSharedPolicies.Count));

        }

        public static void ValidateStoredAccessPolicy<T>(T actualPolicy, T expectedPolicy)
        {
            if (!(typeof(T) == typeof(SharedAccessTablePolicy) ||
                typeof(T) == typeof(SharedAccessBlobPolicy) ||
                typeof(T) == typeof(SharedAccessQueuePolicy) ||
                typeof(T) == typeof(SharedAccessFilePolicy)))
            {
                throw new Exception("Unknown Service Type!");
            }

            Test.Assert(((dynamic)expectedPolicy).SharedAccessStartTime == ((dynamic)actualPolicy).SharedAccessStartTime,
                string.Format("The expected StartTime is: {0} and actual StartTime is: {1}", ((dynamic)expectedPolicy).SharedAccessStartTime, ((dynamic)actualPolicy).SharedAccessStartTime));

            Test.Assert(((dynamic)expectedPolicy).SharedAccessExpiryTime == ((dynamic)actualPolicy).SharedAccessExpiryTime,
                string.Format("The expected ExpiryTime is: {0} and actual ExpiryTime is: {1}", ((dynamic)expectedPolicy).SharedAccessExpiryTime, ((dynamic)actualPolicy).SharedAccessExpiryTime));

            Test.Assert(((dynamic)expectedPolicy).Permissions == ((dynamic)actualPolicy).Permissions,
                string.Format("The expected Permissions is: {0} and actual Permissions is: {1}", ((dynamic)expectedPolicy).Permissions, ((dynamic)actualPolicy).Permissions));
        }

        public static void SetupAccessPolicyPermission<T>(T policy, string permission)
        {
            if (typeof(T) == typeof(SharedAccessTablePolicy))
            {
                SetupAccessPolicyPermission((SharedAccessTablePolicy)(Object)policy, permission);
            }
            else if (typeof(T) == typeof(SharedAccessBlobPolicy))
            {
                SetupAccessPolicyPermission((SharedAccessBlobPolicy)(Object)policy, permission);
            }
            else if ((typeof(T) == typeof(SharedAccessQueuePolicy)))
            {
                SetupAccessPolicyPermission((SharedAccessQueuePolicy)(Object)policy, permission);
            }
            else if (typeof(T) == typeof(SharedAccessFilePolicy))
            {
                SetupAccessPolicyPermission((SharedAccessFilePolicy)(Object)policy, permission);
            }
            else
            {
                throw new Exception("Unknown Service Type!");
            }
        }

        public static string SqueezeSpaces(string value)
        {
            return Regex.Replace(value, "\\s{2,}", " ");
        }

        public static CLICopyState GetCopyState(Agent agent, Language lang)
        {
            if (lang == Language.PowerShell)
            {
                return new CLICopyState(agent.Output[0][PowerShellAgent.BaseObject] as CopyState);
            }
            else
            {
                CLICopyState state = new CLICopyState();
                string progess = agent.Output[0]["copyProgress"] as string;
                long bytesCopied = 0;
                long totalBytes = 0;
                if (!string.IsNullOrEmpty(progess))
                {
                    int index = progess.IndexOf("/");
                    long.TryParse(progess.Substring(0, index), out bytesCopied);
                    long.TryParse(progess.Substring(index + 1), out totalBytes);
                }

                string time = null;
                if (agent.Output[0].ContainsKey("copyCompletionTime"))
                {
                    time = agent.Output[0]["copyCompletionTime"] as string;
                }

                DateTimeOffset completionTime = new DateTimeOffset();
                DateTimeOffset.TryParse(time, out completionTime);

                string raw = agent.Output[0]["copySource"] as string;
                Uri source = new Uri(raw);

                raw = agent.Output[0]["copyStatus"] as string;
                CopyStatus status;
                Enum.TryParse<CopyStatus>(raw, true, out status);

                string statusDescription = null;
                if (agent.Output[0].ContainsKey("copyStatusDescription"))
                {
                    statusDescription = agent.Output[0]["copyStatusDescription"] as string;
                }

                state.BytesCopied = bytesCopied;
                state.CompletionTime = completionTime;
                state.CopyId = agent.Output[0]["copyId"] as string;
                state.Source = source;
                state.Status = status;
                state.StatusDescription = statusDescription;
                state.TotalBytes = totalBytes;

                return state;
            }
        }

        public static int ParseIntFromJsonOutput(Dictionary<string, object> output, string key)
        {
            int result = 0;
            if (output.ContainsKey(key))
            {
                int.TryParse(output[key].ToString(), out result);
            }

            return result;
        }

        public static string ParseStringFromJsonOutput(Dictionary<string, object> output, string key)
        {
            return output.ContainsKey(key) ? output[key] as string : null;
        }

        public static bool ParseBoolFromJsonOutput(Dictionary<string, object> output, string key)
        {
            return output.ContainsKey(key) ? bool.Parse(output[key].ToString()) : false;
        }

        public static T? ParseEnumFromJsonOutput<T>(Dictionary<string, object> output, string key) where T : struct
        {
            T result;
            if (output.ContainsKey(key))
            {
                if (Enum.TryParse<T>(output[key] as string, true, out result))
                {
                    return result;
                } 
                else
                {
                    int value = 0;
                    if (int.TryParse(output[key].ToString(), out value))
                    {
                        return (T)Enum.ToObject(typeof(T), value);
                    }
                }
            }

            return (T?)null;
        }

        /// <summary>
        /// Set up shared access policy permission for SharedAccessTablePolicy
        /// </summary>
        /// <param name="policy">SharedAccessTablePolicy object</param>
        /// <param name="permission">Permission</param>
        internal static void SetupAccessPolicyPermission(SharedAccessTablePolicy policy, string permission)
        {
            if (string.IsNullOrEmpty(permission)) return;
            policy.Permissions = SharedAccessTablePermissions.None;
            permission = permission.ToLower();
            foreach (char op in permission)
            {
                switch (op)
                {
                    case Permission.Add:
                        policy.Permissions |= SharedAccessTablePermissions.Add;
                        break;
                    case Permission.Update:
                        policy.Permissions |= SharedAccessTablePermissions.Update;
                        break;
                    case Permission.Delete:
                        policy.Permissions |= SharedAccessTablePermissions.Delete;
                        break;
                    case Permission.Read:
                    case Permission.Query:
                        policy.Permissions |= SharedAccessTablePermissions.Query;
                        break;
                    default:
                        throw new Exception("Unknown Permission Type!");
                }
            }
        }


        /// <summary>
        /// Set up shared access policy permission for SharedAccessBlobPolicy
        /// </summary>
        /// <param name="policy">SharedAccessBlobPolicy object</param>
        /// <param name="permission">Permission</param>
        internal static void SetupAccessPolicyPermission(SharedAccessBlobPolicy policy, string permission)
        {
            if (string.IsNullOrEmpty(permission)) return;
            policy.Permissions = SharedAccessBlobPermissions.None;
            permission = permission.ToLower();
            foreach (char op in permission)
            {
                switch (op)
                {
                    case Permission.Read:
                        policy.Permissions |= SharedAccessBlobPermissions.Read;
                        break;
                    case Permission.Write:
                        policy.Permissions |= SharedAccessBlobPermissions.Write;
                        break;
                    case Permission.Delete:
                        policy.Permissions |= SharedAccessBlobPermissions.Delete;
                        break;
                    case Permission.List:
                        policy.Permissions |= SharedAccessBlobPermissions.List;
                        break;
                    default:
                        throw new Exception("Unknown Permission Type!");
                }
            }
        }

        /// <summary>
        /// Set up shared access policy permission for SharedAccessFilePolicy
        /// </summary>
        /// <param name="policy">SharedAccessFilePolicy object</param>
        /// <param name="permission">Permission</param>
        internal static void SetupAccessPolicyPermission(SharedAccessFilePolicy policy, string permission)
        {
            if (string.IsNullOrEmpty(permission)) return;
            policy.Permissions = SharedAccessFilePermissions.None;
            permission = permission.ToLower();
            foreach (char op in permission)
            {
                switch (op)
                {
                    case Permission.Read:
                        policy.Permissions |= SharedAccessFilePermissions.Read;
                        break;
                    case Permission.Write:
                        policy.Permissions |= SharedAccessFilePermissions.Write;
                        break;
                    case Permission.Delete:
                        policy.Permissions |= SharedAccessFilePermissions.Delete;
                        break;
                    case Permission.List:
                        policy.Permissions |= SharedAccessFilePermissions.List;
                        break;
                    default:
                        throw new Exception("Unknown Permission Type!");
                }
            }
        }

        /// <summary>
        /// Set up shared access policy permission for SharedAccessQueuePolicy
        /// </summary>
        /// <param name="policy">SharedAccessQueuePolicy object</param>
        /// <param name="permission">Permisson</param>
        internal static void SetupAccessPolicyPermission(SharedAccessQueuePolicy policy, string permission)
        {
            if (string.IsNullOrEmpty(permission)) return;
            policy.Permissions = SharedAccessQueuePermissions.None;
            permission = permission.ToLower();
            foreach (char op in permission)
            {
                switch (op)
                {
                    case Permission.Read:
                        policy.Permissions |= SharedAccessQueuePermissions.Read;
                        break;
                    case Permission.Add:
                        policy.Permissions |= SharedAccessQueuePermissions.Add;
                        break;
                    case Permission.Update:
                        policy.Permissions |= SharedAccessQueuePermissions.Update;
                        break;
                    case Permission.Process:
                        policy.Permissions |= SharedAccessQueuePermissions.ProcessMessages;
                        break;
                    default:
                        throw new Exception("Unknown Permission Type!");
                }
            }
        }

        internal static void WaitForPolicyBecomeValid<T>(T resource, Utility.RawStoredAccessPolicy expectedPolicy = null, int expectedCount = 1)
        {
            DateTimeOffset start = DateTimeOffset.Now;

            bool found = true;
            while (((dynamic)resource).GetPermissions().SharedAccessPolicies.Keys.Count != expectedCount)
            {
                if ((DateTimeOffset.Now - start) <= TimeSpan.FromSeconds(45))
                {
                    Test.Info("Sleep and retry to get the policies again");
                    Thread.Sleep(5000);
                }
                else
                {
                    Test.Warn("No policy was found");
                    found = false;
                    break;
                }
            }

            if (found && expectedCount == 1 && expectedPolicy != null)
            {
                bool match = false;
                start = DateTimeOffset.Now;

                do
                {
                    dynamic policy = null;
                    if (typeof(T) == typeof(CloudBlobContainer))
                    {
                        SharedAccessBlobPolicy output = null;
                        match = ((dynamic)resource).GetPermissions().SharedAccessPolicies.TryGetValue(expectedPolicy.PolicyName, out output);
                        policy = output;
                    }
                    else if (typeof(T) == typeof(CloudTable))
                    {
                        SharedAccessTablePolicy output = null;
                        match = ((dynamic)resource).GetPermissions().SharedAccessPolicies.TryGetValue(expectedPolicy.PolicyName, out output);
                        policy = output;
                    }
                    else if (typeof(T) == typeof(CloudQueue))
                    {
                        SharedAccessQueuePolicy output = null;
                        match = ((dynamic)resource).GetPermissions().SharedAccessPolicies.TryGetValue(expectedPolicy.PolicyName, out output);
                        policy = output;
                    }
                    else if (typeof(T) == typeof(CloudFileShare))
                    {
                        SharedAccessFilePolicy output = null;
                        match = ((dynamic)resource).GetPermissions().SharedAccessPolicies.TryGetValue(expectedPolicy.PolicyName, out output);
                        policy = output;
                    }

                    match = match && IsEqualPolicy(policy, expectedPolicy);

                    if (!match)
                    {
                        if ((DateTimeOffset.Now - start) <= TimeSpan.FromSeconds(45))
                        {
                            Test.Info("Sleep and retry to get the policies again");
                            Thread.Sleep(5000);
                        }
                        else
                        {
                            Test.Warn("No matching policy was found");
                            break;
                        }
                    }
                }
                while (!match);
            }
        }

        internal static bool IsEqualPolicy<T>(T actualPolicy, Utility.RawStoredAccessPolicy expectedPolicy)
        {
            string permissionTmp = expectedPolicy.Permission;

            if (actualPolicy is SharedAccessTablePolicy)
            {
                permissionTmp = expectedPolicy.Permission.Replace('q', 'r');
            }

            object[] parameter = { permissionTmp };
            var permission = permissionTmp != null ? typeof(T).GetMethod("PermissionsFromString").Invoke(null, parameter) : null;
            bool equal = IsEqualTime(((dynamic)actualPolicy).SharedAccessStartTime, expectedPolicy.StartTime);
            equal = equal && IsEqualTime(((dynamic)actualPolicy).SharedAccessExpiryTime, expectedPolicy.ExpiryTime);
            return equal && ((dynamic)actualPolicy).Permissions.Equals(permission);
        }

        internal static bool IsEqualTime(DateTimeOffset? actualTime, DateTime? expectedTime)
        {
            if (actualTime.HasValue != expectedTime.HasValue)
            {
                return false;
            }

            if (expectedTime.HasValue && (actualTime.Value.UtcDateTime - expectedTime.Value.ToUniversalTime()) != TimeSpan.FromSeconds(0))
            {
                return false;
            }

            return true;
        }

        public static class Permission
        {
            /// <summary>
            /// Read permission
            /// </summary>
            public const char Read = 'r';

            /// <summary>
            /// Write permission
            /// </summary>
            public const char Write = 'w';

            /// <summary>
            /// Delete permission
            /// </summary>
            public const char Delete = 'd';

            /// <summary>
            /// List permission
            /// </summary>
            public const char List = 'l';

            /// <summary>
            /// Update permission
            /// </summary>
            public const char Update = 'u';

            /// <summary>
            /// Add permission
            /// </summary>
            public const char Add = 'a';

            /// <summary>
            /// Process permission
            /// </summary>
            public const char Process = 'p';

            /// <summary>
            /// Query permission
            /// </summary>
            public const char Query = 'q';
        }

        public class RawStoredAccessPolicy
        {
            public string PolicyName { get; set; }
            public DateTime? StartTime { get; set; }
            public DateTime? ExpiryTime { get; set; }
            public string Permission { get; set; }

            public RawStoredAccessPolicy(string policyName, DateTime? startTime, DateTime? expiryTime, string permission)
            {
                PolicyName = policyName;
                StartTime = startTime;
                ExpiryTime = expiryTime;
                Permission = permission;
            }

            public RawStoredAccessPolicy(RawStoredAccessPolicy accessPolicy)
            {
                this.PolicyName = accessPolicy.PolicyName;
                this.StartTime = accessPolicy.StartTime;
                this.ExpiryTime = accessPolicy.ExpiryTime;
                this.Permission = accessPolicy.Permission;
            }

        }

        public class CLICopyState
        {
            public CLICopyState()
            {
                BytesCopied = 0;
                TotalBytes = 0;
            }

            public CLICopyState(CopyState state)
            {
                BytesCopied = state.BytesCopied;
                CompletionTime = state.CompletionTime;
                CopyId = state.CopyId;
                Source = state.Source;
                Status = state.Status;
                StatusDescription = state.StatusDescription;
                TotalBytes = state.TotalBytes;
            }

            public long? BytesCopied { get; internal set; }
            public DateTimeOffset? CompletionTime { get; internal set; }
            public string CopyId { get; internal set; }
            public Uri Source { get; internal set; }
            public CopyStatus Status { get; internal set; }
            public string StatusDescription { get; internal set; }
            public long? TotalBytes { get; internal set; }
        }
    }
}
