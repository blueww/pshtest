namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Common.Authentication;
    using Microsoft.Azure.Common.Authentication.Models;
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
                    Utility.GetProfile(),
                    Microsoft.Azure.Common.Authentication.Models.AzureEnvironment.Endpoint.ServiceManagement);

                SRPStorageClient = AzureSession.ClientFactory.CreateClient<SRPManagement.StorageManagementClient>(
                    Utility.GetProfile(),
                    Microsoft.Azure.Common.Authentication.Models.AzureEnvironment.Endpoint.ResourceManager);
            }
            else
            {
                AzureEnvironment environment = Utility.GetTargetEnvironment();
                StorageClient = new StorageManagementClient(Utility.GetCertificateCloudCredential(),
                    environment.GetEndpointAsUri(AzureEnvironment.Endpoint.ServiceManagement));
            }

            this.language = language;
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

        public void ValidateSRPAccount(string resourceGroupName, string accountName, string location, string accountType)
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
    }
}
