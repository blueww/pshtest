namespace Management.Storage.ScenarioTest.Functional.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    public class SetServiceMetrics : SetServiceLogging
    {
        [ClassInitialize()]
        public static void SetServiceMetricsClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void SetServiceMetricsClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceMetrics)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceMetrics)]
        public void EnableDisableServiceMetrics()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericEnableDisableServiceMetrics(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericEnableDisableServiceMetrics(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericEnableDisableServiceMetrics(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericEnableDisableServiceMetrics(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Enable/Disable service hour metrics for {0}", serviceType);
            int retentionDays = 10;
            Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, retentionDays.ToString(), string.Empty), "Enable service hour metrics should succeed");
            ServiceProperties retrievedProperties;
            if (lang == Language.PowerShell)
            {
                retrievedProperties = getServiceProperties();
                ExpectEqual(retentionDays, retrievedProperties.HourMetrics.RetentionDays.Value, "metrics retention days");
            }
            else
            {
                // getServiceProperties() takes several seconds to get the correct properties when retention is turned off by nodejs
                // because the .net and node xscl may connect to different frontend and take some time to sync 
                dynamic metrics = agent.Output[0]["HourMetrics"];
                Test.Assert((bool)metrics[0].RetentionPolicy.Enabled, "service minute metrics retention should be turned on");
                int retention = metrics[0].RetentionPolicy.Days ?? 0;
                ExpectEqual(retentionDays, retention, "metrics retention days");
            }
            retentionDays = -1;
            Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, retentionDays.ToString(), string.Empty), "Disable blob service hour metrics should succeed");
            if (lang == Language.PowerShell)
            {
                retrievedProperties = getServiceProperties();
                Test.Assert(!retrievedProperties.HourMetrics.RetentionDays.HasValue, "service hour metrics retention days should be null");
            }
            else
            {
                dynamic metrics = agent.Output[0]["HourMetrics"];
                Test.Assert(!(bool)metrics[0].RetentionPolicy.Enabled, "service hourly metrics retention should be turned off");
                Test.Assert(metrics[0].RetentionPolicy.Days == null, "service hourly metrics retention days should be null");
            }

            Test.Info("Enable/Disable service minute metrics for {0}", serviceType);
            retentionDays = 10;
            Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Enable service minute metrics should succeed");
            if (lang == Language.PowerShell)
            {
                retrievedProperties = getServiceProperties();
                ExpectEqual(retentionDays, retrievedProperties.MinuteMetrics.RetentionDays.Value, "metrics retention days");
            }
            else
            {
                dynamic metrics = agent.Output[0]["MinuteMetrics"];
                Test.Assert((bool)metrics[0].RetentionPolicy.Enabled, "service minute metrics retention should be turned on");
                int retention = metrics[0].RetentionPolicy.Days ?? 0;
                ExpectEqual(retentionDays, retention, "metrics retention days");
            }
            if (lang == Language.PowerShell)
            {
                retentionDays = -1;
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Disable blob service minute metrics should succeed");

                retrievedProperties = getServiceProperties();
                Test.Assert(!retrievedProperties.MinuteMetrics.RetentionDays.HasValue, "service minute metrics retention days should be null");
            }
            else
            {
                retentionDays = 0;
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Disable blob service minute metrics should succeed");

                dynamic metrics = agent.Output[0]["MinuteMetrics"];
                Test.Assert(!(bool)metrics[0].RetentionPolicy.Enabled, "service minute metrics retention should be turned off");
                Test.Assert(metrics[0].RetentionPolicy.Days == null, "service minute metrics retention days should be null");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceMetrics)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceMetrics)]
        public void SetMetricsOperation()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericSetMetricsOperation(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericSetMetricsOperation(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericSetMetricsOperation(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericSetMetricsOperation(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Set service metrics operation for {0}", serviceType);
            string[] operations = { "None", "Service", "ServiceAndApi"};
            foreach (string op in operations)
            {
                ExpectValidmetricsOperation(serviceType, op, getServiceProperties);
            }
        }

        internal void ExpectValidmetricsOperation(Constants.ServiceType serviceType, 
            string operations, Func<ServiceProperties> getServiceProperties)
        {
            int retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
            if (lang == Language.PowerShell)
            {
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, operations, retentionDays.ToString(), string.Empty), "Set hour metrics level should succeed");
                ServiceProperties retrievedProperties = getServiceProperties();
                MetricsLevel expectOperation = (MetricsLevel)Enum.Parse(typeof(MetricsLevel), operations, true);
                ExpectEqual(expectOperation.ToString(), retrievedProperties.HourMetrics.MetricsLevel.ToString(), "hour metrics level");

                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, operations, retentionDays.ToString(), string.Empty), "Set minute metrics level should succeed");
                retrievedProperties = getServiceProperties();
                expectOperation = (MetricsLevel)Enum.Parse(typeof(MetricsLevel), operations, true);
                ExpectEqual(expectOperation.ToString(), retrievedProperties.MinuteMetrics.MetricsLevel.ToString(), "minute metrics level");
            }
            else
            {
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, operations, retentionDays.ToString(), string.Empty), "Set hour metrics level should succeed");
                dynamic metrics = agent.Output[0]["HourMetrics"];
                bool api = metrics[0].IncludeAPIs;

                if (string.Compare(operations, "ServiceAndApi", true) == 0)
                {
                    Test.Assert(api, string.Format("expected metrics for api is true, actually it's '{0}'", api));
                }
                else
                {
                    Test.Assert(!api, string.Format("expected metrics for api is false, actually it's '{0}'", api));
                }

                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, operations, retentionDays.ToString(), string.Empty), "Set minute metrics level should succeed");
                metrics = agent.Output[0]["MinuteMetrics"];
                api = metrics[0].IncludeAPIs;

                if (string.Compare(operations, "ServiceAndApi", true) == 0)
                {
                    Test.Assert(api, string.Format("expected metrics for api is true, actually it's '{0}'", api));
                }
                else
                {
                    Test.Assert(!api, string.Format("expected metrics for api is false, actually it's '{0}'", api));
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceMetrics)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceMetrics)]
        public void SetInvalidMetricsOperation()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericSetInvalidMetricsOperation(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericSetInvalidMetricsOperation(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericSetInvalidMetricsOperation(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericSetInvalidMetricsOperation(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Set invalid service metrics operation for {0}", serviceType);
            string operation = "all";
            ExpectInvalidMetricsOperation(serviceType, operation, getServiceProperties);
            operation = "xxx";
            ExpectInvalidMetricsOperation(serviceType, operation, getServiceProperties, true);
        }

        internal void ExpectInvalidMetricsOperation(ServiceType serviceType, string operations,
            Func<ServiceProperties> getServiceProperties, bool invalidEnum = false)
        {
            int retentionDays = 1;

            if (lang == Language.PowerShell)
            {
                Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, operations, retentionDays.ToString(), string.Empty), "Set invalid hour metrics operation should fail");
                ExpectedStartsWithErrorMessage("Cannot bind parameter 'MetricsLevel'. Cannot convert value");

                Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, operations, retentionDays.ToString(), string.Empty), "Set invalid minute metrics operation should fail");
                ExpectedStartsWithErrorMessage("Cannot bind parameter 'MetricsLevel'. Cannot convert value");
            }
            else
            {
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, operations, retentionDays.ToString(), string.Empty), "Set any value with hour metrics switch operation should succeed");
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, operations, retentionDays.ToString(), string.Empty), "Set any value with  minute metrics operation should succeed");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceMetrics)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceMetrics)]
        public void SetMetricsRetentionDay()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericSetMetricsRetentionDays(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericSetMetricsRetentionDays(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericSetMetricsRetentionDays(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericSetMetricsRetentionDays(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Set service metrics retention days for {0}", serviceType);

            if (lang == Language.PowerShell)
            {
                // valid values for RetentionDay
                int retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, retentionDays.ToString(), string.Empty), "Set service hour metrics retention days should succeed");
                ServiceProperties retrievedProperties = getServiceProperties();
                ExpectEqual(retentionDays, retrievedProperties.HourMetrics.RetentionDays.Value, "metrics retention days");

                retentionDays = -1;
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, retentionDays.ToString(), string.Empty), "Set service hour metrics retention days should succeed");
                retrievedProperties = getServiceProperties();
                Test.Assert(!retrievedProperties.HourMetrics.RetentionDays.HasValue, "Service metrics retention days should be null");

                // valid values for RetentionDay
                retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Set service minute metrics retention days should succeed");
                retrievedProperties = getServiceProperties();
                ExpectEqual(retentionDays, retrievedProperties.MinuteMetrics.RetentionDays.Value, "metrics retention days");

                retentionDays = -1;
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Set service minute metrics retention days should succeed");
                retrievedProperties = getServiceProperties();
                Test.Assert(!retrievedProperties.MinuteMetrics.RetentionDays.HasValue, "Service metrics retention days should be null");

                // invalid values for RetentionDay
                retentionDays = 0;
                Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, retentionDays.ToString(), string.Empty), "Set service hour metrics retention days for invalid retention days should fail");
                ExpectedStartsWithErrorMessage("The minimum value of retention days is 1, the largest value is 365 (one year).");

                retentionDays = -1 * Utility.GetRandomTestCount(2, 365 + 1);
                Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, retentionDays.ToString(), string.Empty), "Set service hour metrics retention days for invalid retention days should fail");
                ExpectedStartsWithErrorMessage("Cannot validate argument on parameter 'RetentionDays'");

                retentionDays = Utility.GetRandomTestCount(366, 365 + 100);
                Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, retentionDays.ToString(), string.Empty), "Set service hour metrics retention days for invalid retention days should fail");
                ExpectedStartsWithErrorMessage("Cannot validate argument on parameter 'RetentionDays'");

                // invalid values for RetentionDay
                retentionDays = 0;
                Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Set service minute metrics retention days for invalid retention days should fail");
                ExpectedStartsWithErrorMessage("The minimum value of retention days is 1, the largest value is 365 (one year).");

                retentionDays = -1 * Utility.GetRandomTestCount(2, 365 + 1);
                Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Set service minute metrics retention days for invalid retention days should fail");
                ExpectedStartsWithErrorMessage("Cannot validate argument on parameter 'RetentionDays'");

                retentionDays = Utility.GetRandomTestCount(366, 365 + 100);
                Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Set service minute metrics retention days for invalid retention days should fail");
                ExpectedStartsWithErrorMessage("Cannot validate argument on parameter 'RetentionDays'");
            }
            else
            {
                int retentionDays = Utility.GetRandomTestCount(1, 365 + 1);
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, retentionDays.ToString(), string.Empty), "Set service hour metrics retention days should succeed");
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Set service minute metrics retention days should succeed");

                retentionDays = 0;
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, retentionDays.ToString(), string.Empty), "Set service hour metrics retention days for 0 retention days should succeed");
                Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, retentionDays.ToString(), string.Empty), "Set service minute metrics retention days for 0 retention days should succeed");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceMetrics)]
        public void SetMetricsVersion()
        {
            //Blob service
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            GenericSetMetricsVersion(ServiceType.Blob, () => blobClient.GetServiceProperties());

            //Queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            GenericSetMetricsVersion(ServiceType.Queue, () => queueClient.GetServiceProperties());

            //Table service
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            GenericSetMetricsVersion(ServiceType.Table, () => tableClient.GetServiceProperties());
        }

        internal void GenericSetMetricsVersion(ServiceType serviceType, Func<ServiceProperties> getServiceProperties)
        {
            Test.Info("Enable/Disable service hour metrics for {0}", serviceType);
            double version = 1.0;
            Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, string.Empty, version.ToString()), "Set service hour metrics version should succeed");
            ServiceProperties retrievedProperties = getServiceProperties();
            ExpectEqual(version, Convert.ToDouble(retrievedProperties.HourMetrics.Version), "metrics version");

            double invalidVersion = -1.0 * Utility.GetRandomTestCount();
            Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, string.Empty, invalidVersion.ToString()), "Set invalid service hour metrics version should fail");
            ExpectedStartsWithErrorMessage("The remote server returned an error");
            retrievedProperties = getServiceProperties();
            ExpectEqual(version, Convert.ToDouble(retrievedProperties.HourMetrics.Version), "metrics version");

            //Make sure the invalid verion less than 1, otherwise we don't know whether the version is valid in the future.
            invalidVersion = 0.1 * Utility.GetRandomTestCount();
            Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Hour, string.Empty, string.Empty, invalidVersion.ToString()), "Set invalid service hour metrics version should fail");
            ExpectedStartsWithErrorMessage("The remote server returned an error");
            retrievedProperties = getServiceProperties();
            ExpectEqual(version, Convert.ToDouble(retrievedProperties.HourMetrics.Version), "metrics version");

            Test.Info("Enable/Disable service minute metrics for {0}", serviceType);
            version = 1.0;
            Test.Assert(agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, string.Empty, version.ToString()), "Set service minute metrics version should succeed");
            retrievedProperties = getServiceProperties();
            ExpectEqual(version, Convert.ToDouble(retrievedProperties.MinuteMetrics.Version), "metrics version");

            invalidVersion = -1.0 * Utility.GetRandomTestCount();
            Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, string.Empty, invalidVersion.ToString()), "Set invalid service minute metrics version should fail");
            ExpectedStartsWithErrorMessage("The remote server returned an error");
            retrievedProperties = getServiceProperties();
            ExpectEqual(version, Convert.ToDouble(retrievedProperties.MinuteMetrics.Version), "metrics version");

            //Make sure the invalid verion less than 1, otherwise we don't know whether the version is valid in the future.
            invalidVersion = 0.1 * Utility.GetRandomTestCount();
            Test.Assert(!agent.SetAzureStorageServiceMetrics(serviceType, Constants.MetricsType.Minute, string.Empty, string.Empty, invalidVersion.ToString()), "Set invalid service minute metrics version should fail");
            ExpectedStartsWithErrorMessage("The remote server returned an error");
            retrievedProperties = getServiceProperties();
            ExpectEqual(version, Convert.ToDouble(retrievedProperties.MinuteMetrics.Version), "metrics version");
        }
    }
}