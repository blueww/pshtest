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
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Management.Storage.ScenarioTest.Common;
using MS.Test.Common.MsTestLib;

namespace Management.Storage.ScenarioTest.Util
{
    public class CloudQueueUtil
    {
        private CloudStorageAccount account;
        private CloudQueueClient client;
        private Random random;

        private CloudQueueUtil()
        { }

        /// <summary>
        /// init cloud queue util
        /// </summary>
        /// <param name="account">storage account</param>
        public CloudQueueUtil(CloudStorageAccount account)
        {
            this.account = account;
            client = account.CreateCloudQueueClient();
            random = new Random();
        }

        /// <summary>
        /// create a container with random properties and metadata
        /// </summary>
        /// <param name="queueName">container name</param>
        /// <returns>the created container object with properties and metadata</returns>
        public CloudQueue CreateQueue(string queueName = "")
        {
            if (string.IsNullOrEmpty(queueName))
            {
                queueName = Utility.GenNameString("queue");
            }

            CloudQueue queue = client.GetQueueReference(queueName);
            queue.CreateIfNotExists();

            int count = random.Next(1, 5);
            for (int i = 0; i < count; i++)
            {
                string metaKey = Utility.GenNameString("metatest");
                int valueLength = random.Next(10, 20);
                string metaValue = Utility.GenNameString("metavalue-", valueLength);
                queue.Metadata.Add(metaKey, metaValue);
            }

            queue.SetMetadata();

            return queue;
        }

        /// <summary>
        /// create mutiple containers
        /// </summary>
        /// <param name="queueNames">container names list</param>
        /// <returns>a list of container object</returns>
        public List<CloudQueue> CreateQueue(List<string> queueNames)
        {
            List<CloudQueue> queues = new List<CloudQueue>();

            foreach (string name in queueNames)
            {
                queues.Add(CreateQueue(name));
            }

            queues = queues.OrderBy(queue => queue.Name).ToList();

            return queues;
        }

        /// <summary>
        /// remove specified container
        /// </summary>
        /// <param name="queueName">container name</param>
        public void RemoveQueue(string queueName)
        {
            CloudQueue queue = client.GetQueueReference(queueName);
            queue.DeleteIfExists();
        }

        /// <summary>
        /// remove a list containers
        /// </summary>
        /// <param name="queueNames">container names</param>
        public void RemoveQueue(List<string> queueNames)
        {
            foreach (string name in queueNames)
            {
                RemoveQueue(name);
            }
        }

        public int GetExistingQueueCount()
        {
            List<CloudQueue> queues = client.ListQueues().ToList();
            return queues.Count;
        }

        /// <summary>
        /// Validate the read permission in sastoken for the specified queue
        /// </summary>
        internal void ValidateQueueReadableWithSasToken(CloudQueue queue, string sasToken)
        {
            Test.Info("Verify queue read permission");
            CloudQueue sasQueue = GetQueueBySasToken(queue, sasToken);
            queue.FetchAttributes();
            sasQueue.FetchAttributes();
            int oldMessageCount = queue.ApproximateMessageCount.Value;
            TestBase.ExpectEqual(queue.ApproximateMessageCount.Value, sasQueue.ApproximateMessageCount.Value, "Message count");
            string content = Utility.GenNameString("message content");
            CloudQueueMessage message = new CloudQueueMessage(content);
            queue.AddMessage(message);
            queue.FetchAttributes();
            sasQueue.FetchAttributes();
            int newMessageCount = queue.ApproximateMessageCount.Value;
            TestBase.ExpectNotEqual(newMessageCount, oldMessageCount, "The message count after adding");
            TestBase.ExpectEqual(queue.ApproximateMessageCount.Value, sasQueue.ApproximateMessageCount.Value, "Message count");
            CloudQueueMessage retrievedMessage = queue.GetMessage();
            queue.DeleteMessage(retrievedMessage);
        }

        /// <summary>
        /// Validate the add permission in sastoken for the specified queue
        /// </summary>
        internal void ValidateQueueAddableWithSasToken(CloudQueue queue, string sasToken)
        {
            Test.Info("Verify queue add permission");
            CloudQueue sasQueue = GetQueueBySasToken(queue, sasToken);
            queue.FetchAttributes();
            int oldMessageCount = queue.ApproximateMessageCount.Value;
            string content = Utility.GenNameString("message content");
            CloudQueueMessage message = new CloudQueueMessage(content);
            sasQueue.AddMessage(message);
            queue.FetchAttributes();
            int newMessageCount = queue.ApproximateMessageCount.Value;
            TestBase.ExpectNotEqual(newMessageCount, oldMessageCount, "The message count after adding");
            CloudQueueMessage retrievedMessage = queue.GetMessage();
            TestBase.ExpectEqual(content, retrievedMessage.AsString, "message content");
            queue.DeleteMessage(retrievedMessage);
        }

        /// <summary>
        /// Validate the delete permission in sastoken for the specified queue
        /// </summary>
        internal void ValidateQueueRemoveableWithSasToken(CloudQueue queue, string sasToken)
        {
            Test.Info("Verify queue remove permission");
            CloudQueue sasQueue = GetQueueBySasToken(queue, sasToken);
            sasQueue.Delete();
        }

        /// <summary>
        /// Validate the update permission in sastoken for the specified queue
        /// </summary>
        internal void ValidateQueueUpdateableWithSasToken(CloudQueue queue, string sasToken)
        {
            Test.Info("Verify queue update permission");
            CloudQueue sasQueue = GetQueueBySasToken(queue, sasToken);
            string content = Utility.GenNameString("message content");
            CloudQueueMessage message = new CloudQueueMessage(content);
            queue.AddMessage(message);
            CloudQueueMessage freshedMessage = queue.GetMessage();
            string newContent = Utility.GenNameString("new message content");
            freshedMessage.SetMessageContent(newContent);
            sasQueue.UpdateMessage(freshedMessage, TimeSpan.FromSeconds(0.0)/*Make it visible immediately.*/,
                MessageUpdateFields.Content | MessageUpdateFields.Visibility);
            message = queue.GetMessage();
            TestBase.ExpectEqual(newContent, message.AsString, "message content");
            queue.DeleteMessage(message);
        }

        /// <summary>
        /// Validate the process permission in sastoken for the specified queue
        /// </summary>
        internal void ValidateQueueProcessableWithSasToken(CloudQueue queue, string sasToken)
        {
            Test.Info("Verify queue process permission");
            CloudQueue sasQueue = GetQueueBySasToken(queue, sasToken);
            string content = Utility.GenNameString("message content");
            CloudQueueMessage message = new CloudQueueMessage(content);
            queue.AddMessage(message);
            queue.FetchAttributes();
            int oldMessageCount = queue.ApproximateMessageCount.Value;
            CloudQueueMessage sasMessage = sasQueue.GetMessage();
            sasQueue.DeleteMessage(sasMessage);
            queue.FetchAttributes();
            int newMessageCount = queue.ApproximateMessageCount.Value;
            TestBase.ExpectNotEqual(newMessageCount, oldMessageCount, "The message count after deleting");
        }

        /// <summary>
        /// Get the CloudQueue object with sas token
        /// </summary>
        /// <param name="queue">CloudQueue object with full permission</param>
        /// <param name="sasToken">Sas token</param>
        /// <returns>CloudQueue object with the permission specified by sas token</returns>
        private CloudQueue GetQueueBySasToken(CloudQueue queue, string sasToken)
        {
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(queue.ServiceClient.Credentials.AccountName, sasToken);
            CloudQueueClient sasClient = sasAccount.CreateCloudQueueClient();
            CloudQueue sasQueue = sasClient.GetQueueReference(queue.Name);
            return sasQueue;
        }

        /// <summary>
        /// Remove azure storage queue
        /// </summary>
        internal void RemoveQueue(CloudQueue queue)
        {
            queue.DeleteIfExists();
        }
    }
}
