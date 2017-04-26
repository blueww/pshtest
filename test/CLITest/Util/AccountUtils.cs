namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Commands.Common.Authentication.Models;
    using Microsoft.Azure.Management.Storage.Models;
    using Microsoft.Rest.Azure;
    using Microsoft.WindowsAzure.Management.Storage;
    using MS.Test.Common.MsTestLib;
    using SRPManagement = Microsoft.Azure.Management.Storage;
    using SRPModel = Microsoft.Azure.Management.Storage.Models;

    public class AccountUtils
    {
        private static string[] ForbiddenWordsInAccountName = { "msn", "fuck", "shit", "cunt", "cum", "nigger", "kkk", "pedo", "bid", "xxx" };
        private static Tuple<int, int> ValidNameRange = new Tuple<int, int>((int)'a', (int)'z');
        private static Random random = new Random();

        private SRPManagement.StorageManagementClient srpStorageClient;
        private DateTime srpStorageClientLastUpdatedTime;


        public SRPManagement.StorageManagementClient SRPStorageClient
        {
            get
            {
                if (srpStorageClient != null && DateTime.Now - srpStorageClientLastUpdatedTime <= TimeSpan.FromMinutes(30))
                {
                    return srpStorageClient;
                }

                var tempSrpStorageClient = new SRPManagement.StorageManagementClient(Utility.GetTokenCredential())
                {
                    SubscriptionId = Test.Data.Get("AzureSubscriptionID")
                };

                Interlocked.Exchange(ref srpStorageClient, tempSrpStorageClient);
                srpStorageClientLastUpdatedTime = DateTime.Now;

                return srpStorageClient;
            }
        }

        public StorageManagementClient StorageClient { get; private set; }

        private Language language = Language.PowerShell;

        public AccountUtils(Language language, bool isResourceMode)
        {
            this.language = language;
            if (isResourceMode)
            {
                StorageClient = new StorageManagementClient(Utility.GetCertificateCloudCredential());
            }
            else
            {
                AzureEnvironment environment = Utility.GetTargetEnvironment();
                StorageClient = new StorageManagementClient(Utility.GetCertificateCloudCredential(),
                    environment.GetEndpointAsUri(AzureEnvironment.Endpoint.ServiceManagement));
            }
        }

        public string GenerateAccountName(int nameLength = 0)
        {
            string name = string.Empty;

            while (true)
            {
                name = GenerateAvailableAccountName(nameLength);
                if (StorageClient.StorageAccounts.CheckNameAvailability(name).IsAvailable)
                {
                    break;
                }
            };

            return  name;
        }

        public string GenerateResourceGroupName()
        {
            return GenerateAvailableAccountName();
        }

        public string GenerateAccountLocation(string type, bool isResourceMode, bool isMooncake)
        {
            if (type == this.mapAccountType(Constants.AccountType.Premium_LRS))
            {
                if (isResourceMode)
                {
                    throw new InvalidOperationException("SRP does not support Premium_LRS yet");
                }

                return Constants.Location.WestUS;
            }
            else
            {
                if (isMooncake)
                {
                    return Constants.MCLocations[random.Next(0, Constants.MCLocations.Length)];
                }
                else if (isResourceMode)
                {
                    return Constants.SRPLocations[random.Next(0, Constants.SRPLocations.Length)]; 
                }
                else
                {
                    return Constants.Locations[random.Next(0, Constants.Locations.Length)];
                }
            }
        }

        public string GenerateAccountType(bool isResourceMode, bool isMooncake)
        {
            string accountType = null;
            do
            {
                accountType = Constants.AccountTypes[random.Next(0, Constants.AccountTypes.Length)];
            }
            while ((isResourceMode && accountType.Equals(Constants.AccountType.Premium_LRS, StringComparison.InvariantCultureIgnoreCase)) ||
                (isMooncake && (accountType.Equals(Constants.AccountType.Premium_LRS, StringComparison.InvariantCultureIgnoreCase) ||
                accountType.Equals(Constants.AccountType.Standard_ZRS, StringComparison.InvariantCultureIgnoreCase))));

            return accountType;
        }

        public string mapAccountType(string type)
        {
            if (this.language == Language.NodeJS)
            {
                switch (type)
                {
                    case Constants.AccountType.Standard_LRS:
                        return "LRS";
                    case Constants.AccountType.Standard_ZRS:
                        return "ZRS";
                    case Constants.AccountType.Standard_GRS:
                        return "GRS";
                    case Constants.AccountType.Standard_RAGRS:
                        return "RAGRS";
                    case Constants.AccountType.Premium_LRS:
                        return "PLRS";
                }
            }

            return type;
        }

        public void ValidateSRPAccount(string resourceGroupName, 
            string accountName, 
            string location,       
            string skuName,
            Hashtable[] tags = null,
            Kind kind  = Kind.Storage,
            AccessTier? accessTier = null,
            string customDomain = null,
            bool? useSubdomain = null,
            Constants.EncryptionSupportServiceEnum? enableEncryptionService = null,
            bool? enableHttpsTrafficOnly = null)
        {

            AzureOperationResponse<SRPModel.StorageAccount> response = this.SRPStorageClient.StorageAccounts.GetPropertiesWithHttpMessagesAsync(resourceGroupName, accountName).Result;
            Test.Assert(response.Response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            SRPModel.StorageAccount account = response.Body;
            Test.Assert(accountName == account.Name, string.Format("Expected account name is {0} and actually it is {1}", accountName, account.Name));

            Test.Assert(this.mapAccountType(Constants.AccountTypes[(int)account.Sku.Name]).Equals(skuName),
                string.Format("Expected account type is {0} and actually it is {1}", skuName, account.Sku.Name));

            if (!string.IsNullOrEmpty(location))
            {
                Test.Assert(location.Replace(" ", "").ToLower() == account.Location, string.Format("Expected location is {0} and actually it is {1}", location, account.Location));
            }
            Test.Assert(kind == account.Kind, string.Format("Kind should match: {0} == {1}", kind, account.Kind));
            Test.Assert(accessTier == account.AccessTier, string.Format("AccessTier should match: {0} == {1}", accessTier, account.AccessTier));
            if (customDomain == null)
            {
                Test.Assert(account.CustomDomain == null, string.Format("CustomDomain should match: {0} == {1}", customDomain, account.CustomDomain));
            }
            else
            {
                Test.Assert(customDomain == account.CustomDomain.Name, string.Format("CustomDomain should match: {0} == {1}", customDomain, account.CustomDomain.Name));

                // UseSubDomain is only for set, and won't be return in get
                Test.Assert(account.CustomDomain.UseSubDomain == null, string.Format("UseSubDomain should match: {0} == {1}", customDomain, account.CustomDomain.UseSubDomain));
            }
            if (enableHttpsTrafficOnly != null)
            {
                Test.Assert(enableHttpsTrafficOnly == account.EnableHttpsTrafficOnly, string.Format("EnableHttpsTrafficOnly should match: {0} == {1}", enableHttpsTrafficOnly, account.EnableHttpsTrafficOnly));
            }
    
            this.ValidateTags(tags, account.Tags);
            ValidateServiceEncrption(account.Encryption, enableEncryptionService);
        }
        
        public void ValidateTags(Hashtable[] originTags, IDictionary<string, string> targetTags)
        {
            if (null == originTags || 0 == originTags.Length)
            {
                Test.Assert(0 == targetTags.Count, "Should be no tags got set.");
                return;
            }

            foreach (var sourceTag in originTags[0].Keys)
            {
                string tagValue = null;
                Test.Assert(targetTags.TryGetValue(sourceTag.ToString(), out tagValue),
                    "Tag {0} should exist", sourceTag);
                Test.Assert(string.Equals(tagValue, originTags[0][sourceTag].ToString()),
                    "Tag value should be the same. Expect: {0}, actual is: {1}", originTags[0][sourceTag].ToString(), tagValue);
            }
        }
        public void ValidateServiceEncrption(Encryption accountEncryption, Constants.EncryptionSupportServiceEnum? enableEncryptionService)
        {
            if (enableEncryptionService == null || enableEncryptionService == Constants.EncryptionSupportServiceEnum.None)
            {
                Test.Assert(accountEncryption == null
                    || accountEncryption.Services == null
                    || (accountEncryption.Services.Blob == null && accountEncryption.Services.File == null)
                    || (accountEncryption.Services.Blob.Enabled == null && accountEncryption.Services.File.Enabled == null)
                    || (accountEncryption.Services.Blob.Enabled.Value == false && accountEncryption.Services.File.Enabled.Value == false), "The Blob and File Encrption should both be disabled.");
            }
            else
            {
                //Check Blob Encryption
                if ((enableEncryptionService & Constants.EncryptionSupportServiceEnum.Blob) == Constants.EncryptionSupportServiceEnum.Blob)
                {
                    Test.Assert(accountEncryption.Services.Blob.Enabled.Value == true, "The Blob Encrption should be enabled.");
                }
                else
                {
                    Test.Assert(accountEncryption.Services.Blob == null
                        || accountEncryption.Services.Blob.Enabled == null
                        || accountEncryption.Services.Blob.Enabled.Value == false, "The Blob Encrption should be disabled.");
                }

                //Check File Encryption
                if ((enableEncryptionService & Constants.EncryptionSupportServiceEnum.File) == Constants.EncryptionSupportServiceEnum.File)
                {
                    Test.Assert(accountEncryption.Services.File.Enabled.Value == true, "The File Encrption should be enabled.");
                }
                else
                {
                    Test.Assert(accountEncryption.Services.File == null
                        || accountEncryption.Services.File.Enabled == null
                        || accountEncryption.Services.File.Enabled.Value == false, "The File Encrption should be disabled.");
                }
            }
        }

        public static string GenerateAvailableAccountName(int nameLength = 0)
        { 
            bool regenerate = false;
            string name = string.Empty;

            do
            {
                regenerate = false;
                if (0 == nameLength)
                {
                    name = "clitest" + FileNamingGenerator.GenerateNameFromRange(random.Next(10, 18), ValidNameRange);
                }
                else if (nameLength >= 17)
                {
                    name = "clitest" + FileNamingGenerator.GenerateNameFromRange(nameLength - 7, ValidNameRange);
                }
                else 
                {
                    name = FileNamingGenerator.GenerateNameFromRange(nameLength, ValidNameRange);
                }

                foreach (string forbiddenWord in ForbiddenWordsInAccountName)
                {
                    if (name.Contains(forbiddenWord))
                    {
                        regenerate = true;
                    }
                }
            }
            while (regenerate);

            return name;
        }

        public class CheckNameAvailabilityResponse
        {
            public bool? NameAvailable { get; set; }

            public Reason? Reason { get; set; }

            public string Message { get; set; }

            public HttpStatusCode? StatusCode { get; set; }

            public string RequestId  { get; set; }

            public static CheckNameAvailabilityResponse Create(Dictionary<string, object> output, bool isResourceMode)
            {  
                CheckNameAvailabilityResponse response = new CheckNameAvailabilityResponse();
                response.NameAvailable = Utility.ParseBoolFromJsonOutput(output, "nameAvailable");
                response.StatusCode = Utility.ParseEnumFromJsonOutput<HttpStatusCode>(output, "statusCode");
                if (isResourceMode)
                {
                    response.Message = Utility.ParseStringFromJsonOutput(output, "message");
                    response.Reason = Utility.ParseEnumFromJsonOutput<Reason>(output, "reason");
                }
                else
                {
                    response.Message = Utility.ParseStringFromJsonOutput(output, "reason");
                }

                return response;
            }

            public static CheckNameAvailabilityResponse Create(CheckNameAvailabilityResult rawResponse)
            {
                CheckNameAvailabilityResponse response = new CheckNameAvailabilityResponse();
                response.NameAvailable = rawResponse.NameAvailable;
                response.Message = rawResponse.Message;
                response.Reason = rawResponse.Reason;

                return response;
            }
        }
    }
}
