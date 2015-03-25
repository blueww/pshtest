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
using Microsoft.WindowsAzure.Storage.Table;
using Management.Storage.ScenarioTest.Common;
using MS.Test.Common.MsTestLib;
using System.Threading;

namespace Management.Storage.ScenarioTest.Util
{
    public class CloudTableUtil
    {
        private CloudStorageAccount account;
        private CloudTableClient client;
        private Random random;

        private CloudTableUtil()
        { }

        /// <summary>
        /// init cloud queue util
        /// </summary>
        /// <param name="account">storage account</param>
        public CloudTableUtil(CloudStorageAccount account)
        {
            this.account = account;
            client = account.CreateCloudTableClient();
            random = new Random();
        }

        /// <summary>
        /// create a container with random properties and metadata
        /// </summary>
        /// <param name="tableName">container name</param>
        /// <returns>the created container object with properties and metadata</returns>
        public CloudTable CreateTable(string tableName = "")
        {
            if (String.IsNullOrEmpty(tableName))
            {
                tableName = Utility.GenNameString("table");
            }

            CloudTable table = client.GetTableReference(tableName);
            table.CreateIfNotExists();

            return table;
        }

        /// <summary>
        /// create mutiple containers
        /// </summary>
        /// <param name="tableName">container names list</param>
        /// <returns>a list of container object</returns>
        public List<CloudTable> CreateTable(List<string> tableName)
        {
            List<CloudTable> tables = new List<CloudTable>();

            foreach (string name in tableName)
            {
                tables.Add(CreateTable(name));
            }

            tables = tables.OrderBy(table => table.Name).ToList();

            return tables;
        }

        /// <summary>
        /// remove specified container
        /// </summary>
        /// <param name="tableName">table name</param>
        public void RemoveTable(string tableName)
        {
            CloudTable table = client.GetTableReference(tableName);
            this.RemoveTable(table);
        }

        /// <summary>
        /// remove specified container
        /// </summary>
        public void RemoveTable(CloudTable table)
        {
            table.DeleteIfExists();

            // There is delay for deleting a table's permission infos to take effact.
            // Here wait for it.
            while (true)
            {
                try
                {
                    table.GetPermissions();
                }
                catch
                {
                    break;
                }

                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// remove a list containers
        /// </summary>
        /// <param name="tableNames">table names</param>
        public void RemoveTable(List<string> tableNames)
        {
            foreach (string name in tableNames)
            {
                RemoveTable(name);
            }
        }

        public int GetExistingTableCount()
        {
            List<CloudTable> tables = client.ListTables().ToList();
            return tables.Count;
        }

        /// <summary>
        /// Validate the query permission in sastoken for the specified table
        /// </summary>
        internal void ValidateTableQueryableWithSasToken(CloudTable table, string sasToken)
        {
            Test.Info("Verify table query permission");
            DynamicTableEntity entity = new DynamicTableEntity();
            string pk = Utility.GenNameString("pk");
            string rk = Utility.GenNameString("rk");
            string key = Utility.GenNameString("key");
            string value = Utility.GenNameString("value");
            entity.PartitionKey = pk;
            entity.RowKey = rk;
            entity.Properties.Add(key, new EntityProperty(value));
            TableOperation insertOp = TableOperation.Insert(entity);
            table.Execute(insertOp);
            CloudTable sasTable = GetTableBySasToken(table, sasToken);
            TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rk)));
            List<DynamicTableEntity> retrievedEntity = sasTable.ExecuteQuery(query).ToList();
            TestBase.ExpectEqual(retrievedEntity.Count, 1, "table entity");
            TestBase.ExpectEqual(value, retrievedEntity[0].Properties[key].StringValue, "entity properties");
        }

        /// <summary>
        /// Validate the add permission in sastoken for the specified table
        /// </summary>
        internal void ValidateTableAddableWithSasToken(CloudTable table, string sasToken)
        {
            Test.Info("Verify table add permission");
            DynamicTableEntity entity = new DynamicTableEntity();
            string pk = Utility.GenNameString("pk");
            string rk = Utility.GenNameString("rk");
            string key = Utility.GenNameString("key");
            string value = Utility.GenNameString("value");
            entity.PartitionKey = pk;
            entity.RowKey = rk;
            entity.Properties.Add(key, new EntityProperty(value));
            TableOperation insertOp = TableOperation.Insert(entity);
            CloudTable sasTable = GetTableBySasToken(table, sasToken);
            sasTable.Execute(insertOp);
            TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rk)));
            List<DynamicTableEntity> retrievedEntity = table.ExecuteQuery(query).ToList();
            TestBase.ExpectEqual(retrievedEntity.Count, 1, "table entity");
            TestBase.ExpectEqual(value, retrievedEntity[0].Properties[key].StringValue, "entity properties");
        }

        /// <summary>
        /// Validate the update permission in sastoken for the specified table
        /// </summary>
        internal void ValidateTableUpdateableWithSasToken(CloudTable table, string sasToken)
        {
            Test.Info("Verify table update permission");
            DynamicTableEntity entity = new DynamicTableEntity();
            string pk = Utility.GenNameString("pk");
            string rk = Utility.GenNameString("rk");
            string key = Utility.GenNameString("key");
            string value = Utility.GenNameString("value");
            entity.PartitionKey = pk;
            entity.RowKey = rk;
            entity.Properties.Add(key, new EntityProperty(value));
            TableOperation insertOp = TableOperation.Insert(entity);
            table.Execute(insertOp);
            CloudTable sasTable = GetTableBySasToken(table, sasToken);
            string newValue = Utility.GenNameString("new value");
            entity.Properties[key].StringValue = newValue;
            TableOperation updateOp = TableOperation.Replace(entity);
            sasTable.Execute(updateOp);
            TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rk)));
            List<DynamicTableEntity> retrievedEntity = table.ExecuteQuery(query).ToList();
            TestBase.ExpectEqual(retrievedEntity.Count, 1, "table entity");
            TestBase.ExpectEqual(newValue, retrievedEntity[0].Properties[key].StringValue, "entity properties");
        }

        /// <summary>
        /// Validate the delete permission in sastoken for the specified table
        /// </summary>
        internal void ValidateTableDeleteableWithSasToken(CloudTable table, string sasToken)
        {
            Test.Info("Verify table delete permission");
            DynamicTableEntity entity = new DynamicTableEntity();
            string pk = Utility.GenNameString("pk");
            string rk = Utility.GenNameString("rk");
            string key = Utility.GenNameString("key");
            string value = Utility.GenNameString("value");
            entity.PartitionKey = pk;
            entity.RowKey = rk;
            entity.Properties.Add(key, new EntityProperty(value));
            TableOperation insertOp = TableOperation.Insert(entity);
            table.Execute(insertOp);
            CloudTable sasTable = GetTableBySasToken(table, sasToken);
            TableOperation del = TableOperation.Delete(entity);
            sasTable.Execute(del);
        }

        /// <summary>
        /// Get CloudTable object by sas token
        /// </summary>
        /// <param name="table">CloudTable object</param>
        /// <param name="sasToken">string sas token</param>
        /// <returns>CloudTable object</returns>
        public CloudTable GetTableBySasToken(CloudTable table, string sasToken)
        {
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(table.ServiceClient.Credentials.AccountName, sasToken);
            CloudTableClient sasClient = sasAccount.CreateCloudTableClient();
            CloudTable sasTable = sasClient.GetTableReference(table.Name);
            return sasTable;
        }
    }
}
