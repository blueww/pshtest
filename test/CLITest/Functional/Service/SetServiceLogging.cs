namespace Management.Storage.ScenarioTest.Functional.Common
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;
    using Management.Storage.ScenarioTest.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using Constants = Management.Storage.ScenarioTest.Constants;
    using ServiceType = Management.Storage.ScenarioTest.Constants.ServiceType;

    [TestClass]
    public class SetServiceLogging : TestBase
    {
        [ClassInitialize()]
        public static void SetServiceLoggingClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void SetServiceLoggingClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceLogging)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceLogging)]
        public void EnableDisableServiceLogging()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericEnableDisableServiceLogging(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericEnableDisableServiceLogging(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericEnableDisableServiceLogging(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericEnableDisableServiceLogging(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Enable/Disable service logging for {0}", serviceType);
            int retentionDays = 10;
            Test.Assert(agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Enable service logging should succeed");
            ServiceProperties retrievedProperties;
            if (lang == Language.PowerShell)
            {
                retrievedProperties = getServiceProperties();
                ExpectEqual(retentionDays, retrievedProperties.Logging.RetentionDays.Value, "logging retention days");
            }
            else
            {
                // getServiceProperties() takes several seconds to get the correct properties when retention is turned off by nodejs
                // because the .net and node xscl may connect to different frontend and take some time to sync 
                dynamic retention = agent.Output[0]["RetentionPolicy"];
                Test.Assert((bool)retention.Enabled, "service logging retention should be turned on");
                int days = retention.Days ?? 0;
                ExpectEqual(retentionDays, days, "logging retention days");
            }
            
            if (lang == Language.PowerShell)
            {
                retentionDays = -1;
                Test.Assert(agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Enable blob service logging should succeed");

                retrievedProperties = getServiceProperties();
                Test.Assert(!retrievedProperties.Logging.RetentionDays.HasValue, "service logging retention days should be null");
            }
            else
            {
                retentionDays = 0;
                Test.Assert(agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Enable blob service logging should succeed");

                dynamic retention = agent.Output[0]["RetentionPolicy"];
                Test.Assert(!(bool)retention.Enabled, "service logging retention should be turned off");
                Test.Assert(retention.Days == null, "service logging retention days should be null");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceLogging)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceLogging)]
        public void SetLoggingOperation()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericSetLoggingOperation(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericSetLoggingOperation(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericSetLoggingOperation(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericSetLoggingOperation(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Set service logging operation for {0}", serviceType);
            string[] operations = { "None", "All", "Read", "Write", "Delete" };
            foreach (string op in operations)
            {
                ExpectValidLoggingOperation(serviceType, op, getServiceProperties);
            }
            
            //Random combine
            string[] combineOperations = {"Read", "Write", "Delete" };
            string combination = Utility.GenRandomCombination(combineOperations.ToList(), ",");
            Test.Info("Generate combined operations {0}", combination);
            ExpectValidLoggingOperation(serviceType, combination, getServiceProperties);

            //Test case for case-insesitive, duplicate keys, key order.
            combination = "Delete,write,deLETE";
            Test.Info("Generate combined operations {0}", combination);
            ExpectValidLoggingOperation(serviceType, combination, getServiceProperties);
        }

        internal void ExpectValidLoggingOperation(ServiceType serviceType, string operations, Func<ServiceProperties> getServiceProperties)
        {
            int retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
            Test.Assert(agent.SetAzureStorageServiceLogging(serviceType, operations, retentionDays.ToString(), string.Empty), "Set logging operation should succeed");

            if (lang == Language.PowerShell)
            {
                ServiceProperties retrievedProperties = getServiceProperties();
                LoggingOperations expectOperation = (LoggingOperations)Enum.Parse(typeof(LoggingOperations), operations, true);
                ExpectEqual(expectOperation.ToString(), retrievedProperties.Logging.LoggingOperations.ToString(), "logging operation");
            }
            else
            {
                bool? read = agent.Output[0]["Read"] as bool?;
                bool? write = agent.Output[0]["Write"] as bool?;
                bool? delete = agent.Output[0]["Delete"] as bool?;

                Utility.ValidateLoggingOperationProperty(operations, read, write, delete);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceLogging)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceLogging)]
        public void SetInvalidLoggingOperation()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericSetInvalidLoggingOperation(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericSetInvalidLoggingOperation(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericSetInvalidLoggingOperation(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericSetInvalidLoggingOperation(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Set invalid service logging operation for {0}", serviceType);

            if (lang == Language.PowerShell)
            {
                LoggingOperations[] loggingOperations = { LoggingOperations.All, LoggingOperations.Read };
                ExpectInvalidLoggingOperation(serviceType, loggingOperations, getServiceProperties);

                loggingOperations = new LoggingOperations[] { LoggingOperations.Delete, LoggingOperations.None };
                ExpectInvalidLoggingOperation(serviceType, loggingOperations, getServiceProperties);

                string operation = "delete,xxx";
                ExpectInvalidLoggingOperation(serviceType, operation, getServiceProperties, "Cannot bind parameter 'LoggingOperations'. Cannot convert value");
            }
            else
            {
                string operation = " --read --read-off ";
                string expectedError = "--read and --read-off cannot be both defined";
                ExpectInvalidLoggingOperation(serviceType, operation, getServiceProperties, expectedError);
            }       
        }

        internal void ExpectInvalidLoggingOperation(Constants.ServiceType serviceType, LoggingOperations[] operations,
            Func<ServiceProperties> getServiceProperties)
        {
            int retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
            Test.Assert(!agent.SetAzureStorageServiceLogging(serviceType, operations, retentionDays.ToString(), string.Empty), "Set invalid logging operation should fail");
            ExpectedStartsWithErrorMessage("None or All operation can't be used with other operations");
        }

        internal void ExpectInvalidLoggingOperation(Constants.ServiceType serviceType, string operations,
            Func<ServiceProperties> getServiceProperties, string expectedMessage)
        {
            int retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
            Test.Assert(!agent.SetAzureStorageServiceLogging(serviceType, operations, retentionDays.ToString(), string.Empty), "Set invalid logging operation should fail");
            ExpectedStartsWithErrorMessage(expectedMessage);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceLogging)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceLogging)]
        public void SetLoggingRetentionDay()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericSetLoggingRetentionDays(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericSetLoggingRetentionDays(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericSetLoggingRetentionDays(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericSetLoggingRetentionDays(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Set service logging retention days for {0}", serviceType);
            // valid values for RetentionDay
            int retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
            Test.Assert(agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Set service logging retention days should succeed");
            ServiceProperties retrievedProperties = getServiceProperties();
            ExpectEqual(retentionDays, retrievedProperties.Logging.RetentionDays.Value, "Logging retention days");

            if (lang == Language.PowerShell)
            {
                retentionDays = -1;
                Test.Assert(agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Set service logging retention days should succeed");
                retrievedProperties = getServiceProperties();
                Test.Assert(!retrievedProperties.Logging.RetentionDays.HasValue, "Service logging retention days should be null");

                // invalid values for RetentionDay
                retentionDays = 0;
                Test.Assert(!agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Set service logging retention days for invalid retention days should fail");
                ExpectedStartsWithErrorMessage("The minimum value of retention days is 1, the largest value is 365 (one year).");

                retentionDays = -1 * Utility.GetRandomTestCount(2, 365 + 1);
                Test.Assert(!agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Set service logging retention days for invalid retention days should fail");
                ExpectedStartsWithErrorMessage("Cannot validate argument on parameter 'RetentionDays'");

                retentionDays = Utility.GetRandomTestCount(366, 365 + 100);
                Test.Assert(!agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Set service logging retention days for invalid retention days should fail");
                ExpectedStartsWithErrorMessage("Cannot validate argument on parameter 'RetentionDays'");
            }
            else
            {
                retentionDays = 0;
                Test.Assert(agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Set service logging retention days for 0 retention days should succeed");

                retentionDays = Utility.GetRandomTestCount(366, 365 + 100);
                Test.Assert(!agent.SetAzureStorageServiceLogging(serviceType, string.Empty, retentionDays.ToString(), string.Empty), "Set service logging retention days for invalid retention days should fail");

                string[] errMessages = {"XML specified is not syntactically valid" , "Error"};
                ExpectedContainErrorMessage(errMessages);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceLogging)]
        public void SetLoggingVersion()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericSetLoggingVersion(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericSetLoggingVersion(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericSetLoggingVersion(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericSetLoggingVersion(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Enable/Disable service logging for {0}", serviceType);
            //Actually, it's the the only one valid version.
            double version = 1.0;
            Test.Assert(agent.SetAzureStorageServiceLogging(serviceType, string.Empty, string.Empty, version.ToString()), "Set service logging version should succeed");
            ServiceProperties retrievedProperties = getServiceProperties();
            ExpectEqual(version, Convert.ToDouble(retrievedProperties.Logging.Version), "Logging version");

            double invalidVersion = -1 * Utility.GetRandomTestCount();
            Test.Assert(!agent.SetAzureStorageServiceLogging(serviceType, string.Empty, string.Empty, invalidVersion.ToString()), "Set invalid service logging version should fail");
            ExpectedStartsWithErrorMessage("The remote server returned an error");
            retrievedProperties = getServiceProperties();
            ExpectEqual(version, Convert.ToDouble(retrievedProperties.Logging.Version), "Logging version");

            //Make sure the invalid verion less than 1, otherwise we don't know whether the version is valid in the future.
            invalidVersion = 0.1 * Utility.GetRandomTestCount();
            Test.Assert(!agent.SetAzureStorageServiceLogging(serviceType, string.Empty, string.Empty, invalidVersion.ToString()), "Set invalid service logging version should fail");
            ExpectedStartsWithErrorMessage("The remote server returned an error");
            retrievedProperties = getServiceProperties();
            ExpectEqual(version, Convert.ToDouble(retrievedProperties.Logging.Version), "Logging version");
        }
    }
}
