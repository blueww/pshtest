using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Commands.Storage.Model.ResourceModel;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using MS.Test.Common.MsTestLib;

namespace Management.Storage.ScenarioTest.Util
{
    public static class CORSRuleUtil
    {
        private static Random rd = new Random();

        public static PSCorsRule[] GetRandomValidCORSRules(int count)
        {
            PSCorsRule[] rule = new PSCorsRule[count];

            for (int i = 0; i < count; ++i)
            {
                rule[i] = GetRandomValidCORSRule();
            }

            return rule;
        }

        public static PSCorsRule GetRandomValidCORSRule()
        {
            PSCorsRule corsRule = new PSCorsRule();
            corsRule.AllowedHeaders = GetRandomHeaders();
            corsRule.ExposedHeaders = GetRandomHeaders();
            corsRule.AllowedOrigins = GetRandomOrigins();
            corsRule.AllowedMethods = GetRandomMethods();
            corsRule.MaxAgeInSeconds = rd.Next(1, 1000);

            return corsRule;
        }

        public static string[] GetRandomHeaders(int? definedHeaderCount = null, int? prefixedHeaderCount = null, bool largeHeaders = false)
        {
            if (!prefixedHeaderCount.HasValue)
            {
                prefixedHeaderCount = rd.Next(0, 3);
            }

            if (!definedHeaderCount.HasValue)
            {
                definedHeaderCount = rd.Next(largeHeaders? 3: 1, largeHeaders ? 65 - prefixedHeaderCount.Value : 3);
            }

            string[] headers = new string[prefixedHeaderCount.Value + definedHeaderCount.Value];
            int headerMaxLength = 12;

            if (largeHeaders)
            {
                headerMaxLength = 256;
            }

            for (int i = 0; i < prefixedHeaderCount.Value; ++i)
            {
                headers[i] = Utility.GenNameString("", rd.Next(1, headerMaxLength)) + "*";
            }

            for (int i = 0; i < definedHeaderCount.Value; ++i)
            {
                headers[i + prefixedHeaderCount.Value] = Utility.GenNameString("", rd.Next(1, headerMaxLength));
            }

            return headers;
        }

        public static string[] GetRandomOrigins(int? originCount = null, bool largeOrigins = false)
        {
            if (!originCount.HasValue)
            {
                originCount = rd.Next(largeOrigins ? 10 : 1, largeOrigins ? 64 : 5);
            }

            int headerMaxLength = 12;

            if (largeOrigins)
            {
                headerMaxLength = 256;
            }

            string[] origins = new string[originCount.Value];

            for (int i = 0; i < originCount.Value; ++i)
            {
                if (rd.Next(0, 3) == 0)
                {
                    origins[i] = Utility.GenNameString("*", rd.Next(1, headerMaxLength));
                }
                else
                {
                    origins[i] = Utility.GenNameString("", rd.Next(1, headerMaxLength));
                }
            }

            return origins;
        }

        public static string[] GetRandomMethods()
        {
            CorsHttpMethods methods = (CorsHttpMethods)rd.Next(1, 512);

            List<string> methodList = new List<string>();

            foreach (CorsHttpMethods methodValue in Enum.GetValues(typeof(CorsHttpMethods)).Cast<CorsHttpMethods>())
            {
                if (methodValue != CorsHttpMethods.None && (methods & methodValue) != 0)
                {
                    methodList.Add(methodValue.ToString());
                }
            }

            return methodList.ToArray();
        }

        /// <summary>
        /// Clean all the settings on the ServiceProperties project
        /// </summary>
        /// <param name="serviceProperties">Service properties</param>
        internal static void Clean(this ServiceProperties serviceProperties)
        {
            serviceProperties.Logging = null;
            serviceProperties.HourMetrics = null;
            serviceProperties.MinuteMetrics = null;
            serviceProperties.Cors = null;
            serviceProperties.DefaultServiceVersion = null;
        }

        internal static void ValidateCORSRules(PSCorsRule[] expectedRules, PSCorsRule[] acturalRules)
        {
            Test.Info("Validating CORS rules........");

            if (expectedRules.Length != acturalRules.Length)
            {
                Test.Error("The actual rules length are different with expected: {0} == {1}", expectedRules.Length, acturalRules.Length);
                return;
            }

            for (int i = 0; i < expectedRules.Length; ++i)
            {
                if (!ValidateStrings(expectedRules[i].AllowedHeaders, acturalRules[i].AllowedHeaders, false))
                {
                    Test.Error("AllowedHeaders of actural rule is not the expected.");
                    return;
                }

                if (!ValidateStrings(expectedRules[i].AllowedMethods, acturalRules[i].AllowedMethods, true))
                {
                    Test.Error("AllowedMethods of actural rule is not the expected.");
                    return;
                }

                if (!ValidateStrings(expectedRules[i].AllowedOrigins, acturalRules[i].AllowedOrigins, false))
                {
                    Test.Error("AllowedOrigins of actural rule is not the expected.");
                    return;
                }

                if (!ValidateStrings(expectedRules[i].ExposedHeaders, acturalRules[i].ExposedHeaders, false))
                {
                    Test.Error("ExposedHeaders of actural rule is not the expected.");
                    return;
                }

                if (expectedRules[i].MaxAgeInSeconds != acturalRules[i].MaxAgeInSeconds)
                { 
                    Test.Error("MaxAgeInSeconds is not the expected.");
                    return;
                }
            }
        }

        private static bool ValidateStrings(string[] expectedStrings, string[] acturalStrings, bool ignoreCase)
        {
            if (expectedStrings.Length != acturalStrings.Length)
            {
                return false;
            }

            for (int i = 0; i < expectedStrings.Length; ++i)
            {
                if (!acturalStrings.Contains(expectedStrings[i], ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal))
                {
                    return false;
                }
            }

            for (int i = 0; i < acturalStrings.Length; ++i)
            {
                if (!expectedStrings.Contains(acturalStrings[i], ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
