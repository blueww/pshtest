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

        public ICloudBlob Blob
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
            container.CreateIfNotExists();

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
        /// <returns>ICloudBlob object</returns>
        public ICloudBlob CreatePageBlob(CloudBlobContainer container, string blobName)
        {
            CloudPageBlob pageBlob = container.GetPageBlobReference(blobName);
            int size = random.Next(1, 10) * PageBlobUnitSize;
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
        /// <returns>ICloudBlob object</returns>
        public ICloudBlob CreateBlockBlob(CloudBlobContainer container, string blobName)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            int maxBlobSize = 1024 * 1024;
            string md5sum = string.Empty;
            int blobSize = random.Next(maxBlobSize);
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
        /// generate random blob properties and metadata
        /// </summary>
        /// <param name="blob">ICloudBlob object</param>
        private void GenerateBlobPropertiesAndMetaData(ICloudBlob blob)
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
        /// <returns>ICloudBlob object</returns>
        public ICloudBlob CreateBlob(CloudBlobContainer container, string blobName, StorageBlob.BlobType type)
        {
            if (type == StorageBlob.BlobType.BlockBlob)
            {
                return CreateBlockBlob(container, blobName);
            }
            else
            {
                return CreatePageBlob(container, blobName);
            }
        }

        /// <summary>
        /// create a list of blobs with random properties/metadata/blob type
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="blobName">a list of blob names</param>
        /// <returns>a list of cloud page blobs</returns>
        public List<ICloudBlob> CreateRandomBlob(CloudBlobContainer container, List<string> blobNames)
        {
            List<ICloudBlob> blobs = new List<ICloudBlob>();

            foreach (string blobName in blobNames)
            {
                blobs.Add(CreateRandomBlob(container, blobName));
            }

            blobs = blobs.OrderBy(blob => blob.Name).ToList();

            return blobs;
        }

        public List<ICloudBlob> CreateRandomBlob(CloudBlobContainer container)
        {
            int count = random.Next(1, 5);
            List<string> blobNames = new List<string>();
            for (int i = 0; i < count; i++)
            {
                blobNames.Add(Utility.GenNameString("blob"));
            }

            return CreateRandomBlob(container, blobNames);
        }

        /// <summary>
        /// Create a list of blobs with random properties/metadata/blob type
        /// </summary>
        /// <param name="container">CloudBlobContainer object</param>
        /// <param name="blobName">Blob name</param>
        /// <param name="BlobType">type</param>
        /// <returns>ICloudBlob object</returns>
        public ICloudBlob CreateRandomBlob(CloudBlobContainer container, string blobName, StorageBlobType type = StorageBlobType.Unspecified)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                blobName = Utility.GenNameString(TestBase.SpecialChars);
            }

            if (type == StorageBlobType.Unspecified)
            {
                type = (random.Next(0, 2) == 0) ? StorageBlobType.PageBlob : StorageBlobType.BlockBlob;
            }
            
            if (type == StorageBlobType.PageBlob)
            {
                return CreatePageBlob(container, blobName);
            }
            else
            {
                return CreateBlockBlob(container, blobName);
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
        /// Create a snapshot for the specified ICloudBlob object
        /// </summary>
        /// <param name="blob">ICloudBlob object</param>
        public ICloudBlob SnapShot(ICloudBlob blob)
        {
            ICloudBlob snapshot = default(ICloudBlob);

            switch (blob.BlobType)
            {
                case StorageBlob.BlobType.BlockBlob:
                    snapshot = ((CloudBlockBlob)blob).CreateSnapshot();
                    break;
                case StorageBlob.BlobType.PageBlob:
                    snapshot = ((CloudPageBlob)blob).CreateSnapshot();
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

        public static void PackBlobCompareData(ICloudBlob blob, Dictionary<string, object> dic)
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

        public static bool WaitForCopyOperationComplete(ICloudBlob destBlob, int maxRetry = 100)
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

        public static ICloudBlob GetBlob(CloudBlobContainer container, string blobName, StorageBlobType blobType)
        {
            ICloudBlob blob = null;
            if (blobType == StorageBlobType.BlockBlob)
            {
                blob = container.GetBlockBlobReference(blobName);
            }
            else
            {
                blob = container.GetPageBlobReference(blobName);
            }
            return blob;
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
            List<ICloudBlob> randomBlobs = CreateRandomBlob(container);
            ICloudBlob blob = randomBlobs[0];
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(container.ServiceClient.Credentials.AccountName, sastoken);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(container.Name);
            ICloudBlob retrievedBlob = sasContainer.GetBlobReferenceFromServer(blob.Name);
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
            ICloudBlob retrievedBlob = container.GetBlobReferenceFromServer(blobName);
            Test.Assert(retrievedBlob != null, "Page blob should exist on server");
            TestBase.ExpectEqual(StorageBlobType.PageBlob.ToString(), retrievedBlob.BlobType.ToString(), "blob type");
            TestBase.ExpectEqual(blobSize, retrievedBlob.Properties.Length, "blob size");
        }

        /// <summary>
        /// Expect sas token has the delete permission for the specified container.
        /// </summary>
        internal void ValidateContainerDeleteableWithSasToken(CloudBlobContainer container, string sastoken)
        {
            Test.Info("Verify container delete permission");
            List<ICloudBlob> randomBlobs = CreateRandomBlob(container);
            ICloudBlob blob = randomBlobs[0];
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(container.ServiceClient.Credentials.AccountName, sastoken);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(container.Name);

            ICloudBlob sasBlob = default(ICloudBlob);
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
        internal void ValidateBlobReadableWithSasToken(ICloudBlob cloudBlob, string sasToken)
        {
            Test.Info("Verify blob read permission");
            ICloudBlob sasBlob = GetICloudBlobBySasToken(cloudBlob, sasToken);
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
        internal void ValidateBlobWriteableWithSasToken(ICloudBlob cloudBlob, string sasToken)
        {
            Test.Info("Verify blob write permission");
            ICloudBlob sasBlob = GetICloudBlobBySasToken(cloudBlob, sasToken);
            DateTimeOffset? lastModifiedTime = cloudBlob.Properties.LastModified;
            long buffSize = 1024 * 1024;
            byte[] buffer = new byte[buffSize];
            random.NextBytes(buffer);
            MemoryStream ms = new MemoryStream(buffer);
            sasBlob.UploadFromStream(ms);
            cloudBlob.FetchAttributes();
            DateTimeOffset? newModifiedTime = cloudBlob.Properties.LastModified;
            //We don't have the permission to set the content-md5
            TestBase.ExpectNotEqual(lastModifiedTime.ToString(), newModifiedTime.ToString(), "Last modified time");
        }

        /// <summary>
        /// Validate the delete permission in the sas token for the the specified blob
        /// </summary>
        internal void ValidateBlobDeleteableWithSasToken(ICloudBlob cloudBlob, string sasToken)
        {
            Test.Info("Verify blob delete permission");
            Test.Assert(cloudBlob.Exists(), "The blob should exist");
            ICloudBlob sasBlob = GetICloudBlobBySasToken(cloudBlob, sasToken);
            sasBlob.Delete();
            Test.Assert(!cloudBlob.Exists(), "The blob should not exist after deleting with sas token");
        }

        /// <summary>
        /// Get ICloudBlob by sas token
        /// </summary>
        internal ICloudBlob GetICloudBlobBySasToken(ICloudBlob blob, string sasToken)
        {
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(blob.ServiceClient.Credentials.AccountName, sasToken);
            CloudBlobClient sasBlobClient = sasAccount.CreateCloudBlobClient();
            CloudBlobContainer sasContainer = sasBlobClient.GetContainerReference(blob.Container.Name);
            ICloudBlob sasBlob = default(ICloudBlob);

            if (blob.BlobType == StorageBlobType.BlockBlob)
            {
                sasBlob = sasContainer.GetBlockBlobReference(blob.Name);
            }
            else
            {
                sasBlob = sasContainer.GetPageBlobReference(blob.Name);
            }
            
            return sasBlob;
        }
    }
}