using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Azure;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Resources.Models;
using MS.Test.Common.MsTestLib;

namespace Management.Storage.ScenarioTest.Util
{
    class ResourceManagerWrapper
    {
        private ResourceManagementClient resourceManager;

        public ResourceManagerWrapper()
        {
            string certFile = Test.Data.Get("ManagementCert");
            string certPassword = Test.Data.Get("CertPassword");
            X509Certificate2 cert = new X509Certificate2(certFile, certPassword);
            CertificateCloudCredentials credentials = new CertificateCloudCredentials(Test.Data.Get("AzureSubscriptionID"), cert);
            resourceManager = new ResourceManagementClient(credentials);
        }

        public void CreateResourceGroup(string resourceGroupName, string location)
        {
            resourceManager.ResourceGroups.CreateOrUpdate(resourceGroupName, new ResourceGroup
                {
                    Location = location
                });
        }

        public void DeleteResourceGroup(string resourceGroupName)
        {
            resourceManager.ResourceGroups.Delete(resourceGroupName);
        }
    }
}
