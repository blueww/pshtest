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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Management.Storage.ScenarioTest.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using StorageBlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;
using StorageBlob = Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

namespace Management.Storage.ScenarioTest.Util
{
    public class CloudBlobUtil
    {
        private CloudStorageAccount account;
        private CloudBlobClient client;
        private Random random;
        private const int PageBlobUnitSize = 512;
        private static List<string> HttpsCopyHosts;

        public string ContainerName
        {
            get;
            private set;
        }

        public string BlobName
        {
            get;
            private set;
        }

        public CloudBlob Blob
        {
            get;
            private set;
        }

        public CloudBlobContainer Container
        {
            get;
            private set;
        }

        private CloudBlobUtil()
        { }

        /// <summary>
        /// init cloud blob util
        /// </summary>
        /// <param name="account">storage account</param>
        public CloudBlobUtil(CloudStorageAccount account)
        {
            this.account = account;
            client = account.CreateCloudBlobClient();

            // Enable logging for blob service and enable $logs container
            ServiceProperties properties = client.GetServiceProperties();
            properties.Cors = new CorsProperties(); // Clear all CORS rule to eliminate the effect by CORS cases
            properties.Logging.LoggingOperations = LoggingOperations.All;
            client.SetServiceProperties(properties);

            random = new Random();
        }

        /// <summary>
        /// Create a random container with a random blob
        /// </summary>
        /// <param name="type"></param>
        /// <param name="blobNamePrefix">prefix of the blob name</param>
        public void SetupTestContainerAndBlob(string blobNamePrefix, StorageBlobType type = StorageBlobType.Unspecified)
        {
            ContainerName = Utility.GenNameString("container");
            BlobName = Utility.GenNameString(blobNamePrefix);
            CloudBlobContainer container = CreateContainer(ContainerName);
            Blob = CreateRandomBlob(container, BlobName, type);
            Container = container;
        }

        /// <summary>
        /// Create a random container with a random blob
        /// </summary>
        /// <param name="blobNamePrefix">prefix of the blob name</param>
        public void SetupTestContainerAndBlob(StorageBlobType type = StorageBlobType.Unspecified)
        {
            SetupTestContainerAndBlob(TestBase.SpecialChars, type);
        }

        /// <summary>
        /// clean test container and blob
        /// </summary>
        public void CleanupTestContainerAndBlob()
        {
            if (String.IsNullOrEmpty(ContainerName))
            {
                return;
            }

            RemoveContainer(ContainerName);
            ContainerName = string.Empty;
            BlobName = string.Empty;
            Blob = null;
            Container = null;
        }

        /// <summary>
        /// create a container with random properties and metadata
        /// </summary>
        /// <param name="containerName">container name</param>
        /// <returns>the created container object with properties and metadata</returns>
        public CloudBlobContainer CreateContainer(string containerName = "")
        {
            if (String.IsNullOrEmpty(containerName))
            {
                containerName = Utility.GenNameString("container");
            }

            CloudBlobContainer container = client.GetContainerReference(containerName);
            container.CreateIfNotExists(BlobContainerPublicAccessType.Blob);

            //there is no properties to set
            container.FetchAttributes();

            int minMetaCount = 1;
            int maxMetaCount = 5;
            int minMetaValueLength = 10;
            int maxMetaValueLength = 20;
            int count = random.Next(minMetaCount, maxMetaCount);
            for (int i = 0; i < count; i++)
            {
                string metaKey = Utility.GenNameString("metatest");
                int valueLength = random.Next(minMetaValueLength, maxMetaValueLength);
                string metaValue = Utility.GenNameString("metavalue-", valueLength);
                container.Metadata.Add(metaKey, metaValue);
            }

            container.SetMetadata();

            Test.Info(string.Format("create container '{0}'", containerName));
            return container;
        }

        public CloudBlobContainer CreateContainer(string containerName, BlobContainerPublicAccessType permission)
        {
            CloudBlobContainer container = CreateContainer(containerName);
            BlobContainerPermissions containerPermission = new BlobContainerPermissions();
            containerPermission.PublicAccess = permission;
            container.SetPermissions(containerPermission);
            return container;
        }

        /// <summary>
        /// create mutiple containers
        /// </summary>
        /// <param name="containerNames">container names list</param>
        /// <returns>a list of container object</returns>
        public List<CloudBlobContainer> CreateContainer(List<string> containerNames)
        {
            List<CloudBlobContainer> containers = new List<CloudBlobContainer>();

            foreach (string name in containerNames)
            {
                containers.Add(CreateContainer(name));
            }

            containers = containers.OrderBy(container => container.Name).ToList();

            return containers;
        }

        /// <summary>
        /// remove specified container
        /// </summary>
        /// <param name="Container">Cloud blob container object</param>
        public void RemoveContainer(CloudBlobContainer Container)
        {
            RemoveContainer(Container.Name);
        }

        /// <summary>
        /// remove specified container
        /// </summary>
        /// <param name="containerName">container name</param>
        public void RemoveContainer(string containerName)
        {
            CloudBlobContainer container = client.GetContainerReference(containerName);
            container.DeleteIfExists();
            Test.Info(string.Format("remove container '{0}'", containerName));
        }

        public void CleanupContainer(string containerName)
        {
            foreach (var listItem in client.GetContainerReference(containerName).ListBlobs(null, true, BlobListingDetails.All))
            {
                CloudBlob blob = listItem as CloudBlob;

                if (null != blob)
                {
                    blob.Delete();
                }
            }
        }

        /// <summary>
        /// remove a list containers
        /// </summary>
        /// <param name="containerNames">container names</param>
        public void RemoveContainer(List<string> containerNames)
        {
            foreach (string name in containerNames)
            {
                try
                {
                    RemoveContainer(name);
                }
                catch (Exception e)
                {
                    Test.Warn(string.Format("Can't remove container {0}. Exception: {1}", name, e.Message));
                }
            }
        }

        /// <summary>
        /// create a new page blob with random properties and metadata
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="blobName">blob name</param>
        /// <returns>CloudBlob object</returns>
        public CloudBlob CreatePageBlob(CloudBlobContainer container, string blobName, bool createBigBlob = false)
        {
            CloudPageBlob pageBlob = container.GetPageBlobReference(blobName);
            int size = (createBigBlob ? random.Next(102400, 204800) : random.Next(1, 10)) * PageBlobUnitSize;
            pageBlob.Create(size);

            byte[] buffer = new byte[size];
            // fill in random data
            random.NextBytes(buffer);
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                pageBlob.UploadFromStream(ms);
            }

            string md5sum = Convert.ToBase64String(Helper.GetMD5(buffer));
            pageBlob.Properties.ContentMD5 = md5sum;
            GenerateBlobPropertiesAndMetaData(pageBlob);
            Test.Info(string.Format("create page blob '{0}' in container '{1}', md5 = {2}", blobName, container.Name, md5sum));
            return pageBlob;
        }

        /// <summary>
        /// create a block blob with random properties and metadata
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="blobName">Block blob name</param>
        /// <returns>CloudBlob object</returns>
        public CloudBlob CreateBlockBlob(CloudBlobContainer container, string blobName, bool createBigBlob = false)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            string md5sum = string.Empty;
            int blobSize = createBigBlob ? random.Next(1024 * 1024 *50, 1024 * 1024 *100) : random.Next(1024 * 1024);
            byte[] buffer = new byte[blobSize];
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                random.NextBytes(buffer);
                //ms.Read(buffer, 0, buffer.Length);
                blockBlob.UploadFromStream(ms);
                md5sum = Convert.ToBase64String(Helper.GetMD5(buffer));
            }

            blockBlob.Properties.ContentMD5 = md5sum;
            GenerateBlobPropertiesAndMetaData(blockBlob);
            Test.Info(string.Format("create block blob '{0}' in container '{1}'", blobName, container.Name));
            return blockBlob;
        }
        
        /// <summary>
        /// create a new append blob with random properties and metadata
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="blobName">blob name</param>
        /// <returns>CloudAppendBlob object</returns>
        public CloudAppendBlob CreateAppendBlob(CloudBlobContainer container, string blobName, bool createBigBlob = false)
        {
            CloudAppendBlob appendBlob = container.GetAppendBlobReference(blobName);

            int size = createBigBlob ? random.Next(1024 * 1024 * 50, 1024 * 1024 * 100) : random.Next(1024 * 1024);
            appendBlob.CreateOrReplace();

            byte[] buffer = new byte[size];
            // fill in random data
            random.NextBytes(buffer);
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                appendBlob.UploadFromStream(ms);
            }

            string md5sum = Convert.ToBase64String(Helper.GetMD5(buffer));
            appendBlob.Properties.ContentMD5 = md5sum;
            GenerateBlobPropertiesAndMetaData(appendBlob);
            Test.Info(string.Format("create append blob '{0}' in container '{1}', md5 = {2}", blobName, container.Name, md5sum));
            return appendBlob;
        }

        /// <summary>
        /// generate random blob properties and metadata
        /// </summary>
        /// <param name="blob">CloudBlob object</param>
        private void GenerateBlobPropertiesAndMetaData(CloudBlob blob)
        {
            blob.Properties.ContentEncoding = Utility.GenNameString("encoding");
            blob.Properties.ContentLanguage = Utility.GenNameString("lang");

            int minMetaCount = 1;
            int maxMetaCount = 5;
            int minMetaValueLength = 10;
            int maxMetaValueLength = 20;
            int count = random.Next(minMetaCount, maxMetaCount);

            for (int i = 0; i < count; i++)
            {
                string metaKey = Utility.GenNameString("metatest");
                int valueLength = random.Next(minMetaValueLength, maxMetaValueLength);
                string metaValue = Utility.GenNameString("metavalue-", valueLength);
                blob.Metadata.Add(metaKey, metaValue);
            }

            blob.SetProperties();
            blob.SetMetadata();
            blob.FetchAttributes();
        }

        /// <summary>
        /// Create a blob with specified blob type
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="blobName">Blob name</param>
        /// <param name="type">Blob type</param>
        /// <returns>CloudBlob object</returns>
        public CloudBlob CreateBlob(CloudBlobContainer container, string blobName, StorageBlob.BlobType type)
        {
            if (type == StorageBlob.BlobType.BlockBlob)
            {
                return CreateBlockBlob(container, blobName);
            }
            else if (type == StorageBlob.BlobType.PageBlob)
            {
                return CreatePageBlob(container, blobName);
            }
            else if (type == StorageBlob.BlobType.AppendBlob)
            {
                return CreateAppendBlob(container, blobName);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Invalid blob type: {0}", type));
            }
        }

        /// <summary>
        /// create a list of blobs with random properties/metadata/blob type
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="blobName">a list of blob names</param>
        /// <returns>a list of cloud page blobs</returns>
        public List<CloudBlob> CreateRandomBlob(CloudBlobContainer container, List<string> blobNames, bool createBigBlob = false)
        {
            List<CloudBlob> blobs = new List<CloudBlob>();

            foreach (string blobName in blobNames)
            {
                blobs.Add(CreateRandomBlob(container, blobName, createBigBlob: createBigBlob));
            }

            blobs = blobs.OrderBy(blob => blob.Name).ToList();

            return blobs;
        }

        public List<CloudBlob> CreateRandomBlob(CloudBlobContainer container, bool createBigBlob = false)
        {
            int count = random.Next(1, 5);
            List<string> blobNames = new List<string>();
            for (int i = 0; i < count; i++)
            {
                blobNames.Add(Utility.GenNameString("blob"));
            }

            return CreateRandomBlob(container, blobNames, createBigBlob);
        }

        /// <summary>
        /// Create a list of blobs with random properties/metadata/blob type
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="blobName">Blob name</param>
        /// <param name="BlobType">type</param>
        /// <returns>CloudBlob object</returns>
        public CloudBlob CreateRandomBlob(CloudBlobContainer container, string blobName, StorageBlobType type = StorageBlobType.Unspecified, bool createBigBlob = false)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                blobName = Utility.GenNameString(TestBase.SpecialChars);
            }

            if (type == StorageBlobType.Unspecified)
            {
                int randomValue = random.Next(1, 4);
                switch (randomValue)
                {
                    case 1:
                        type = StorageBlobType.PageBlob;
                        break;
                    case 2:
                        type = StorageBlobType.BlockBlob;
                        break;
                    case 3:
                        type = StorageBlobType.AppendBlob;
                        break;
                    default:
                        break;
                }
            }
            
            if (type == StorageBlobType.PageBlob)
            {
                return CreatePageBlob(container, blobName, createBigBlob);
            }
            else if (type == StorageBlobType.BlockBlob)
            {
                return CreateBlockBlob(container, blobName, createBigBlob);
            }
            else if (type == StorageBlobType.AppendBlob)
            {
                return CreateAppendBlob(container, blobName, createBigBlob);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Invalid blob type: {0}", type));
            }
        }

        public CloudBlob GetRandomBlobReference(CloudBlobContainer container, string blobName)
        {
            int randomValue = random.Next(1, 4);
            switch (randomValue)
            {
                case 1:
                    return container.GetPageBlobReference(blobName);
                case 2:
                    return container.GetBlockBlobReference(blobName);
                case 3:
                    return container.GetAppendBlobReference(blobName);
                default:
                    throw new InvalidOperationException(string.Format("Invalid blob type: {0}", randomValue));
            }
        }

        /// <summary>
        /// convert blob name into valid file name
        /// </summary>
        /// <param name="blobName">blob name</param>
        /// <returns>valid file name</returns>
        public string ConvertBlobNameToFileName(string blobName, string dir, DateTimeOffset? snapshotTime = null)
        {
            string fileName = blobName;

            //replace dirctionary
            Dictionary<string, string> replaceRules = new Dictionary<string, string>()
	            {
	                {"/", "\\"}
	            };

            foreach (KeyValuePair<string, string> rule in replaceRules)
            {
                fileName = fileName.Replace(rule.Key, rule.Value);
            }

            if (snapshotTime != null)
            {
                int index = fileName.LastIndexOf('.');

                string prefix = string.Empty;
                string postfix = string.Empty;
                string timeStamp = string.Format("{0:u}", snapshotTime.Value);
                timeStamp = timeStamp.Replace(":", string.Empty).TrimEnd(new char[] { 'Z' });

                if (index == -1)
                {
                    prefix = fileName;
                    postfix = string.Empty;
                }
                else
                {
                    prefix = fileName.Substring(0, index);
                    postfix = fileName.Substring(index);
                }

                fileName = string.Format("{0} ({1}){2}", prefix, timeStamp, postfix);
            }

            return Path.Combine(dir, fileName);
        }

        public string ConvertFileNameToBlobName(string fileName)
        {
            return fileName.Replace('\\', '/');
        }

        /// <summary>
        /// list all the existing containers
        /// </summary>
        /// <returns>a list of cloudblobcontainer object</returns>
        public List<CloudBlobContainer> GetExistingContainers()
        {
            ContainerListingDetails details = ContainerListingDetails.All;
            return client.ListContainers(string.Empty, details).ToList();
        }

        /// <summary>
        /// get the number of existing container
        /// </summary>
        /// <returns></returns>
        public int GetExistingContainerCount()
        {
            return GetExistingContainers().Count;
        }

        /// <summary>
        /// Create a snapshot for the specified CloudBlob object
        /// </summary>
        /// <param name="blob">CloudBlob object</param>
        public CloudBlob SnapShot(CloudBlob blob)
        {
            CloudBlob snapshot = null;

            switch (blob.BlobType)
            {
                case StorageBlob.BlobType.BlockBlob:
                    snapshot = ((CloudBlockBlob)blob).CreateSnapshot();
                    break;
                case StorageBlob.BlobType.PageBlob:
                    snapshot = ((CloudPageBlob)blob).CreateSnapshot();
                    break;
                case StorageBlob.BlobType.AppendBlob:
                    snapshot = ((CloudAppendBlob)blob).CreateSnapshot();
                    break;
                default:
                    throw new ArgumentException(string.Format("Unsupport blob type {0} when create snapshot", blob.BlobType));
            }

            Test.Info(string.Format("Create snapshot for '{0}' at {1}", blob.Name, snapshot.SnapshotTime));

            return snapshot;
        }

        public static void PackContainerCompareData(CloudBlobContainer container, Dictionary<string, object> dic)
        {
            BlobContainerPermissions permissions = container.GetPermissions();
            dic["PublicAccess"] = permissions.PublicAccess;
            dic["Permission"] = permissions;
            dic["LastModified"] = container.Properties.LastModified;
        }

        public static void PackBlobCompareData(CloudBlob blob, Dictionary<string, object> dic)
        {
            dic["Length"] = blob.Properties.Length;
            dic["ContentType"] = blob.Properties.ContentType;
            dic["LastModified"] = blob.Properties.LastModified;
            dic["SnapshotTime"] = blob.SnapshotTime;
        }

        public static string ConvertCopySourceUri(string uri)
        {
            if (HttpsCopyHosts == null)
            {
                HttpsCopyHosts = new List<string>();
                string httpsHosts = Test.Data.Get("HttpsCopyHosts");
                string[] hosts = httpsHosts.Split();

                foreach (string host in hosts)
                {
                    if (!String.IsNullOrWhiteSpace(host))
                    {
                        HttpsCopyHosts.Add(host);
                    }
                }
            }

            //Azure always use https to copy from these hosts such windows.net
            bool useHttpsCopy = HttpsCopyHosts.Any(host => uri.IndexOf(host) != -1);

            if (useHttpsCopy)
            {
                return uri.Replace("http://", "https://");
            }
            else
            {
                return uri;
            }
        }

        public static bool WaitForCopyOperationComplete(CloudBlob destBlob, int maxRetry = 100)
        {
            int retryCount = 0;
            int sleepInterval = 1000; //ms

            if (destBlob == null)
            {
                return false;
            }

            do
            {
                if (retryCount > 0)
                {
                    Test.Info(String.Format("{0}th check current copy state and it's {1}. Wait for copy completion", retryCount, destBlob.CopyState.Status));
                }

                Thread.Sleep(sleepInterval);
                destBlob.FetchAttributes();
                retryCount++;
            }
            while (destBlob.CopyState.Status == CopyStatus.Pending && retryCount < maxRetry);

            Test.Info(String.Format("Final Copy status is {0}", destBlob.CopyState.Status));
            return destBlob.CopyState.Status != CopyStatus.Pending;
        }

        public static CloudBlob GetBlob(CloudBlobContainer container, string blobName, StorageBlobType blobType)
        {
            switch (blobType)
            {
                case StorageBlobType.BlockBlob:
                    return container.GetBlockBlobReference(blobName);
                case StorageBlobType.PageBlob:
                    return container.GetPageBlobReference(blobName);
                case StorageBlobType.AppendBlob:
                    return container.GetAppendBlobReference(blobName);
                default:
                    throw new InvalidOperationException(string.Format("Invalid blob type: {0}", blobType));
            }
        }

        /// <summary>
        /// Create a page blob with many small ranges (filling the page blob randomly by 3MB each time)
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="blobSize"></param>
        public void CreatePageBlobWithManySmallRanges(string containerName, string blobName, int blobSize)
        {
            const int Min_PageRageSizeinByte = 1024 * 1024 * 3;

            CloudBlobContainer container = client.GetContainerReference(containerName);
            CloudPageBlob pageblob = container.GetPageBlobReference(blobName);

            pageblob.Create(blobSize);

            //write small page ranges
            Random rnd = new Random();
            byte[] data = new byte[Min_PageRageSizeinByte];
            rnd.NextBytes(data);

            int pageno = 0;
            for (long i = 0; i < blobSize - Min_PageRageSizeinByte; i = i + Min_PageRageSizeinByte)
            {
                if (Utility.GetRandomBool())
                {
                    pageblob.WritePages(new MemoryStream(data), i);
                }
                pageno++;
            }

            Test.Info("Page No: {0} totally", pageno);
        }

        /// <summary>
        /// Expect sas token has the list permission for the specified container.
        /// </summary>
        public void ValidateContainerListableWithSasToken(CloudBlobContainer container, string sastoken)
        {
            Test.Info("Verify container list permission");
            int blobCount = container.ListBlobs().ToList().Count;
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(container.ServiceClient.Credentials.AccountName, sastoken);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(container.Name);
            int retrievedBlobCount = sasContainer.ListBlobs().ToList().Count;
            TestBase.ExpectEqual(blobCount, retrievedBlobCount, "blob count");
        }

        /// <summary>
        /// Expect sas token has the read permission for the specified container.
        /// </summary>
        internal void ValidateContainerReadableWithSasToken(CloudBlobContainer container, string sastoken)
        {
            Test.Info("Verify container read permission");
            List<CloudBlob> randomBlobs = CreateRandomBlob(container);
            CloudBlob blob = randomBlobs[0];
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(container.ServiceClient.Credentials.AccountName, sastoken);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(container.Name);
            CloudBlob retrievedBlob = StorageExtensions.GetBlobReferenceFromServer(sasContainer, blob.Name);
            long buffSize = retrievedBlob.Properties.Length;
            byte[] buffer = new byte[buffSize];
            MemoryStream ms = new MemoryStream(buffer);
            retrievedBlob.DownloadRangeToStream(ms, 0, buffSize);
            string md5 = Utility.GetBufferMD5(buffer);
            TestBase.ExpectEqual(blob.Properties.ContentMD5, md5, "content md5");
        }

        /// <summary>
        /// Expect sas token has the write permission for the specified container.
        /// </summary>
        internal void ValidateContainerWriteableWithSasToken(CloudBlobContainer container, string sastoken)
        {
            Test.Info("Verify container write permission");
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(container.ServiceClient.Credentials.AccountName, sastoken);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(container.Name);
            string blobName = Utility.GenNameString("saspageblob");
            CloudPageBlob pageblob = sasContainer.GetPageBlobReference(blobName);
            long blobSize = 1024 * 1024;
            pageblob.Create(blobSize);

            long buffSize = 1024 * 1024;
            byte[] buffer = new byte[buffSize];
            random.NextBytes(buffer);
            MemoryStream ms = new MemoryStream(buffer);
            pageblob.UploadFromStream(ms);

            CloudBlob retrievedBlob = StorageExtensions.GetBlobReferenceFromServer(container, blobName);
            Test.Assert(retrievedBlob != null, "Page blob should exist on server");
            TestBase.ExpectEqual(StorageBlobType.PageBlob.ToString(), retrievedBlob.BlobType.ToString(), "blob type");
            TestBase.ExpectEqual(blobSize, retrievedBlob.Properties.Length, "blob size");
        }

        /// <summary>
        /// Expect sas token has the Create permission for the specified container.
        /// </summary>
        internal void ValidateContainerCreateableWithSasToken(CloudBlobContainer container, string sastoken)
        {
            Test.Info("Verify container Create permission");
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(container.ServiceClient.Credentials.AccountName, sastoken);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(container.Name);
            if (!container.Exists())
            {
                sasContainer.Create();
                Test.Assert(sasContainer.Exists(), "The container should  exist after Creating with sas token");
            }
            string blobName = Utility.GenNameString("saspageblob");
            CloudPageBlob pageblob = sasContainer.GetPageBlobReference(blobName);
            long blobSize = 1024 * 1024;
            pageblob.Create(blobSize);
            CloudBlob retrievedBlob = StorageExtensions.GetBlobReferenceFromServer(container, blobName);
            Test.Assert(retrievedBlob != null, "Page blob should exist on server");
            TestBase.ExpectEqual(StorageBlobType.PageBlob.ToString(), retrievedBlob.BlobType.ToString(), "blob type");
            TestBase.ExpectEqual(blobSize, retrievedBlob.Properties.Length, "blob size");
        }

        /// <summary>
        /// Expect sas token has the Append permission for the specified container.
        /// </summary>
        internal void ValidateContainerAppendableWithSasToken(CloudBlobContainer container, string sastoken)
        {
            Test.Info("Verify container Append permission");
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(container.ServiceClient.Credentials.AccountName, sastoken);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(container.Name);
            string blobName = Utility.GenNameString("sasAppendblob");
            CloudAppendBlob appendblob = sasContainer.GetAppendBlobReference(blobName);
            container.GetAppendBlobReference(blobName).CreateOrReplace();

            long buffSize = 1024 * 1024;
            byte[] buffer = new byte[buffSize];
            random.NextBytes(buffer);
            MemoryStream ms = new MemoryStream(buffer);
            appendblob.AppendBlock(ms);

            CloudBlob retrievedBlob = StorageExtensions.GetBlobReferenceFromServer(container, blobName);
            Test.Assert(retrievedBlob != null, "Append blob should exist on server");
            TestBase.ExpectEqual(StorageBlobType.AppendBlob.ToString(), retrievedBlob.BlobType.ToString(), "blob type");
            TestBase.ExpectEqual(buffSize, retrievedBlob.Properties.Length, "blob size");
        }

        /// <summary>
        /// Expect sas token has the delete permission for the specified container.
        /// </summary>
        internal void ValidateContainerDeleteableWithSasToken(CloudBlobContainer container, string sastoken)
        {
            Test.Info("Verify container delete permission");
            List<CloudBlob> randomBlobs = CreateRandomBlob(container);
            CloudBlob blob = randomBlobs[0];
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(container.ServiceClient.Credentials.AccountName, sastoken);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(container.Name);

            CloudBlob sasBlob = null;
            if (blob.BlobType == StorageBlobType.BlockBlob)
            {
                sasBlob = sasContainer.GetBlockBlobReference(blob.Name);
            }
            else
            {
                sasBlob = sasContainer.GetPageBlobReference(blob.Name);
            }

            sasBlob.Delete();
            Test.Assert(!blob.Exists(), "blob should not exist");
        }

        /// <summary>
        /// Validate the read permission in the sas token for the the specified blob
        /// </summary>
        internal void ValidateBlobReadableWithSasToken(CloudBlob cloudBlob, string sasToken, bool useHttps = true)
        {
            Test.Info("Verify blob read permission");
            CloudBlob sasBlob = GetCloudBlobBySasToken(cloudBlob, sasToken, useHttps);
            long buffSize = cloudBlob.Properties.Length;
            byte[] buffer = new byte[buffSize];
            MemoryStream ms = new MemoryStream(buffer);
            sasBlob.DownloadRangeToStream(ms, 0, buffSize);
            string md5 = Utility.GetBufferMD5(buffer);
            TestBase.ExpectEqual(cloudBlob.Properties.ContentMD5, md5, "content md5");
        }

        /// <summary>
        /// Validate the write permission in the sas token for the the specified blob
        /// </summary>
        internal void ValidateBlobWriteableWithSasToken(CloudBlob cloudBlob, string sasToken, bool useHttps = true)
        {
            Test.Info("Verify blob write permission");
            CloudBlob sasBlob = GetCloudBlobBySasToken(cloudBlob, sasToken, useHttps);
            DateTimeOffset? lastModifiedTime = cloudBlob.Properties.LastModified;
            Thread.Sleep(1000); // to make sure the LMT of the blob will change
            long buffSize = 1024 * 1024;
            byte[] buffer = new byte[buffSize];
            random.NextBytes(buffer);
            MemoryStream ms = new MemoryStream(buffer);
            sasBlob.UploadFromStream(ms);
            sasBlob.UploadFromStream(ms);
            cloudBlob.FetchAttributes();
            DateTimeOffset? newModifiedTime = cloudBlob.Properties.LastModified;
            //We don't have the permission to set the content-md5
            TestBase.ExpectNotEqual(lastModifiedTime.ToString(), newModifiedTime.ToString(), "Last modified time");
        }

        /// <summary>
        /// Validate the delete permission in the sas token for the the specified blob
        /// </summary>
        internal void ValidateBlobDeleteableWithSasToken(CloudBlob cloudBlob, string sasToken)
        {
            Test.Info("Verify blob delete permission");
            if (!cloudBlob.Exists())
            {
                cloudBlob = CreateRandomBlob(cloudBlob.Container, cloudBlob.Name, cloudBlob.Properties.BlobType);
            }
            CloudBlob sasBlob = GetCloudBlobBySasToken(cloudBlob, sasToken);
            sasBlob.Delete();
            Test.Assert(!cloudBlob.Exists(), "The blob should not exist after deleting with sas token");
        }

        /// <summary>
        /// Validate the Create permission in the sas token for the the specified blob
        /// </summary>
        internal void ValidateBlobCreateableWithSasToken(CloudBlob cloudBlob, string sasToken, bool useHttps = true)
        {
            Test.Info("Verify blob Create permission");
            cloudBlob.DeleteIfExists();
            CloudPageBlob pageblob;
            if (cloudBlob.BlobType != StorageBlobType.PageBlob)
            {
                pageblob = cloudBlob.Parent.GetPageBlobReference(cloudBlob.Name);
            }
            else
            {
                pageblob = (CloudPageBlob)cloudBlob;
            }
            try
            {
                CloudBlob sasBlob = GetCloudBlobBySasToken((CloudBlob)pageblob, sasToken, useHttps);

                ((CloudPageBlob)sasBlob).Create(512);
                Test.Assert(pageblob.Exists(), "The blob should  exist after creating with sas token");
            }
            catch
            {
                throw;
            }
            finally
            {
                pageblob.DeleteIfExists();
                cloudBlob = CreateRandomBlob(cloudBlob.Container, cloudBlob.Name, cloudBlob.Properties.BlobType);
            }
        }

        /// <summary>
        /// Validate the Append permission in the sas token for the the specified blob
        /// </summary>
        internal void ValidateBlobAppendableWithSasToken(CloudBlob cloudBlob, string sasToken, bool useHttps = true)
        {
            Test.Info("Verify blob Append permission");

            long buffSize = 1024 * 1024;
            byte[] buffer = new byte[buffSize];
            random.NextBytes(buffer);
            MemoryStream ms = new MemoryStream(buffer);
            CloudAppendBlob appendblob;
            if (cloudBlob.BlobType != StorageBlobType.AppendBlob)
            {
                cloudBlob.DeleteIfExists();
                appendblob = cloudBlob.Parent.GetAppendBlobReference(cloudBlob.Name);
            }
            else
            {
                appendblob = (CloudAppendBlob)cloudBlob;
            }
            try
            {
                appendblob.CreateOrReplace();
                CloudBlob sasBlob = GetCloudBlobBySasToken(appendblob, sasToken, useHttps);
                DateTimeOffset? lastModifiedTime = cloudBlob.Properties.LastModified;
                Thread.Sleep(1000); // to make sure the LMT of the blob will change
                ((CloudAppendBlob)sasBlob).AppendBlock(ms);
                appendblob.FetchAttributes();
                DateTimeOffset? newModifiedTime = appendblob.Properties.LastModified;
                //We don't have the permission to set the content-md5
                TestBase.ExpectNotEqual(lastModifiedTime.ToString(), newModifiedTime.ToString(), "Last modified time");
            }
            catch
            {
                throw;
            }
            finally
            {
                appendblob.DeleteIfExists();
                cloudBlob = CreateRandomBlob(cloudBlob.Container, cloudBlob.Name, cloudBlob.Properties.BlobType);
            }
        }

        /// <summary>
        /// Get CloudBlob by sas token
        /// </summary>
        internal CloudBlob GetCloudBlobBySasToken(CloudBlob blob, string sasToken, bool useHttps = true)
        {
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(blob.ServiceClient.Credentials.AccountName, sasToken, useHttps);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(blob.Container.Name);
            CloudBlob sasBlob = null;

            if (blob.BlobType == StorageBlobType.BlockBlob)
            {
                sasBlob = sasContainer.GetBlockBlobReference(blob.Name);
            }
            else if (blob.BlobType == StorageBlobType.PageBlob)
            {
                sasBlob = sasContainer.GetPageBlobReference(blob.Name);
            }
            else if (blob.BlobType == StorageBlobType.AppendBlob)
            {
                sasBlob = sasContainer.GetAppendBlobReference(blob.Name);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Invalid blob type: {0}", blob.BlobType));
            }
            
            return sasBlob;
        }
    }
}
