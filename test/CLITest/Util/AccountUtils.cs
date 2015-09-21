namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Common.Authentication;
    using Microsoft.WindowsAzure.Management.Storage;
    using MS.Test.Common.MsTestLib;
    using SRPManagement = Microsoft.Azure.Management.Storage;
    using SRPModel = Microsoft.Azure.Management.Storage.Models;

    public class AccountUtils
    {
        private static string[] ForbiddenWordsInAccountName = { "msn", "fuck", "shit", "cunt", "cum", "nigger", "kkk", "pedo", "bid", "xxx" };
        private static Tuple<int, int> ValidNameRange = new Tuple<int, int>((int)'a', (int)'z');
        private static Random random = new Random();

        public SRPManagement.StorageManagementClient SRPStorageClient
        {
            get;
            private set;
        }

        public StorageManagementClient StorageClient
        {
            get;
            private set;
        }

        private Language language = Language.PowerShell;

        public AccountUtils(Language language, bool isResourceMode)
        {
            if (isResourceMode)
            {
                StorageClient = AzureSession.ClientFactory.CreateClient<StorageManagementClient>(
                    Utility.GetProfile().Context,
                    Microsoft.Azure.Common.Authentication.Models.AzureEnvironment.Endpoint.ServiceManagement);

                SRPStorageClient = AzureSession.ClientFactory.CreateClient<SRPManagement.StorageManagementClient>(
                    Utility.GetProfile().Context,
                    Microsoft.Azure.Common.Authentication.Models.AzureEnvironment.Endpoint.ResourceManager);
            }
            else
            {
                StorageClient = new StorageManagementClient(Utility.GetCertificateCloudCredential());
            }

            this.language = language;
        }

        public string GenerateAccountName()
        {
            string name = string.Empty;

            while (true)
            {
                name = this.GenerateAvailableAccountName();
                try
                {
                    StorageClient.StorageAccounts.Get(name);
                }
                catch (Exception)
                {
                    break;
                }
            };

            return  name;
        }

        public string GenerateResourceGroupName()
        {
            return this.GenerateAvailableAccountName();
        }

        public string GenerateAccountLocation(string type, bool isResourceMode)
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
                if (isResourceMode)
                {
                    return Constants.SRPLocations[random.Next(0, Constants.SRPLocations.Length)];
                }
                else
                {
                    return Constants.Locations[random.Next(0, Constants.Locations.Length)];
                }

            }
        }

        public string GenerateAccountType(bool isResourceMode)
        {
            string accountType = null;
            do
            {
                accountType = Constants.AccountTypes[random.Next(0, Constants.AccountTypes.Length)];
            }
            while (isResourceMode && accountType.Equals(Constants.AccountType.Premium_LRS, StringComparison.InvariantCultureIgnoreCase));

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

        public void ValidateSRPAccount(string resourceGroupName, string accountName, string location, string accountType, Hashtable[] tags = null)
        {
            SRPModel.StorageAccountGetPropertiesResponse response = this.SRPStorageClient.StorageAccounts.GetPropertiesAsync(resourceGroupName, accountName, CancellationToken.None).Result;
            Test.Assert(response.StatusCode == HttpStatusCode.OK, string.Format("Account {0} should be created successfully.", accountName));

            SRPModel.StorageAccount account = response.StorageAccount;
            Test.Assert(accountName == account.Name, string.Format("Expected account name is {0} and actually it is {1}", accountName, account.Name));

            Test.Assert(this.mapAccountType(Constants.AccountTypes[(int)account.AccountType]).Equals(accountType),
                string.Format("Expected account type is {0} and actually it is {1}", accountType, account.AccountType));

            if (!string.IsNullOrEmpty(location))
            {
                Test.Assert(location == account.Location, string.Format("Expected location is {0} and actually it is {1}", location, account.Location));
            }

            this.ValidateTags(tags, account.Tags);
        }

        public void ValidateTags(Hashtable[] originTags, IDictionary<string, string> targetTags)
        {
            if (null == originTags || 0 == originTags.Length)
            {
                Test.Assert(0 == targetTags.Count, "Should be no tags got set.");
                return;
            }

            foreach (var sourceTag in originTags)
            {
                string tagValue = null;
                Test.Assert(targetTags.TryGetValue(sourceTag["Name"].ToString(), out tagValue), "Tag {0} should exist", sourceTag["Name"]);
                Test.Assert(string.Equals(tagValue, sourceTag["Value"].ToString()), "Tag value should be the same.");
            }
        }

        private string GenerateAvailableAccountName()
        { 
            bool regenerate = false;
            string name = string.Empty;

            do
            {
                regenerate = false;
                name = "clitest" + FileNamingGenerator.GenerateNameFromRange(random.Next(10, 18), ValidNameRange);

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
    }
}
