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
using Newtonsoft.Json.Linq;
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
        private const string CORSRuleInvalidError = "CORS rules setting is invalid. Please reference to \"https://msdn.microsoft.com/en-us/library/azure/hh452235.aspx\" to get detailed information.";
        
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceCORS)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceCORS)]
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
        [TestCategory(PsTag.ServiceCORS)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceCORS)]
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
        [TestCategory(PsTag.ServiceCORS)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceCORS)]
        public void ChangeCORSRules()
        {
            Constants.ServiceType serviceType = GetRandomServiceType();

            try
            {
                PSCorsRule[] corsRules = CORSRuleUtil.GetRandomValidCORSRules(random.Next(1, 5));
                Test.Assert(agent.SetAzureStorageCORSRules(serviceType, corsRules),
                    "Set cors rule to {0} service should succeed", serviceType);

                Test.Assert(agent.GetAzureStorageCORSRules(serviceType),
                    "Get CORS rule of {0} service should succeed.", serviceType);

                PSCorsRule[] newCorsRules = GetCORSRules();

                CORSRuleUtil.ValidateCORSRules(corsRules, newCorsRules);

                foreach (var corsRule in newCorsRules)
                {
                    switch (random.Next(0, 5))
                    {
                        case 0:
                            corsRule.AllowedHeaders = CORSRuleUtil.GetRandomHeaders();
                            break;
                        case 1:
                            corsRule.AllowedMethods = CORSRuleUtil.GetRandomMethods();
                            break;
                        case 2:
                            corsRule.AllowedOrigins = CORSRuleUtil.GetRandomOrigins();
                            break;
                        case 3:
                            corsRule.ExposedHeaders = CORSRuleUtil.GetRandomHeaders();
                            break;
                        case 4:
                            corsRule.MaxAgeInSeconds = random.Next(1, 1000);
                            break;
                    }
                }

                Test.Assert(agent.SetAzureStorageCORSRules(serviceType, newCorsRules),
                    "Set cors rule to {0} service should succeed", serviceType);

                Test.Assert(agent.GetAzureStorageCORSRules(serviceType),
                    "Set cors rule of {0} service should succeed", serviceType);

                PSCorsRule[] actualCORSRules = GetCORSRules();

                CORSRuleUtil.ValidateCORSRules(newCorsRules, actualCORSRules);
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

        [TestMethod]
        [TestCategory(Tag.Function)]    
        [TestCategory(PsTag.ServiceCORS)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceCORS)]
        public void CORSRuleBoundaryTest()
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
        [TestCategory(PsTag.ServiceCORS)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceCORS)]
        public void Set0CORSRuleTest()
        {
            PSCorsRule[] corsRules = new PSCorsRule[0];
            this.ValidateCORSRuleSetGet(corsRules);
        }
        
        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.ServiceCORS)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceCORS)]
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

            // No allowed methods
            corsRules[0].AllowedOrigins = CORSRuleUtil.GetRandomOrigins();
            corsRules[0].AllowedMethods = null;

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Set cors rule without allowed method to {0} service should fail", serviceType);

            ExpectedContainErrorMessage(NoOriginNoMethod0MaxCacheAgeError);

            // Max age in second is negative.
            corsRules[0].AllowedMethods = CORSRuleUtil.GetRandomMethods();
            corsRules[0].MaxAgeInSeconds = -1;

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Set cors rule to {0} service should fail when max age is negative.", serviceType);

            ExpectedContainErrorMessage(NoOriginNoMethod0MaxCacheAgeError);

            // Length of one of allowed origins is greater than 256
            corsRules[0].MaxAgeInSeconds = random.Next(1, 1000);
            corsRules[0].AllowedOrigins = CORSRuleUtil.GetRandomOrigins();
            corsRules[0].AllowedOrigins[0] = Utility.GenNameString("origin", 251);

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Set cors rule to {0} service should fail, when allowed origin length is greater than 256.", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            //Count of allowed origin is more than 64.
            corsRules[0].AllowedOrigins = CORSRuleUtil.GetRandomOrigins(random.Next(65, 100));

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Allowed origins count is greater than 64, set cors rule {0} service should fail", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            // Invalid method name
            string invalidMethodName = Utility.GenNameString("");
            corsRules[0].AllowedOrigins = CORSRuleUtil.GetRandomOrigins();
            corsRules[0].AllowedMethods = CORSRuleUtil.GetRandomMethods();
            corsRules[0].AllowedMethods[0] = invalidMethodName;

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Inalid method name, set cors rule to {0} service should fail", serviceType);
            ExpectedContainErrorMessage(string.Format("'{0}' is an invalid HTTP method", invalidMethodName));

            // More than 2 prefixed allowed headers
            corsRules[0].AllowedMethods = CORSRuleUtil.GetRandomMethods();
            corsRules[0].AllowedHeaders = CORSRuleUtil.GetRandomHeaders(null, random.Next(3, 10));

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "More than 2 prefixed allowed headers, set cors rule to {0} service should fail", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            // More than 64 defined allowed headers
            corsRules[0].AllowedHeaders = CORSRuleUtil.GetRandomHeaders(random.Next(65, 100));

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "More than 64 defined allowed headers, set cors rule to {0} service should fail", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            // Allowed header length greater than 256
            corsRules[0].AllowedHeaders = CORSRuleUtil.GetRandomHeaders();
            corsRules[0].AllowedHeaders[0] = Utility.GenNameString("header", 251);

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Allowed header length greater than 256, set cors rule to {0} service should fail", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            // More than 2 prefixed exposed headers
            corsRules[0].AllowedMethods = CORSRuleUtil.GetRandomMethods();
            corsRules[0].ExposedHeaders = CORSRuleUtil.GetRandomHeaders(null, random.Next(3, 10));

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "More than 2 prefixed exposed headers, set cors rule to {0} service should fail", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            // More than 64 defined exposed headers
            corsRules[0].ExposedHeaders = CORSRuleUtil.GetRandomHeaders(random.Next(65, 100));

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "More than 64 defined exposed headers, set cors rule to {0} service should fail", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            // Exposed header length greater than 256
            corsRules[0].ExposedHeaders = CORSRuleUtil.GetRandomHeaders();
            corsRules[0].ExposedHeaders[0] = Utility.GenNameString("header", 251);

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Exposed header length greater than 256, set cors rule to {0} service should fail", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            // big total size
            corsRules[0].AllowedOrigins = CORSRuleUtil.GetRandomOrigins(null, true);
            corsRules[0].AllowedHeaders = CORSRuleUtil.GetRandomHeaders(null, null, true);
            corsRules[0].ExposedHeaders = CORSRuleUtil.GetRandomHeaders(null, null, true);

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Exposed header length greater than 256, set cors rule to {0} service should fail", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            // 6 CORS ruls
            corsRules = CORSRuleUtil.GetRandomValidCORSRules(6);

            serviceType = GetRandomServiceType();
            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "6 CORS rules, set cors rule to {0} service should fail", serviceType);
            ExpectedContainErrorMessage(CORSRuleInvalidError);

            // Invalid Service Type
            corsRules = CORSRuleUtil.GetRandomValidCORSRules(random.Next(1, 5));

            Test.Assert(!agent.SetAzureStorageCORSRules(Constants.ServiceType.InvalidService, corsRules), "Set cors rules to invalid service type should fail.");
            ExpectedContainErrorMessage("Unable to match the identifier name InValid to a valid enumerator name.  Specify one of the following enumerator names and try again: Blob, Table, Queue, File");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void GetCORSRulesNegativeTest()
        {
            Test.Assert(!agent.GetAzureStorageCORSRules(Constants.ServiceType.InvalidService), "Get CORS rules of invalid service type should fail.");
            ExpectedContainErrorMessage("Unable to match the identifier name InValid to a valid enumerator name.  Specify one of the following enumerator names and try again: Blob, Table, Queue, File");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void RemoveCORSRulesNegativeTest()
        {
            Test.Assert(!agent.RemoveAzureStorageCORSRules(Constants.ServiceType.InvalidService), "Remove CORS rules of invalid service type should fail.");
            ExpectedContainErrorMessage("Unable to match the identifier name InValid to a valid enumerator name.  Specify one of the following enumerator names and try again: Blob, Table, Queue, File");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void RemoveCORSRulesTest()
        {
            // No CORS rule exist
            Constants.ServiceType serviceType = GetRandomServiceType();

            ServiceProperties serviceProperties = new ServiceProperties();
            serviceProperties.Clean();
            serviceProperties.Cors = new CorsProperties();
            serviceProperties.Cors.CorsRules.Clear();

            this.SetSerivceProperties(serviceType, serviceProperties);
            this.ValidateRemoveCORSRule(serviceType);
            
            // Set CORS rules with cmdlet
            serviceType = GetRandomServiceType();
            PSCorsRule[] corsRules = CORSRuleUtil.GetRandomValidCORSRules(random.Next(1, 5));
            Test.Assert(agent.SetAzureStorageCORSRules(serviceType, corsRules),
                "Set CORS rules to {0} service should succeed.");
            this.ValidateRemoveCORSRule(serviceType);
        }

        private void ValidateRemoveCORSRule(Constants.ServiceType serviceType)
        {
            Test.Assert(agent.RemoveAzureStorageCORSRules(serviceType), "Remove CORS rules of {0} service should succeed.", serviceType);

            Test.Assert(agent.GetAzureStorageCORSRules(serviceType), "Get CORS rules of {0} service should succeed.", serviceType);

            PSCorsRule[] actualCORSRule = (PSCorsRule[])agent.Output[0]["_basicobject"];

            Test.Assert(0 == actualCORSRule.Length, "There should be 0 CORS rule after removing. Actually there are {0} CORS rule(s)", actualCORSRule.Length);
        }


        private void ValidateCORSRuleSetGet(PSCorsRule[] corsRules)
        {
            Constants.ServiceType serviceType = GetRandomServiceType();

            try
            {

                Test.Assert(agent.SetAzureStorageCORSRules(serviceType, corsRules), "Set cors rule with 64 allowed origins to {0} service should succeed", serviceType);

                Test.Assert(agent.GetAzureStorageCORSRules(serviceType), "Get CORS rules of {0} service should succeed", serviceType);

                CORSRuleUtil.ValidateCORSRules(corsRules, GetCORSRules());
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

        private Constants.ServiceType GetRandomServiceType()
        {
            var serviceTypes = Enum.GetValues(typeof(Constants.ServiceType));

            return (Constants.ServiceType)serviceTypes.GetValue(random.Next(0, serviceTypes.Length - 1));
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

                PSCorsRule[] acturalRules = GetCORSRules();

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

        private PSCorsRule[] GetCORSRules()
        {
            if (lang == Language.PowerShell)
            {
                return agent.Output[0]["_baseObject"] as PSCorsRule[];
            }
            else
            {
                List<PSCorsRule> rules = new List<PSCorsRule>();
                for (int i = 0; i < agent.Output.Count; i++)
                {
                    PSCorsRule rule = new PSCorsRule();

                    bool hasValue = false;
                    JArray categories;
                    string key = "AllowedMethods";
                    if (agent.Output[i].ContainsKey(key))
                    {
                        categories = (JArray)agent.Output[i][key];
                        rule.AllowedMethods = categories.Select(c => (string)c).ToArray();
                        hasValue = true;
                    }

                    key = "AllowedOrigins";
                    if (agent.Output[i].ContainsKey(key))
                    {
                        categories = (JArray)agent.Output[i][key];
                        rule.AllowedOrigins = categories.Select(c => (string)c).ToArray();
                        hasValue = true;
                    }

                    key = "AllowedHeaders";
                    if (agent.Output[i].ContainsKey(key))
                    {  
                        categories = (JArray)agent.Output[i][key];
                        rule.AllowedHeaders = categories.Select(c => (string)c).ToArray();
                        hasValue = true;
                    }

                    key = "ExposedHeaders"; 
                    if (agent.Output[i].ContainsKey(key))
                    {
                        categories = (JArray)agent.Output[i][key];
                        rule.ExposedHeaders = categories.Select(c => (string)c).ToArray();
                        hasValue = true;
                    }

                    key = "MaxAgeInSeconds";
                    if (agent.Output[i].ContainsKey(key))
                    {
                        rule.MaxAgeInSeconds = (int)(agent.Output[i][key] as long?);
                        hasValue = true;
                    }

                    if (hasValue)
                    {
                        rules.Add(rule);
                    }
                }

                return rules.ToArray();
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
