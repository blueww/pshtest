
namespace StorageTestLib
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.File;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Queue.Protocol;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table;
    using StorageBlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;

    public static class StorageExtensions
    {
#if DOTNET5_4

        public static ServiceProperties GetServiceProperties(this CloudBlobClient blobClient)
        {
            return blobClient.GetServicePropertiesAsync().GetAwaiter().GetResult();
        }

        public static void SetServiceProperties(
            this CloudBlobClient blobClient,
            ServiceProperties properties)
        {
            blobClient.SetServicePropertiesAsync(properties).GetAwaiter().GetResult();
        }

        #region blob
        public static void SetProperties(this CloudBlob blob, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            blob.SetPropertiesAsync(accessCondition, options, operationContext).GetAwaiter().GetResult();
        }

        public static void FetchAttributes(this CloudBlob blob)
        {
            blob.FetchAttributesAsync().GetAwaiter().GetResult();
        }

        public static void Delete(this CloudBlob blob, DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            blob.DeleteAsync(deleteSnapshotsOption, accessCondition, options, operationContext).GetAwaiter().GetResult();
        }

        public static bool Exists(this CloudBlob blob)
        {
            return blob.ExistsAsync().GetAwaiter().GetResult();
        }

        public static bool DeleteIfExists(this CloudBlob blob)
        {
            return blob.DeleteIfExistsAsync().GetAwaiter().GetResult();
        }

        public static void DownloadToStream(this CloudBlob blob, Stream target, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            blob.DownloadToStreamAsync(target, accessCondition, options, operationContext).GetAwaiter().GetResult();
        }

        public static CloudPageBlob CreateSnapshot(this CloudPageBlob blob)
        {
            return blob.CreateSnapshotAsync().GetAwaiter().GetResult();
        }

        public static CloudAppendBlob CreateSnapshot(this CloudAppendBlob blob)
        {
            return blob.CreateSnapshotAsync().GetAwaiter().GetResult();
        }

        public static CloudBlockBlob CreateSnapshot(this CloudBlockBlob blob)
        {
            return blob.CreateSnapshotAsync().GetAwaiter().GetResult();
        }

        public static void UploadFromStream(this CloudBlockBlob blob, 
            Stream source,
            AccessCondition accessCondition = null, 
            BlobRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            blob.UploadFromStreamAsync(source, accessCondition, options, operationContext).GetAwaiter().GetResult();
        }

        public static void UploadFromStream(this CloudAppendBlob blob,
            Stream source,
            AccessCondition accessCondition = null,
            BlobRequestOptions options = null,
            OperationContext operationContext = null)
        {
            blob.UploadFromStreamAsync(source, accessCondition, options, operationContext).GetAwaiter().GetResult();
        }

        public static void PutBlockList(this CloudBlockBlob blob, string[] blockIds)
        {
            blob.PutBlockListAsync(blockIds).GetAwaiter().GetResult();
        }

        public static void Create(this CloudPageBlob blob, long size)
        {
            blob.CreateAsync(size).GetAwaiter().GetResult();
        }

        public static void WritePages(this CloudPageBlob blob, Stream pageData, long startOffset, string contentMD5 = null, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            blob.WritePagesAsync(pageData, startOffset, contentMD5, accessCondition, options, operationContext).GetAwaiter().GetResult();
        }

        #endregion

        #region container
        public static bool Exists(this CloudBlobContainer container)
        {
            return container.ExistsAsync().GetAwaiter().GetResult();
        }

        public static void Create(this CloudBlobContainer container, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            container.CreateAsync(options, operationContext).GetAwaiter().GetResult();
        }

        public static bool CreateIfNotExists(this CloudBlobContainer container,
            BlobContainerPublicAccessType accessType = BlobContainerPublicAccessType.Off, 
            BlobRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            return container.CreateIfNotExistsAsync(accessType, options, operationContext).GetAwaiter().GetResult();
        }

        public static void Delete(this CloudBlobContainer container, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            container.DeleteAsync(accessCondition, options, operationContext).GetAwaiter().GetResult();
        }

        public static bool DeleteIfExists(this CloudBlobContainer container,
            AccessCondition accessCondition = null, 
            BlobRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            return container.DeleteIfExistsAsync(accessCondition, options, operationContext).GetAwaiter().GetResult();
        }

        public static BlobContainerPermissions GetPermissions(this CloudBlobContainer container)
        {
            return container.GetPermissionsAsync().GetAwaiter().GetResult();
        }

        public static void SetPermissions(this CloudBlobContainer container, BlobContainerPermissions permissions)
        {
            container.SetPermissionsAsync(permissions).GetAwaiter().GetResult();
        }

        public static IEnumerable<IListBlobItem> ListBlobs(this CloudBlobDirectory blobDirectory,
            bool useFlatBlobListing = false, 
            BlobListingDetails blobListingDetails = BlobListingDetails.None, 
            BlobRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            BlobContinuationToken currentToken = null;
            int? maxResults = 250;
            do
            {
                var blobListReult = blobDirectory.ListBlobsSegmentedAsync(
                    useFlatBlobListing,
                    blobListingDetails,
                    maxResults,
                    currentToken,
                    options,
                    operationContext).GetAwaiter().GetResult();

                foreach (var blobItem in blobListReult.Results)
                {
                    yield return blobItem;
                }
                currentToken = blobListReult.ContinuationToken;
            }
            while (null != currentToken);
        }

        public static IEnumerable<IListBlobItem> ListBlobs(this CloudBlobContainer container,
            string prefix = null, 
            bool useFlatBlobListing = false,
            BlobListingDetails blobListingDetails = BlobListingDetails.None, 
            BlobRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            BlobContinuationToken currentToken = null;
            int? maxResults = 250;
            do
            {
                var blobListReult = container.ListBlobsSegmentedAsync(
                    prefix,
                    useFlatBlobListing,
                    blobListingDetails,
                    maxResults,
                    currentToken,
                    options,
                    operationContext).GetAwaiter().GetResult();

                foreach (var blobItem in blobListReult.Results)
                {
                    yield return blobItem;
                }
                currentToken = blobListReult.ContinuationToken;
            }
            while (null != currentToken);
        }
        #endregion


        public static FileServiceProperties GetServiceProperties(this CloudFileClient fileClient)
        {
            return fileClient.GetServicePropertiesAsync().GetAwaiter().GetResult();
        }

        public static IEnumerable<CloudFileShare> ListShares(this CloudFileClient fileClient,
            string prefix = null,
            ShareListingDetails detailsIncluded = ShareListingDetails.None,
            FileRequestOptions options = null,
            OperationContext operationContext = null)
        {
            FileContinuationToken currentToken = null;
            int? maxResults = null;

            do
            {
                var listResults = fileClient.ListSharesSegmentedAsync(prefix,
                    detailsIncluded,
                    maxResults,
                    currentToken,
                    options,
                    operationContext).GetAwaiter().GetResult();

                foreach (var shareItem in listResults.Results)
                {
                    yield return shareItem;
                }

                currentToken = listResults.ContinuationToken;
            } while (null != currentToken);
        }

        #region file
        public static void Delete(this CloudFile cloudFile)
        {
            cloudFile.DeleteAsync().GetAwaiter().GetResult();
        }

        public static bool DeleteIfExists(this CloudFile cloudFile)
        {
            return cloudFile.DeleteIfExistsAsync().GetAwaiter().GetResult();
        }

        public static bool Exists(this CloudFile cloudFile)
        {
            return cloudFile.ExistsAsync().GetAwaiter().GetResult();
        }

        public static void Create(this CloudFile file, long size)
        {
            file.CreateAsync(size).GetAwaiter().GetResult();
        }
        #endregion

        #region sharedirectory

        public static bool CreateIfNotExists(this CloudFileDirectory fileDirectory)
        {
            return fileDirectory.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }
        
        public static void Delete(this CloudFileDirectory fileDirectory)
        {
            fileDirectory.DeleteAsync().GetAwaiter().GetResult();
        }

        public static bool Exists(this CloudFileDirectory fileDirectory)
        {
            return fileDirectory.ExistsAsync().GetAwaiter().GetResult();
        }

        public static FileSharePermissions GetPermissions(this CloudFileShare fileShare)
        {
            return fileShare.GetPermissionsAsync().GetAwaiter().GetResult();
        }

        public static void SetPermissions(this CloudFileShare fileShare, FileSharePermissions permissions)
        {
            fileShare.SetPermissionsAsync(permissions).GetAwaiter().GetResult();
        }

        public static IEnumerable<IListFileItem> ListFilesAndDirectories(this CloudFileDirectory fileDirectory)
        {
            FileContinuationToken currentToken = null;
            do
            {
                var fileListResult = fileDirectory.ListFilesAndDirectoriesSegmentedAsync(currentToken).GetAwaiter().GetResult();

                foreach (var fileItem in fileListResult.Results)
                {
                    yield return fileItem;
                }

                currentToken = fileListResult.ContinuationToken;
            }
            while (null != currentToken);
        }

        public static bool CreateIfNotExists(this CloudFileShare share)
        {
            return share.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }

        public static bool DeleteIfExists(this CloudFileShare share,
            DeleteShareSnapshotsOption deleteSnapshotsOption = DeleteShareSnapshotsOption.None, 
            AccessCondition accessCondition = null,
            FileRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            return share.DeleteIfExistsAsync().GetAwaiter().GetResult();
        }

        public static bool Exists(this CloudFileShare share)
        {
            return share.ExistsAsync().GetAwaiter().GetResult();
        }
        #endregion


        public static QueuePermissions GetPermissions(this CloudQueue queue)
        {
            return queue.GetPermissionsAsync().GetAwaiter().GetResult();
        }

        public static void SetPermissions(this CloudQueue queue, QueuePermissions permissions)
        {
            queue.SetPermissionsAsync(permissions).GetAwaiter().GetResult();
        }

        public static ServiceProperties GetServiceProperties(this CloudQueueClient queueClient)
        {
            return queueClient.GetServicePropertiesAsync().GetAwaiter().GetResult();
        }


        public static ServiceProperties GetServiceProperties(this CloudTableClient tableClient)
        {
            return tableClient.GetServicePropertiesAsync().GetAwaiter().GetResult();
        }

        public static bool CreateIfNotExists(this CloudTable table)
        {
            return table.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }

        public static bool DeleteIfExists(this CloudTable table)
        {
            return table.DeleteIfExistsAsync().GetAwaiter().GetResult();
        }

        public static bool Exists(this CloudTable table)
        {
            return table.ExistsAsync().GetAwaiter().GetResult();
        }
        
        public static TablePermissions GetPermissions(this CloudTable table)
        {
            return table.GetPermissionsAsync().GetAwaiter().GetResult();
        }

        public static void SetPermissions(this CloudTable table, TablePermissions permissions)
        {
            table.SetPermissionsAsync(permissions).GetAwaiter().GetResult();
        }

        public static IEnumerable<CloudTable> ListTables(this CloudTableClient tableClient,
            string prefix = null, 
            TableRequestOptions requestOptions = null, 
            OperationContext operationContext = null)
        {
            int? maxResults = 250;
            TableContinuationToken currentToken = null;
            do
            {
                var tableListResults = tableClient.ListTablesSegmentedAsync(prefix, maxResults, currentToken, requestOptions, operationContext).GetAwaiter().GetResult();
                foreach (var tableItem in tableListResults.Results)
                {
                    yield return tableItem;
                }

                currentToken = tableListResults.ContinuationToken;
            }
            while (null != currentToken);
        }

#endif

        public static void UploadFromStream(this CloudBlob blob, Stream sourceStream)
        {
            switch (blob.BlobType)
            {
                case StorageBlobType.BlockBlob:
                    (blob as CloudBlockBlob).UploadFromStream(sourceStream);
                    return;
                case StorageBlobType.PageBlob:
                    (blob as CloudPageBlob).UploadFromStream(sourceStream);
                    return;
                case StorageBlobType.AppendBlob:
                    (blob as CloudAppendBlob).UploadFromStream(sourceStream);
                    return;
                default:
                    throw new InvalidOperationException(string.Format("Does not support blob type {0}", blob.BlobType));
            }
        }

        public static CloudBlob GetBlobReferenceFromServer(CloudBlobContainer container, string blobName)
        {
            CloudBlob blob = container.GetBlobReference(blobName);
            blob.FetchAttributes();

            blob = GetBlobReference(blob.Uri, container.ServiceClient.Credentials, blob.BlobType);
            blob.FetchAttributes();

            return blob;
        }

        private static CloudBlob GetBlobReference(Uri blobUri, StorageCredentials credentials, StorageBlobType blobType)
        {
            switch (blobType)
            { 
                case StorageBlobType.BlockBlob:
                    return new CloudBlockBlob(blobUri, credentials);
                case StorageBlobType.PageBlob:
                    return new CloudPageBlob(blobUri, credentials);
                case StorageBlobType.AppendBlob:
                    return new CloudAppendBlob(blobUri, credentials);
                default:
                    throw new InvalidOperationException(string.Format("Does not support blob type of: {0}", blobType));
            }
        }
    }
}
