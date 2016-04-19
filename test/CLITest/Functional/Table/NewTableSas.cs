namespace Management.Storage.ScenarioTest.Functional.Table
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Management.Storage.ScenarioTest.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    public class NewTableSas : TestBase
    {
        public static List<string> TablePermission;

        [ClassInitialize()]
        public static void NewTableSasClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
            TablePermission = Utility.TablePermission;
        }

        [ClassCleanup()]
        public static void NewTableSasClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// 1.	Generate SAS of a table with only limited access right(read, write,delete,list)
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithPermission()
        {
            //table read permission
            string tablePermission = "r";
            GenerateSasTokenAndValidate(tablePermission);

            //table read permission
            if (lang == Language.PowerShell)
            {
                tablePermission = "q";
                GenerateSasTokenAndValidate(tablePermission);
            }

            //table add permission
            tablePermission = "a";
            GenerateSasTokenAndValidate(tablePermission);

            //table update permission
            tablePermission = "u";
            GenerateSasTokenAndValidate(tablePermission);

            //table delete permission
            tablePermission = "d";
            GenerateSasTokenAndValidate(tablePermission);

            // Permission param is required according to the design, cannot accept string.Empty, so comment this. We may support this in the future.
            //None permission
            //tablePermission = "";
            //GenerateSasTokenAndValidate(tablePermission);

            //Full permission
            tablePermission = "raud";
            GenerateSasTokenAndValidate(tablePermission);

            //Full permission with q
            tablePermission = "raudq";
            GenerateSasTokenAndValidate(tablePermission);

            //Random combination
            tablePermission = Utility.GenRandomCombination(NewTableSas.TablePermission);
            GenerateSasTokenAndValidate(tablePermission);
        }

        /// <summary>
        /// 2.	Generate SAS of a table with a limited time period
        /// Wait for the time expiration
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithLifeTime()
        {
            CloudTable table = tableUtil.CreateTable();
            double lifeTime = 3; //Minutes
            const double deltaTime = 0.5;
            DateTime startTime = DateTime.Now.AddMinutes(lifeTime);
            DateTime expiryTime = startTime.AddMinutes(lifeTime);

            try
            {
                string tablePermission = Utility.GenRandomCombination(NewTableSas.TablePermission);
                string sastoken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, tablePermission, startTime, expiryTime);
                try
                {
                    ValidateSasToken(table, tablePermission, sastoken);
                    Test.Error(string.Format("Access table should fail since the start time is {0}, but now is {1}",
                        startTime.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }

                Test.Info("Sleep and wait for the sas token start time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));
                ValidateSasToken(table, tablePermission, sastoken);
                Test.Info("Sleep and wait for sas token expiry time");
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime + deltaTime));

                try
                {
                    ValidateSasToken(table, tablePermission, sastoken);
                    Test.Error(string.Format("Access table should fail since the expiry time is {0}, but now is {1}",
                        expiryTime.ToUniversalTime().ToString(), DateTime.UtcNow.ToString()));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// 3.	Generate SAS of a table by policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithPolicy()
        {
            CloudTable table = tableUtil.CreateTable();

            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                TablePermissions permission = new TablePermissions();
                string policyName = Utility.GenNameString("saspolicy");

                permission.SharedAccessPolicies.Add(policyName, new SharedAccessTablePolicy
                {
                    SharedAccessExpiryTime = DateTime.Now.Add(sasLifeTime),
                    Permissions = SharedAccessTablePermissions.Query
                });

                table.SetPermissions(permission);

                string sasToken = CommandAgent.GetTableSasFromCmd(table.Name, policyName, string.Empty);
                Test.Info("Sleep and wait for sas policy taking effect");
                double lifeTime = 1;
                Thread.Sleep(TimeSpan.FromMinutes(lifeTime));
                ValidateSasToken(table, "r", sasToken);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// 4.	Generate SAS of a table of a non-existing policy
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        public void NewTableSasWithNotExistPolicy()
        {
            CloudTable table = tableUtil.CreateTable();

            try
            {
                string policyName = Utility.GenNameString("notexistpolicy");

                Test.Assert(!CommandAgent.NewAzureStorageTableSAS(table.Name, policyName, string.Empty),
                    "Generate table sas token with not exist policy should fail");
                ExpectedContainErrorMessage(string.Format("Invalid access policy '{0}'.", policyName));
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// 3.	Generate SAS of a table with expiry time before start time
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithInvalidLifeTime()
        {
            CloudTable table = tableUtil.CreateTable();

            try
            {
                DateTime start = DateTime.UtcNow;
                DateTime end = start.AddHours(1.0);
                Test.Assert(!CommandAgent.NewAzureStorageTableSAS(table.Name, string.Empty, "d", end, start),
                        "Generate table sas token with invalid should fail");
                ExpectedContainErrorMessage("The expiry time of the specified access policy should be greater than start time");
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }

        }

        /// <summary>
        /// 4.	Return full uri with SAS token
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithFullUri()
        {
            CloudTable table = tableUtil.CreateTable();

            try
            {
                string tablePermission = Utility.GenRandomCombination(NewTableSas.TablePermission);
                string fullUri = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, tablePermission);
                string sasToken = (lang == Language.PowerShell ? fullUri.Substring(fullUri.IndexOf("?")) : fullUri);
                ValidateSasToken(table, tablePermission, sasToken);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// 1.	Generate SAS of a table with only limited access right(read,write,delete,list,none)
        ///     Verify access with the non-granted right to this table is denied
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithLimitedPermission()
        {
            CloudTable table = tableUtil.CreateTable();

            try
            {
                //table read permission
                string tablePermission = "r";
                string limitedPermission = "uda";
                string sastoken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, tablePermission);
                ValidateLimitedSasPermission(table, limitedPermission, sastoken);

                //table add permission
                tablePermission = "a";
                limitedPermission = "rdu";
                sastoken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, tablePermission);
                ValidateLimitedSasPermission(table, limitedPermission, sastoken);

                //table update permission
                tablePermission = "u";
                limitedPermission = "rad";
                sastoken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, tablePermission);
                ValidateLimitedSasPermission(table, limitedPermission, sastoken);

                //table delete permission
                tablePermission = "d";
                limitedPermission = "rau";
                sastoken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, tablePermission);
                ValidateLimitedSasPermission(table, limitedPermission, sastoken);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// 4.	Generate shared access signature of a non-existing table or a non-existing table
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithNotExistTable()
        {
            string tableName = Utility.GenNameString("table");
            CommandAgent.GetTableSasFromCmd(tableName, string.Empty, "r");
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithRangePK()
        {
            CloudTable table = tableUtil.CreateTable();
            string rk = Utility.GenNameString("rk");
            string key = Utility.GenNameString("key");
            string pk1 = Utility.GenNameString("pk1");
            string pk2 = Utility.GenNameString("pk2");
            string pk3 = Utility.GenNameString("pk3");
            string pk4 = Utility.GenNameString("pk4");
            string value1 = InsertTableEntity(table, pk1, rk, key);
            string value2 = InsertTableEntity(table, pk2, rk, key);
            string value3 = InsertTableEntity(table, pk3, rk, key);
            string value4 = InsertTableEntity(table, pk4, rk, key);

            //Range(PK2, Pk3)
            string permission = "r";
            string sasToken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, permission,
                null, null, false, pk2, string.Empty, pk3, string.Empty);
            CloudTable sasTable = tableUtil.GetTableBySasToken(table, sasToken);

            try
            {
                ExpectPermissionException(sasTable, pk1, rk, "Pk1 entity");
                ExpectEntityValue(value2, sasTable, pk2, rk, key, "PK2 entity");
                ExpectEntityValue(value3, sasTable, pk3, rk, key, "PK3 entity");
                ExpectPermissionException(sasTable, pk4, rk, "Pk4 entity");
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithRangeRK()
        {
            CloudTable table = tableUtil.CreateTable();
            string pk = Utility.GenNameString("pk");
            string key = Utility.GenNameString("key");
            string rk1 = Utility.GenNameString("rk1");
            string rk2 = Utility.GenNameString("rk2");
            string rk3 = Utility.GenNameString("rk3");
            string rk4 = Utility.GenNameString("rk4");
            string value1 = InsertTableEntity(table, pk, rk1, key);
            string value2 = InsertTableEntity(table, pk, rk2, key);
            string value3 = InsertTableEntity(table, pk, rk3, key);
            string value4 = InsertTableEntity(table, pk, rk4, key);

            try
            {
                //Range(RK2, Rk3)
                string permission = "r";
                string sasToken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, permission,
                    null, null, false, pk, rk2, pk, rk3);
                CloudTable sasTable = tableUtil.GetTableBySasToken(table, sasToken);

                ExpectPermissionException(sasTable, pk, rk1, "Rk1 entity");
                ExpectEntityValue(value2, sasTable, pk, rk2, key, "RK2 entity");
                ExpectEntityValue(value3, sasTable, pk, rk3, key, "RK3 entity");
                ExpectPermissionException(sasTable, pk, rk4, "Rk4 entity");
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithInvalidRangePK()
        {
            CloudTable table = tableUtil.CreateTable();
            string rk = Utility.GenNameString("rk");
            string key = Utility.GenNameString("key");
            string pk1 = Utility.GenNameString("pk1");
            string pk2 = Utility.GenNameString("pk2");
            string pk3 = Utility.GenNameString("pk3");
            string pk4 = Utility.GenNameString("pk4");
            string value1 = InsertTableEntity(table, pk1, rk, key);
            string value2 = InsertTableEntity(table, pk2, rk, key);
            string value3 = InsertTableEntity(table, pk3, rk, key);
            string value4 = InsertTableEntity(table, pk4, rk, key);
            string permission = "r";
            string sasToken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, permission,
                null, null, false, pk3, string.Empty, pk2, string.Empty);
            CloudTable sasTable = tableUtil.GetTableBySasToken(table, sasToken);

            try
            {
                ExpectPermissionException(sasTable, pk1, rk, "Pk1 entity");
                ExpectPermissionException(sasTable, pk2, rk, "PK2 entity");
                ExpectPermissionException(sasTable, pk3, rk, "PK3 entity");
                ExpectPermissionException(sasTable, pk4, rk, "Pk4 entity");
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Table)]
        [TestCategory(PsTag.NewTableSas)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.NewTableSas)]
        public void NewTableSasWithInvalidRangeRK()
        {
            CloudTable table = tableUtil.CreateTable();
            string pk = Utility.GenNameString("pk");
            string key = Utility.GenNameString("key");
            string rk1 = Utility.GenNameString("rk1");
            string rk2 = Utility.GenNameString("rk2");
            string rk3 = Utility.GenNameString("rk3");
            string rk4 = Utility.GenNameString("rk4");
            string value1 = InsertTableEntity(table, pk, rk1, key);
            string value2 = InsertTableEntity(table, pk, rk2, key);
            string value3 = InsertTableEntity(table, pk, rk3, key);
            string value4 = InsertTableEntity(table, pk, rk4, key);

            try
            {
                //Range(RK2, Rk3)
                string permission = "r";
                string sasToken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, permission,
                    null, null, false, pk, rk3, pk, rk2);
                CloudTable sasTable = tableUtil.GetTableBySasToken(table, sasToken);

                ExpectPermissionException(sasTable, pk, rk1, "Rk1 entity");
                ExpectPermissionException(sasTable, pk, rk2, "RK2 entity");
                ExpectPermissionException(sasTable, pk, rk3, "RK3 entity");
                ExpectPermissionException(sasTable, pk, rk4, "Rk4 entity");
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// 1.	Generate SAS of protocal: HttpsOrHttp, and all available value of permission.
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewTableSas)]
        public void NewTableSas_HttpsOrHttp()
        {
            CloudTable table = tableUtil.CreateTable();
            try
            {
                string sastoken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, "ruda", protocol: SharedAccessProtocol.HttpsOrHttp);

                tableUtil.ValidateTableQueryableWithSasToken(table, sastoken, useHttps: false);
                tableUtil.ValidateTableQueryableWithSasToken(table, sastoken, useHttps: true);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// 1. Generate SAS of IPAddressOrRange: [Not Current IP], and all available value of permission, protocal.
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewTableSas)]
        public void NewTableSas_NotCurrentIP()
        {
            CloudTable table = tableUtil.CreateTable();
            try
            {
                string sastoken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, "ruda", iPAddressOrRange: "10.10.10.10");
                try
                {
                    tableUtil.ValidateTableUpdateableWithSasToken(table, sastoken);
                    Test.Error(string.Format("Update Table should fail since the ipAcl not current IP."));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// 1. Generate SAS of IPAddressOrRange: [Range exclude Current IP], and all available value of permission.
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.NewTableSas)]
        public void NewTableSas_ExcludeIPRange()
        {
            CloudTable table = tableUtil.CreateTable();
            try
            {
                string sastoken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, "ruda", iPAddressOrRange: "10.10.10.10-10.10.10.11");
                try
                {
                    tableUtil.ValidateTableUpdateableWithSasToken(table, sastoken);
                    Test.Error(string.Format("Update Table should fail since the ip range not include current IP."));
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    ExpectEqual(e.RequestInformation.HttpStatusCode, 403, "(403) Forbidden");
                }
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Expect the table with sas token can't access the specified entity
        /// </summary>
        internal void ExpectPermissionException(CloudTable sasTable, string pk, string rk, string message)
        {
            Test.Info("Verifying {0}", message);
            TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rk)));
            List<DynamicTableEntity> retrievedEntity = null;
            try
            {
                retrievedEntity = sasTable.ExecuteQuery(query).ToList();
                TestBase.ExpectEqual(retrievedEntity.Count, 0, "None table entity");
            }
            catch (StorageException e)
            {
                Test.Info(e.Message);
                IsPermissionStorageException(e);
            }
        }

        /// <summary>
        /// Expect the table with sas token can access the specified entity
        /// </summary>
        internal void ExpectEntityValue(string expectedValue, CloudTable sasTable, string pk, string rk, string key, string message)
        {
            Test.Info("Verifying {0}", message);
            TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rk)));
            List<DynamicTableEntity> retrievedEntity = sasTable.ExecuteQuery(query).ToList();
            TestBase.ExpectEqual(retrievedEntity.Count, 1, "table entity");
            if (retrievedEntity.Count != 0)
            {
                TestBase.ExpectEqual(expectedValue, retrievedEntity[0].Properties[key].StringValue, message);
            }
            else
            {
                throw new ArgumentException("Can't retrieve the specified table entity");
            }
        }

        internal string InsertTableEntity(CloudTable table, string pk, string rk, string k)
        {
            string v = Utility.GenNameString("value");
            DynamicTableEntity entity = new DynamicTableEntity();
            entity.PartitionKey = pk;
            entity.RowKey = rk;
            entity.Properties.Add(k, new EntityProperty(v));
            TableOperation insertOp = TableOperation.Insert(entity);
            table.Execute(insertOp);
            return v;
        }

        /// <summary>
        /// Generate a sas token and validate it.
        /// </summary>
        /// <param name="tablePermission">table permission</param>
        internal void GenerateSasTokenAndValidate(string tablePermission)
        {
            CloudTable table = tableUtil.CreateTable();
            try
            {
                string sastoken = CommandAgent.GetTableSasFromCmd(table.Name, string.Empty, tablePermission);
                ValidateSasToken(table, tablePermission, sastoken);
            }
            finally
            {
                tableUtil.RemoveTable(table);
            }
        }

        /// <summary>
        /// Validate the sas token 
        /// </summary>
        /// <param name="table">Cloudtable object</param>
        /// <param name="tablePermission">table permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateSasToken(CloudTable table, string tablePermission, string sasToken)
        {
            foreach (char permission in tablePermission.ToLower())
            {
                switch (permission)
                {
                    case 'r':
                    case 'q':
                        tableUtil.ValidateTableQueryableWithSasToken(table, sasToken);
                        break;
                    case 'a':
                        tableUtil.ValidateTableAddableWithSasToken(table, sasToken);
                        break;
                    case 'u':
                        tableUtil.ValidateTableUpdateableWithSasToken(table, sasToken);
                        break;
                    case 'd':
                        tableUtil.ValidateTableDeleteableWithSasToken(table, sasToken);
                        break;
                }
            }
        }

        /// <summary>
        /// Validte the limited permission for sas token 
        /// </summary>
        /// <param name="table">CloudTable object</param>
        /// <param name="tablePermission">Limited permission</param>
        /// <param name="sasToken">sas token</param>
        internal void ValidateLimitedSasPermission(CloudTable table,
            string limitedPermission, string sasToken)
        {
            foreach (char permission in limitedPermission.ToLower())
            {
                try
                {
                    ValidateSasToken(table, permission.ToString(), sasToken);
                    Test.Error("sastoken '{0}' should not contain the permission {1}", sasToken, permission.ToString());
                }
                catch (StorageException e)
                {
                    Test.Info(e.Message);
                    IsPermissionStorageException(e);
                }
            }
        }

        internal bool IsPermissionStorageException(StorageException e)
        {
            if (403 == e.RequestInformation.HttpStatusCode)
            {
                Test.Info("Limited permission sas token should not access storage objects. {0}", e.RequestInformation.HttpStatusMessage);
                return true;
            }
            else
            {
                Test.Error("Limited permission sas token should return 403, but actually it's {0} {1}",
                    e.RequestInformation.HttpStatusCode, e.RequestInformation.HttpStatusMessage);
                return false;
            }
        }
    }
}
