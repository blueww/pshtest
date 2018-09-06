using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table;

namespace Management.Storage.ScenarioTest
{
    static class Extensions
    {
        public static AuthenticationResult AcquireToken(this AuthenticationContext context, string resource, ClientCredential clientCredential)
        {
            return context.AcquireTokenAsync(resource, clientCredential).GetAwaiter().GetResult();
        }

        #region blob

        public static IEnumerable<CloudBlobContainer> ListContainers(
            this CloudBlobClient blobClient,
            string prefix = null, 
            ContainerListingDetails detailsIncluded = ContainerListingDetails.None, 
            BlobRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            BlobContinuationToken currentToken = null;
            int? maxResults = 250;

            do
            {
                var listResults = blobClient.ListContainersSegmentedAsync(prefix,
                    detailsIncluded,
                    maxResults,
                    currentToken,
                    options,
                    operationContext).GetAwaiter().GetResult();

                foreach (var containerItem in listResults.Results)
                {
                    yield return containerItem;
                }

                currentToken = listResults.ContinuationToken;
            } while (null != currentToken);
        }

        public static void FetchAttributes(this CloudBlobContainer container)
        {
            container.FetchAttributesAsync().GetAwaiter().GetResult();
        }

        public static void SetMetadata(this CloudBlobContainer container)
        {
            container.SetMetadataAsync().GetAwaiter().GetResult();
        }

        public static string StartCopy(this CloudBlockBlob dest, CloudBlockBlob source)
        {
            return dest.StartCopyAsync(source).GetAwaiter().GetResult();
        }

        public static string StartCopy(this CloudAppendBlob dest, CloudAppendBlob source)
        {
            return dest.StartCopyAsync(source).GetAwaiter().GetResult();
        }

        public static string StartCopy(this CloudPageBlob dest, CloudPageBlob source)
        {
            return dest.StartCopyAsync(source).GetAwaiter().GetResult();
        }

        public static long AppendBlock(this CloudAppendBlob appendBlob,
            Stream blockData, 
            string contentMD5 = null, 
            AccessCondition accesscondition = null, 
            BlobRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            return appendBlob.AppendBlockAsync(blockData, contentMD5, accesscondition, options, operationContext).GetAwaiter().GetResult();
        }

        public static void CreateOrReplace(this CloudAppendBlob appendBlob)
        {
            appendBlob.CreateOrReplaceAsync().GetAwaiter().GetResult();
        }

        public static void DownloadRangeToStream(this CloudBlob blob,
            Stream target, 
            long? offset = null, 
            long? length = null)
        {
            blob.DownloadRangeToStreamAsync(target, offset, length).GetAwaiter().GetResult();
        }

        public static void SetMetadata(this CloudBlob blob)
        {
            blob.SetMetadataAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region file

        public static void Create(this CloudFileShare share)
        {
            share.CreateAsync().GetAwaiter().GetResult();
        }

        public static void Delete(this CloudFileShare share)
        {
            share.DeleteAsync().GetAwaiter().GetResult();
        }

        public static void Create(this CloudFileDirectory directory)
        {
            directory.CreateAsync().GetAwaiter().GetResult();
        }

        public static bool DeleteIfExists(this CloudFileDirectory directory)
        {
            return directory.DeleteIfExistsAsync().GetAwaiter().GetResult();
        }

        public static void FetchAttributes(this CloudFile file)
        {
            file.FetchAttributesAsync().GetAwaiter().GetResult();
        }

        public static void SetMetadata(this CloudFile file)
        {
            file.SetMetadataAsync().GetAwaiter().GetResult();
        }

        public static void SetProperties(this CloudFile file)
        {
            file.SetPropertiesAsync().GetAwaiter().GetResult();
        }

        public static void DownloadRangeToStream(this CloudFile file, 
            Stream target,
            long? offset = null,
            long? length = null)
        {
            file.DownloadRangeToStreamAsync(target, offset, length).GetAwaiter().GetResult();
        }

        public static void UploadFromByteArray(this CloudFile file,
            byte[] buffer, 
            int index, 
            int count, 
            AccessCondition accessCondition = null, 
            FileRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            file.UploadFromByteArrayAsync(
                buffer,
                index,
                count,
                accessCondition,
                options,
                operationContext).GetAwaiter().GetResult();
        }

        public static void UploadFromFile(this CloudFile file,
            string path,
            AccessCondition accessCondition = null,
            FileRequestOptions options = null,
            OperationContext operationContext = null)
        {
            file.UploadFromFileAsync(
                path,
                accessCondition,
                options,
                operationContext).GetAwaiter().GetResult();
        }

        public static void UploadFromStream(this CloudFile file,
            Stream source,
            AccessCondition accessCondition = null,
            FileRequestOptions options = null,
            OperationContext operationContext = null)
        {
            file.UploadFromStreamAsync(
                source,
                accessCondition,
                options,
                operationContext).GetAwaiter().GetResult();
        }

        #endregion

        #region queue

        public static void SetServiceProperties(this CloudQueueClient queueClient, ServiceProperties properties)
        {
            queueClient.SetServicePropertiesAsync(properties).GetAwaiter().GetResult();
        }

        public static IEnumerable<CloudQueue> ListQueues(
            this CloudQueueClient queueClient,
            string prefix = null, 
            QueueListingDetails detailsIncluded = QueueListingDetails.None, 
            QueueRequestOptions options = null, 
            OperationContext operationContext = null)
        {
            QueueContinuationToken currentToken = null;
            int? maxResults = 250;

            do
            {
                var listResults = queueClient.ListQueuesSegmentedAsync(
                    prefix, 
                    detailsIncluded, 
                    maxResults, 
                    currentToken, 
                    options, 
                    operationContext).GetAwaiter().GetResult();

                foreach (var queueItem in listResults.Results)
                {
                    yield return queueItem;
                }

                currentToken = listResults.ContinuationToken;
            } while (null != currentToken);
        }

        public static bool Exists(this CloudQueue queue)
        {
            return queue.ExistsAsync().GetAwaiter().GetResult();
        }

        public static bool CreateIfNotExists(this CloudQueue queue)
        {
            return queue.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }

        public static bool DeleteIfExists(this CloudQueue queue)
        {
            return queue.DeleteIfExistsAsync().GetAwaiter().GetResult();
        }

        public static void FetchAttributes(this CloudQueue queue)
        {
            queue.FetchAttributesAsync().GetAwaiter().GetResult();
        }

        public static void Delete(this CloudQueue queue)
        {
            queue.DeleteAsync().GetAwaiter().GetResult();
        }

        public static void SetMetadata(this CloudQueue queue)
        {
            queue.SetMetadataAsync().GetAwaiter().GetResult();
        }

        public static void AddMessage(this CloudQueue queue, CloudQueueMessage message)
        {
            queue.AddMessageAsync(message).GetAwaiter().GetResult();
        }

        public static void DeleteMessage(this CloudQueue queue, CloudQueueMessage message)
        {
            queue.DeleteMessageAsync(message).GetAwaiter().GetResult();
        }

        public static CloudQueueMessage GetMessage(this CloudQueue queue)
        {
            return queue.GetMessageAsync().GetAwaiter().GetResult();
        }

        public static void UpdateMessage(this CloudQueue queue,
            CloudQueueMessage message, 
            TimeSpan visibilityTimeout, 
            MessageUpdateFields updateFields)
        {
            queue.UpdateMessageAsync(message, visibilityTimeout, updateFields).GetAwaiter().GetResult();
        }

        #endregion

        #region table

        public static void SetServiceProperties(this CloudTableClient tableClient, ServiceProperties properties)
        {
            tableClient.SetServicePropertiesAsync(properties).GetAwaiter().GetResult();
        }

        public static IEnumerable<CloudTable> ListTables(this CloudTableClient tableClient,
            string prefix = null, 
            TableRequestOptions requestOptions = null, 
            OperationContext operationContext = null)
        {
            TableContinuationToken currentToken = null;
            int? maxResults = null;

            do
            {
                var listResults = tableClient.ListTablesSegmentedAsync(prefix,
                    maxResults,
                    currentToken,
                    requestOptions,
                    operationContext).GetAwaiter().GetResult();

                foreach (var tableItem in listResults.Results)
                {
                    yield return tableItem;
                }

                currentToken = listResults.ContinuationToken;
            } while (null != currentToken);
        }

        public static bool CreateIfNotExists(this CloudTable table)
        {
            return table.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }

        public static bool DeleteIfExists(this CloudTable table)
        {
            return table.DeleteIfExistsAsync().GetAwaiter().GetResult();
        }

        public static TablePermissions GetPermissions(this CloudTable table)
        {
            return table.GetPermissionsAsync().GetAwaiter().GetResult();
        }

        public static void Execute(this CloudTable table,
            TableOperation operation, 
            TableRequestOptions requestOptions = null, 
            OperationContext operationContext = null)
        {
            table.ExecuteAsync(operation, requestOptions, operationContext).GetAwaiter().GetResult();
        }

        public static IList<DynamicTableEntity> ExecuteQuery(this CloudTable table,
            TableQuery<DynamicTableEntity> query,
            TableRequestOptions requestOptions = null,
            OperationContext operationContext = null)
        {
            List<DynamicTableEntity> tableEntities = new List<DynamicTableEntity>();
            TableContinuationToken currentToken = null;

            do
            {
                var listResults = table.ExecuteQuerySegmentedAsync(query,
                    currentToken,
                    requestOptions,
                    operationContext).GetAwaiter().GetResult();

                tableEntities.AddRange(listResults.Results);

                currentToken = listResults.ContinuationToken;
            } while (null != currentToken);

            return tableEntities;
        }
        #endregion
    }
}
