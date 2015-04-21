namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using Microsoft.Azure.Common.Authentication;
    using Microsoft.WindowsAzure.Management.Storage;
    using SRPManagement = Microsoft.Azure.Management.Storage;

    public class AccountUtils
    {
        private static string[] ForbiddenWordsInAccountName = { "msn", "fuck", "shit", "cunt", "cum", "nigger", "kkk", "pedo" };
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

        public AccountUtils(Language language)
        {
            StorageClient = AzureSession.ClientFactory.CreateClient<StorageManagementClient>(
                Utility.GetProfile(),
                Microsoft.Azure.Common.Authentication.Models.AzureEnvironment.Endpoint.ServiceManagement);

            SRPStorageClient = AzureSession.ClientFactory.CreateClient<SRPManagement.StorageManagementClient>(
                Utility.GetProfile(),
                Microsoft.Azure.Common.Authentication.Models.AzureEnvironment.Endpoint.ResourceManager);

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

        private string GenerateAvailableAccountName()
        { 
            bool regenerate = false;
            string name = string.Empty;

            do
            {
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
