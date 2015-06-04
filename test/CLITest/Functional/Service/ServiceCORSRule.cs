using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Commands.Storage.Model.ResourceModel;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using MS.Test.Common.MsTestLib;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.Service
{
    [TestClass]
    public class ServiceCORSRule : TestBase
    {
        [ClassInitialize()]
        public static void ServiceCORSRuleClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void ServiceCORSRuleClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        private const string NoOriginNoMethod0MaxCacheAgeError = "A CORS rule must contain at least one allowed origin and allowed method, and MaxAgeInSeconds cannot have a value less than zero.";
        
        [TestMethod]
        [TestCategory(Tag.Function)]
        public void OverwriteExistingCORSRuleOfXSCL()
        {
            Action<Constants.ServiceType> setCORSRules = (serviceType) =>
                {
                    PSCorsRule[] corsRules = CORSRuleUtil.GetRandomValidCORSRules(random.Next(1, 5));

                    ServiceProperties serviceProperties = new ServiceProperties();
                    serviceProperties.Clean();
                    serviceProperties.Cors = new CorsProperties();

                    foreach (var rule in corsRules)
                    {
                        serviceProperties.Cors.CorsRules.Add(new CorsRule()
                        {
                            AllowedHeaders = rule.AllowedHeaders,
                            AllowedOrigins = rule.AllowedOrigins,
                            ExposedHeaders = rule.ExposedHeaders,
                            MaxAgeInSeconds = rule.MaxAgeInSeconds,
                            AllowedMethods = (CorsHttpMethods)random.Next(1, 512)
                        });
                    }

                    this.SetSerivceProperties(serviceType, serviceProperties);
                };

            this.OverwriteCORSRules(setCORSRules, Constants.ServiceType.Blob);
            this.OverwriteCORSRules(setCORSRules, Constants.ServiceType.Queue);
            this.OverwriteCORSRules(setCORSRules, Constants.ServiceType.Table);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void OverwriteExistingCORSRuleOfCmdlet()
        {
            Action<Constants.ServiceType> setCORSRules = (serviceType) =>
            {
                PSCorsRule[] corsRules = CORSRuleUtil.GetRandomValidCORSRules(random.Next(1, 5));
                Test.Assert(agent.SetAzureStorageCORSRules(serviceType, corsRules),
                     "Set cors rule to blob service should succeed");
            };

            this.OverwriteCORSRules(setCORSRules, Constants.ServiceType.Blob);
            this.OverwriteCORSRules(setCORSRules, Constants.ServiceType.Queue);
            this.OverwriteCORSRules(setCORSRules, Constants.ServiceType.Table);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void SetCORSRuleBoundaryTest()
        {
            // 64 allowed origins
            PSCorsRule[] corsRules = new PSCorsRule[1];
            corsRules[0] = new PSCorsRule()
            {
                AllowedOrigins = CORSRuleUtil.GetRandomOrigins(64),
                AllowedMethods = CORSRuleUtil.GetRandomMethods(),
                AllowedHeaders = CORSRuleUtil.GetRandomHeaders(),
                ExposedHeaders = CORSRuleUtil.GetRandomHeaders(),
                MaxAgeInSeconds = random.Next(1, 1000)
            };

            this.ValidateCORSRuleSetGet(corsRules);
            
            // Allowed origin "*" 
            corsRules[0].AllowedOrigins = new string[] { "*" };
            this.ValidateCORSRuleSetGet(corsRules);

            // Origin length 256
            corsRules[0].AllowedOrigins = CORSRuleUtil.GetRandomOrigins(3, true);
            corsRules[0].AllowedOrigins[0] = Utility.GenNameString("", 256);
            this.ValidateCORSRuleSetGet(corsRules);

            corsRules[0].AllowedOrigins = CORSRuleUtil.GetRandomOrigins();

            // Allowed method "*"
            corsRules[0].AllowedMethods = new string[] { "*" };
            this.ValidateCORSRuleSetGet(corsRules);

            // Allowed header 64  defined headers
            corsRules[0].AllowedHeaders = CORSRuleUtil.GetRandomHeaders(64);
            this.ValidateCORSRuleSetGet(corsRules);

            // Allowed header 2 prefix headers
            corsRules[0].AllowedHeaders = CORSRuleUtil.GetRandomHeaders(null, 2);
            this.ValidateCORSRuleSetGet(corsRules);

            // Allowed header "*"
            corsRules[0].AllowedHeaders = new string[] { "*" };
            this.ValidateCORSRuleSetGet(corsRules);

            // Allowed header 256 chars
            corsRules[0].AllowedHeaders = CORSRuleUtil.GetRandomHeaders(3, 0, true);
            corsRules[0].AllowedHeaders[0] = Utility.GenNameString("", 256);
            this.ValidateCORSRuleSetGet(corsRules);

            // Exposed header 64  defined headers
            corsRules[0].ExposedHeaders = CORSRuleUtil.GetRandomHeaders(64);
            this.ValidateCORSRuleSetGet(corsRules);

            // Exposed header 2 prefixed headers
            corsRules[0].ExposedHeaders = CORSRuleUtil.GetRandomHeaders(null, 2);
            this.ValidateCORSRuleSetGet(corsRules);

            // Exposed header "*"
            corsRules[0].ExposedHeaders = new string[] { "*" };
            this.ValidateCORSRuleSetGet(corsRules);

            // Exposed header 256 chars
            corsRules[0].ExposedHeaders = CORSRuleUtil.GetRandomHeaders(3, 0, true);
            corsRules[0].ExposedHeaders[0] = Utility.GenNameString("", 256);
            this.ValidateCORSRuleSetGet(corsRules);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void Set0CORSRuleTest()
        {
            PSCorsRule[] corsRules = new PSCorsRule[0];
            this.ValidateCORSRuleSetGet(corsRules);
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void SetCORSRuleNegativeTest()
        {
            // No allowed origins
            PSCorsRule[] corsRules = new PSCorsRule[1];
            corsRules[0] = new PSCorsRule()
            {
                AllowedOrigins = new string[0],
                AllowedMethods = CORSRuleUtil.GetRandomMethods(),
                AllowedHeaders = CORSRuleUtil.GetRandomHeaders(),
                ExposedHeaders = CORSRuleUtil.GetRandomHeaders(),
                MaxAgeInSeconds = random.Next(1, 1000)
            };
            Constants.ServiceType serviceType = GetRandomServiceType();

            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Set cors rule without allowed origin to {0} service should fail", serviceType);

            ExpectedContainErrorMessage(NoOriginNoMethod0MaxCacheAgeError);
        }

        private void ValidateCORSRuleSetGet(PSCorsRule[] corsRules)
        {
            Constants.ServiceType serviceType = GetRandomServiceType();

            Test.Assert(agent.SetAzureStorageCORSRules(serviceType, corsRules), "Set cors rule with 64 allowed origins to {0} service should succeed", serviceType);

            Test.Assert(agent.GetAzureStorageCORSRules(serviceType), "Get CORS rules of {0} service should succeed", serviceType);

            CORSRuleUtil.ValidateCORSRules(corsRules, agent.Output[0]["_baseObject"] as PSCorsRule[]);
        }

        private Constants.ServiceType GetRandomServiceType()
        {
            var serviceTypes = Enum.GetValues(typeof(Constants.ServiceType));

            return (Constants.ServiceType)serviceTypes.GetValue(random.Next(0, serviceTypes.Length));
        }

        private void OverwriteCORSRules(Action<Constants.ServiceType> setCORSRules, Constants.ServiceType serviceType)
        {
            try
            {
                setCORSRules(serviceType);

                PSCorsRule[] newCorsRules = CORSRuleUtil.GetRandomValidCORSRules(random.Next(1, 5));

                Test.Assert(agent.SetAzureStorageCORSRules(serviceType, newCorsRules),
                    "Set cors rule to blob service should succeed");

                Test.Assert(agent.GetAzureStorageCORSRules(serviceType),
                    "Get cors rules of blob service should succeed.");

                PSCorsRule[] acturalRules = agent.Output[0]["_baseObject"] as PSCorsRule[];

                CORSRuleUtil.ValidateCORSRules(newCorsRules, acturalRules);
            }
            finally
            {
                ServiceProperties serviceProperties = new ServiceProperties();
                serviceProperties.Clean();
                serviceProperties.Cors = new CorsProperties();
                serviceProperties.Cors.CorsRules.Clear();

                this.SetSerivceProperties(serviceType, serviceProperties);
            }
        }

        private void SetSerivceProperties(Constants.ServiceType serviceType, ServiceProperties serviceProperties)
        {
            switch (serviceType)
            {
                case Constants.ServiceType.Blob:
                    StorageAccount.CreateCloudBlobClient().SetServiceProperties(serviceProperties);
                    break;
                case Constants.ServiceType.Queue:
                    StorageAccount.CreateCloudQueueClient().SetServiceProperties(serviceProperties);
                    break;
                case Constants.ServiceType.Table:
                    StorageAccount.CreateCloudTableClient().SetServiceProperties(serviceProperties);
                    break;
            }
        }
    }
}
