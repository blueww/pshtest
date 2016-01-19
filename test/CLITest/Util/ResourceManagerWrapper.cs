namespace Management.Storage.ScenarioTest.Util
{
    using Microsoft.Azure.Common.Authentication;
    using Microsoft.Azure.Management.Resources;
    using Microsoft.Azure.Management.Resources.Models;

    class ResourceManagerWrapper
    {
        private ResourceManagementClient resourceManager;

        public ResourceManagerWrapper()
        {
            resourceManager = new ResourceManagementClient(Utility.GetTokenCloudCredential());
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
