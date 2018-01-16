using System;
using System.Collections.Generic;
using System.Linq;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Commands.Storage.Model.ResourceModel;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using MS.Test.Common.MsTestLib;
using Newtonsoft.Json.Linq;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.Service
{
    [TestClass]
    public class ServiceProperties : TestBase
    {
        [ClassInitialize()]
        public static void DefaultServiceVersionClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void DefaultServiceVersionClassCleanup()
        {
            ServiceCORSRule.ClearCorsRules(Constants.ServiceType.Blob);
            ServiceCORSRule.ClearCorsRules(Constants.ServiceType.Table);
            ServiceCORSRule.ClearCorsRules(Constants.ServiceType.Queue);
            ServiceCORSRule.ClearCorsRules(Constants.ServiceType.File);

            TestBase.TestClassCleanup();
        }

        public string[] ValidServiceType = new string[] { "Blob", "Table", "File", "Queue" };

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void GetServiceProperties_AllService_AllProperties()
        {
            //prepare the service properties to set and get 
            PSCorsRule[] corsRules = CORSRuleUtil.GetRandomValidCORSRules(random.Next(1, 5));

            Microsoft.WindowsAzure.Storage.Shared.Protocol.ServiceProperties serviceProperties = new Microsoft.WindowsAzure.Storage.Shared.Protocol.ServiceProperties();
            serviceProperties.Clean();
            serviceProperties.Cors = new CorsProperties();

            foreach (var rule in corsRules)
            {
                CorsRule corsRule = new CorsRule()
                {
                    AllowedHeaders = rule.AllowedHeaders,
                    AllowedOrigins = rule.AllowedOrigins,
                    ExposedHeaders = rule.ExposedHeaders,
                    MaxAgeInSeconds = rule.MaxAgeInSeconds,
                };
                SetAllowedMethods(corsRule, rule.AllowedMethods);
                serviceProperties.Cors.CorsRules.Add(corsRule);
            }
            serviceProperties.HourMetrics = new MetricsProperties("1.0");
            serviceProperties.HourMetrics.MetricsLevel = MetricsLevel.ServiceAndApi;
            serviceProperties.HourMetrics.RetentionDays = 1;
            serviceProperties.MinuteMetrics = new MetricsProperties("1.0");
            serviceProperties.MinuteMetrics.MetricsLevel = MetricsLevel.Service;
            serviceProperties.MinuteMetrics.RetentionDays = 3;

            serviceProperties.Logging = new LoggingProperties("1.0");
            serviceProperties.Logging.LoggingOperations = LoggingOperations.All;
            serviceProperties.Logging.RetentionDays = 5;


            foreach (string servicetype in ValidServiceType)
            {
                Constants.ServiceType service = Constants.ServiceType.Blob;
                Enum.TryParse<Constants.ServiceType>(servicetype, true, out service);
                if (service == Constants.ServiceType.Blob) //only Blob support default service version
                {
                    serviceProperties.DefaultServiceVersion = "2017-04-17";

                    serviceProperties.DeleteRetentionPolicy = new DeleteRetentionPolicy();
                    serviceProperties.DeleteRetentionPolicy.Enabled = true;
                    serviceProperties.DeleteRetentionPolicy.RetentionDays = 10;
                }

                //Set Service Properties with XSCL API
                ServiceCORSRule.SetSerivceProperties(service, serviceProperties);
                
                //Get Service Properties with PowerShell
                PSSeriviceProperties properties = GetServicePropertiesFromPSH(service);

                //Validate Cors, metric, logging
                CORSRuleUtil.ValidateCORSRules(corsRules, properties.Cors);

                if (service != Constants.ServiceType.File) // File service don't support logging
                {
                    ExpectEqual(serviceProperties.Logging.Version, properties.Logging.Version, "Logging version");
                    ExpectEqual(serviceProperties.Logging.LoggingOperations.ToString(), properties.Logging.LoggingOperations.ToString(), "Logging Operations");
                    ExpectEqual(serviceProperties.Logging.RetentionDays.Value, properties.Logging.RetentionDays.Value, "Logging RetentionDays");
                }

                ExpectEqual(serviceProperties.HourMetrics.Version, properties.HourMetrics.Version, "HourMetrics Version");
                ExpectEqual(serviceProperties.HourMetrics.MetricsLevel.ToString(), properties.HourMetrics.MetricsLevel.ToString(), "HourMetrics MetricsLevel");
                ExpectEqual(serviceProperties.HourMetrics.RetentionDays.Value, properties.HourMetrics.RetentionDays.Value, "HourMetrics RetentionDays");

                ExpectEqual(serviceProperties.MinuteMetrics.Version, properties.MinuteMetrics.Version, "MinuteMetrics Version");
                ExpectEqual(serviceProperties.MinuteMetrics.MetricsLevel.ToString(), properties.MinuteMetrics.MetricsLevel.ToString(), "MinuteMetrics MetricsLevel");
                ExpectEqual(serviceProperties.MinuteMetrics.RetentionDays.Value, properties.MinuteMetrics.RetentionDays.Value, "MinuteMetrics RetentionDays");


                if (service == Constants.ServiceType.Blob)
                {
                    ExpectEqual(serviceProperties.DefaultServiceVersion, properties.DefaultServiceVersion, "DefaultServiceVersion");

                    ExpectEqual(serviceProperties.DeleteRetentionPolicy.Enabled.ToString(), properties.DeleteRetentionPolicy.Enabled.ToString(), "DeleteRetentionPolicy Enabled");
                    ExpectEqual(serviceProperties.DeleteRetentionPolicy.RetentionDays.Value, properties.DeleteRetentionPolicy.RetentionDays.Value, "DeleteRetentionPolicy RetentionDays");

                    serviceProperties.DeleteRetentionPolicy = null;
                    serviceProperties.DefaultServiceVersion = null;
                }
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void SetGetServiceProperties_DefaultServiceVersion_Blob()
        {
            Constants.ServiceType service = Constants.ServiceType.Blob;
            string DefaultServiceVersion = "2016-05-31";

            Test.Assert(CommandAgent.UpdateAzureStorageServiceProperties(service, DefaultServiceVersion), "SetAzureStorageServiceProperties with service {0} defaultserviceversion {1} should success.", service, DefaultServiceVersion);
            PSSeriviceProperties properties = GetServicePropertiesFromPSH(service);

            ExpectEqual(DefaultServiceVersion, properties.DefaultServiceVersion, "DefaultServiceVersion");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void SetGetServiceProperties_DefaultServiceVersion_File_Neg()
        {
            Constants.ServiceType service = Constants.ServiceType.File;
            string DefaultServiceVersion = "2016-05-31";

            Test.Assert(CommandAgent.UpdateAzureStorageServiceProperties(service, DefaultServiceVersion), "SetAzureStorageServiceProperties with service {0} defaultserviceversion {1} should success.", service, DefaultServiceVersion);
            PSSeriviceProperties properties = GetServicePropertiesFromPSH(service);

            ExpectNotEqual(DefaultServiceVersion, properties.DefaultServiceVersion, "DefaultServiceVersion");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void SetServiceProperties_DefaultServiceVersion_Table_Neg()
        {
            Constants.ServiceType service = Constants.ServiceType.Table;
            string DefaultServiceVersion = "2016-05-31";

            Test.Assert(!CommandAgent.UpdateAzureStorageServiceProperties(service, DefaultServiceVersion), "SetAzureStorageServiceProperties with service {0} defaultserviceversion {1} should fail.", service, DefaultServiceVersion);
            ExpectedContainErrorMessage("XML specified is not syntactically valid.");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void SetServiceProperties_DefaultServiceVersion_Queue_Neg()
        {
            Constants.ServiceType service = Constants.ServiceType.Queue;
            string DefaultServiceVersion = "2016-05-31";

            Test.Assert(!CommandAgent.UpdateAzureStorageServiceProperties(service, DefaultServiceVersion), "SetAzureStorageServiceProperties with service {0} defaultserviceversion {1} should fail.", service, DefaultServiceVersion);
            ExpectedContainErrorMessage("XML specified is not syntactically valid.");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void SetServiceProperties_InvalidDefaultServiceVersion_Blob()
        {
            Constants.ServiceType service = Constants.ServiceType.Blob;
            string DefaultServiceVersion = "2016-01-02";

            Test.Assert(!CommandAgent.UpdateAzureStorageServiceProperties(service, DefaultServiceVersion), "SetAzureStorageServiceProperties with service {0} defaultserviceversion {1} should fail.", service, DefaultServiceVersion);
            ExpectedContainErrorMessage("XML specified is not syntactically valid.");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void Enable_DisableAzureStorageServiceDeleteRetentionPolicy()
        {
            int[] validRetentionDays = {1, 10, 365};

            foreach (int retentionDays in validRetentionDays)
            {
                //Enable DeleteRetentionPolicy
                Test.Assert(CommandAgent.EnableAzureStorageDeleteRetentionPolicy(retentionDays), "EnableAzureStorageDeleteRetentionPolicy with retentionDays {0} should success.", retentionDays);
                PSSeriviceProperties properties = GetServicePropertiesFromPSH(Constants.ServiceType.Blob);

                Test.Assert(properties.DeleteRetentionPolicy.Enabled, "DeleteRetentionPolicy Enabled should be enabled.");
                ExpectEqual(retentionDays, properties.DeleteRetentionPolicy.RetentionDays.Value, "retentionDays");
                
                //Disable DeleteRetentionPolicy
                Test.Assert(CommandAgent.DisableAzureStorageDeleteRetentionPolicy(PassThru: true), "DisableAzureStorageDeleteRetentionPolicy should success.");
                properties = GetServicePropertiesFromPSH(Constants.ServiceType.Blob);

                Test.Assert(!properties.DeleteRetentionPolicy.Enabled, "DeleteRetentionPolicy Enabled should be disabled.");
                Test.Assert(properties.DeleteRetentionPolicy.RetentionDays == null, "DeleteRetentionPolicy RetentionDays should be null.");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void EnableAzureStorageServiceDeleteRetentionPolicy_invalidRetentionDays()
        {
            int[] invalidRetentionDays = { -1, 0, 366 };

            foreach (int retentionDays in invalidRetentionDays)
            {
                Test.Assert(!CommandAgent.EnableAzureStorageDeleteRetentionPolicy(retentionDays), "EnableAzureStorageDeleteRetentionPolicy with retentionDays {0} should success.", retentionDays);
                ExpectedContainErrorMessage("RetentionDays must be greater than 0 and less than or equal to 365 days.");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void AzureStorageServiceDeleteRetentionPolicy_PassThru()
        {
            int retentionDays = 7;

            //Enable DeleteRetentionPolicy
            Test.Assert(CommandAgent.EnableAzureStorageDeleteRetentionPolicy(retentionDays, PassThru: true), "EnableAzureStorageDeleteRetentionPolicy with retentionDays {0} should success.", retentionDays);
            PSDeleteRetentionPolicy policy = CommandAgent.Output[0][PowerShellAgent.BaseObject] as PSDeleteRetentionPolicy;

            Test.Assert(policy.Enabled, "DeleteRetentionPolicy Enabled should be enabled.");
            ExpectEqual(retentionDays, policy.RetentionDays.Value, "retentionDays");

            //Disable DeleteRetentionPolicy
            Test.Assert(CommandAgent.DisableAzureStorageDeleteRetentionPolicy(PassThru: true), "DisableAzureStorageDeleteRetentionPolicy should success.");
            policy = CommandAgent.Output[0][PowerShellAgent.BaseObject] as PSDeleteRetentionPolicy;

            Test.Assert(!policy.Enabled, "DeleteRetentionPolicy Enabled should be disabled.");
            Test.Assert(policy.RetentionDays == null, "DeleteRetentionPolicy RetentionDays should be null.");
        }

        private PSSeriviceProperties GetServicePropertiesFromPSH(Constants.ServiceType service)
        {
            Test.Assert(CommandAgent.GetAzureStorageServiceProperties(service), "GetAzureStorageServiceProperties with service as {0} should success.", service);
            return CommandAgent.Output[0][PowerShellAgent.BaseObject] as PSSeriviceProperties;
        }   
        
        /// <summary>
        /// Set Allowed method for a Cors Rule from a string array
        /// </summary>
        private void SetAllowedMethods(CorsRule corsRule, string[] allowedMethods)
        {
            corsRule.AllowedMethods = CorsHttpMethods.None;

            if (null != allowedMethods)
            {
                foreach (var method in allowedMethods)
                {
                    CorsHttpMethods allowedCorsMethod = CorsHttpMethods.None;
                    if (Enum.TryParse<CorsHttpMethods>(method, true, out allowedCorsMethod))
                    {
                        corsRule.AllowedMethods |= allowedCorsMethod;
                    }
                    else
                    {
                        Test.Error(string.Format("Can't parse {0} to CorsHttpMethods.", method));
                    }
                }
            }
        }
    }
}
