namespace Management.Storage.ScenarioTest.Util
{
#if DOTNET5_4
    using Microsoft.Azure.Management.ResourceManager;
    using Microsoft.Azure.Management.ResourceManager.Models;
    using MS.Test.Common.MsTestLib;
#else
    using Microsoft.Azure.Management.Resources;
    using Microsoft.Azure.Management.Resources.Models;
#endif

    class ResourceManagerWrapper
    {
        private ResourceManagementClient resourceManager;

        public ResourceManagerWrapper()
        {
            resourceManager = new ResourceManagementClient(Utility.GetTokenCredential())
            {
                SubscriptionId = Test.Data.Get("AzureSubscriptionID")
            };
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
