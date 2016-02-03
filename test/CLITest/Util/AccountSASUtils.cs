namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using MS.Test.Common.MsTestLib;

    class AccountSASUtils
    {
        static readonly string[] sasOptions = { "ss", "srt", "sp", "st", "se", "sip", "spr", "sig", "sv"};
        static readonly string[] sasRequiredOptions = { "ss", "srt", "sp", "se", "sig", "sv"};
        public static readonly string fullPermission = "racwdlup";

        public static void ValidateAccountSAS(
            SharedAccessAccountServices service, 
            SharedAccessAccountResourceTypes resourceType, 
            string permission, 
            SharedAccessProtocol? protocol, 
            string iPAddressOrRange,
            DateTime? startTime, 
            DateTime? expiryTime,
            string sasToken)
        {
            Test.Assert(sasToken.StartsWith("?"), "Sas token must be a query string.");
            string[] sasSegs = sasToken.Substring(1).Split('&');

            var sasRequiredOptionList = sasRequiredOptions.ToList();
            var sasOptionList = sasOptions.ToList();

            if (service == SharedAccessAccountServices.None)
                sasRequiredOptionList.Remove("ss");
            if (resourceType == SharedAccessAccountResourceTypes.None)
                sasRequiredOptionList.Remove("srt");

            foreach (string sasSegment in sasSegs)
            {
                string[] queryPair = sasSegment.Split('=');
                Test.Assert(queryPair.Length == 2, "One segment should be a key value pair: {0}", sasSegment);

                sasRequiredOptionList.Remove(queryPair[0]);
                sasOptionList.Remove(queryPair[0]);

                switch (queryPair[0])
                { 
                    case "ss":
                        ValidateSASService(service, queryPair[1]);
                        break;
                    case "srt":
                        ValidateResourceType(resourceType, queryPair[1]);
                        break;
                    case "sp":
                        ValidatePermissions(permission, queryPair[1]);
                        break;
                    case "sip":
                        ValidateIpRange(iPAddressOrRange, queryPair[1]);
                        break;
                    case "spr":
                        ValidateProtocol(protocol, queryPair[1]);
                        break;
                    case "st":
                        ValidateStartTime(startTime, queryPair[1]);
                        break;
                    case "se":
                        ValidateExpiryTime(expiryTime, queryPair[1]);
                        break;
                }
            }

            Test.Assert(0 == sasRequiredOptionList.Count, "All required options should exist.");
            if (0 != sasRequiredOptionList.Count)
                Test.Info("Not exist required options: " + sasRequiredOptionList.First());

            if (string.IsNullOrEmpty(iPAddressOrRange))
            { 
                Test.Assert(sasOptionList.Contains("sip"), "IPACL option should be null.");
            }
            else if (null == startTime)
            {
                Test.Assert(sasOptionList.Contains("st"), "StartTime option should be null.");
            }
        }

        private static void ValidateSASService(
            SharedAccessAccountServices expectedService,
            string sasService)
        {
            string serviceString = SharedAccessAccountPolicy.ServicesToString(expectedService);
            Test.Assert(string.Equals(sasService, serviceString), "Service: {0} == {1}", serviceString, sasService);
        }

        private static void ValidateResourceType(
            SharedAccessAccountResourceTypes expectedResourceType,
            string sasResourceType)
        {
            string resourceTypeString = SharedAccessAccountPolicy.ResourceTypesToString(expectedResourceType);
            Test.Assert(string.Equals(sasResourceType, resourceTypeString), "Resource type: {0} == {1}", resourceTypeString, sasResourceType);
        }

        private static void ValidatePermissions(
            string expectedPermissions,
            string permissions)
        {
            var expectedPerChars = expectedPermissions.ToCharArray().ToList();
            var perChars = permissions.ToCharArray().ToList();

            foreach (var perChar in perChars)
            {
                Test.Assert(expectedPerChars.Remove(perChar), "Permissions {0} should be expected.", perChar);

                //Remove the dup permission in inout parameter
                while (expectedPerChars.Contains(perChar)) 
                    expectedPerChars.Remove(perChar);
            }

            Test.Assert(0 == expectedPerChars.Count, "All expected permissions should exist.");
        }

        private static void ValidateIpRange(
            string expectedIPAddressOrRange,
            string IPAddressOrRange)
        {
            if (string.IsNullOrEmpty(expectedIPAddressOrRange))
            {
                Test.Error("IPACL should not exist in the SAS token");
                return;
            }

            Test.Assert(string.Equals(expectedIPAddressOrRange, IPAddressOrRange), "IPACL: {0} == {1}", expectedIPAddressOrRange, IPAddressOrRange);
        }

        private static void ValidateProtocol(
            SharedAccessProtocol? expectedProtocol,
            string protocol)
        {
            string protocolString = "https,http";
            if (null != expectedProtocol && SharedAccessProtocol.HttpsOrHttp != expectedProtocol)
            {
                protocolString = "https";
            }

            Test.Assert(string.Equals(protocol.Replace("%2C", ","), protocolString), "Protocol: {0} == {1}", protocolString, protocol);
        }

        private static void ValidateStartTime(
            DateTime? expectedStartTime,
            string startTime)
        {
            if (null == expectedStartTime)
            {
                Test.Error("Start time option should not exist in SAS token.");
                return;
            }

            DateTime dateTime;
            Test.Assert(DateTime.TryParse(startTime.Replace("%3A", ":"), out dateTime), "The start time option should be an available time.");

            Test.Assert((expectedStartTime - dateTime < new TimeSpan(0, 0, 1)) && (dateTime - expectedStartTime < new TimeSpan(0, 0, 1)), "Start time value should be expected.");
        }

        private static void ValidateExpiryTime(
            DateTime? expectedExpiryTime,
            string expiryTime)
        {
            DateTime dateTime;
            Test.Assert(DateTime.TryParse(expiryTime.Replace("%3A", ":"), out dateTime), "The expiry time option should be an available time.");

            if (null == expectedExpiryTime)
            {
                expectedExpiryTime = DateTime.Now.AddHours(1);
                Test.Assert(dateTime < expectedExpiryTime && dateTime.AddSeconds(10) >= expectedExpiryTime, "Expiry time value should be expected.");
                return;
            }

            Test.Assert((expectedExpiryTime - dateTime < new TimeSpan(0, 0, 1)) && (dateTime - expectedExpiryTime < new TimeSpan(0, 0, 1)), "Expiry time value should be expected.");
        
        }
    }
}
