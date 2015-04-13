namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using Microsoft.WindowsAzure.Management.Storage;
    using SRPManagement = Microsoft.Azure.Management.Storage;

    public class AccountUtils
    {
        private static string[] ForbiddenWordsInAccountName = { "msn", "fuck", "shit", "cunt", "cum", "nigger", "kkk" };
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
            StorageClient = new StorageManagementClient(Utility.GetTokenCloudCredential("https://management.core.windows.net/"));
            SRPStorageClient = new SRPManagement.StorageManagementClient(Utility.GetTokenCloudCredential());
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

        public string GenerateAccountLocation(string type)
        {
            if (type == this.mapAccountType(Constants.AccountType.Premium_LRS))
            {
                return Constants.Location.WestUS;
            }
            else
            {
                return Constants.Locations[random.Next(0, Constants.Locations.Length)];
            }
        }

        public string GenerateAccountType()
        {
            return Constants.AccountTypes[random.Next(0, Constants.AccountTypes.Length)];
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
