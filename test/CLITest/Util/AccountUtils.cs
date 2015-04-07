namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Management.Storage;
    using Microsoft.WindowsAzure.Management.Storage.Models;
    using MS.Test.Common.MsTestLib;
    using SRPCredentials = Microsoft.Azure;
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
            string certFile = Test.Data.Get("ManagementCert");
            string certPassword = Test.Data.Get("CertPassword");
            X509Certificate2 cert = new X509Certificate2(certFile, certPassword);
            CertificateCloudCredentials credetial = new CertificateCloudCredentials(Test.Data.Get("AzureSubscriptionID"), cert);
            SRPCredentials.CertificateCloudCredentials srpCredentials = new SRPCredentials.CertificateCloudCredentials(Test.Data.Get("AzureSubscriptionID"), cert);
            SRPStorageClient = new SRPManagement.StorageManagementClient(srpCredentials);
            StorageClient = new StorageManagementClient(credetial);
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

        public string GenerateAndValidateNonExsitingAccountName()
        {
            string accountName = string.Empty;
            bool validated = false;
            while (!validated)
            {
                accountName = this.GenerateAccountName();
                StorageAccountGetResponse response;
                try
                {
                    // Use service management client to check the existing account for a global search
                    response = this.StorageClient.StorageAccounts.Get(accountName);
                }
                catch (CloudException ex)
                {
                    Test.Assert(ex.ErrorCode.Equals("ResourceNotFound"), string.Format("Account {0} should not exist. Exception: {1}", accountName, ex));
                    validated = true;
                }
            }
            return accountName;
        }

        public string GenerateResourceGroupName()
        {
            return this.GenerateAvailableAccountName();
        }

        public string GenerateAccountLocation(string location)
        {
            if (location == this.mapAccountType(Constants.AccountType.Premium_LRS))
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
