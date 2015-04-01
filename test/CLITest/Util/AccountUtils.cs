using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SRPManagement = Microsoft.Azure.Management.Storage;
using SRPCredentials = Microsoft.Azure;
using Microsoft.WindowsAzure;
using MS.Test.Common.MsTestLib;
using Microsoft.WindowsAzure.Management.Storage;

namespace Management.Storage.ScenarioTest.Util
{
    class AccountUtils
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

        public AccountUtils()
        { 
            string certFile = Test.Data.Get("ManagementCert");
            string certPassword = Test.Data.Get("CertPassword");
            X509Certificate2 cert = new X509Certificate2(certFile, certPassword);
            CertificateCloudCredentials credetial = new CertificateCloudCredentials(Test.Data.Get("AzureSubscriptionID"), cert);
            SRPCredentials.CertificateCloudCredentials srpCredentials = new SRPCredentials.CertificateCloudCredentials(Test.Data.Get("AzureSubscriptionID"), cert);
            SRPStorageClient = new SRPManagement.StorageManagementClient(srpCredentials);
            StorageClient = new StorageManagementClient(credetial);
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

        private string GenerateAvailableAccountName()
        { 
            bool regenerate = false;
            string name = string.Empty;

            do
            {
                name = "clitest" + FileNamingGenerator.GenerateNameFromRange(random.Next(0, 18), ValidNameRange);

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
