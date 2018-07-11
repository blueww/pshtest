using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Management.Storage.ScenarioTest.BVT;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.File;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using StorageFile = Microsoft.WindowsAzure.Storage.File;

namespace Management.Storage.ScenarioTest.Functional
{
    [TestClass]
    public class InvalidCredentialCases : TestBase
    {
        [ClassInitialize()]
        public static void InvalidCredentialCasesClassInit(TestContext testContext)
        {
            StorageAccount = null;
            TestBase.TestClassInitialize(testContext);
            CLICommonBVT.SaveAndCleanSubScriptionAndEnvConnectionString();
            string storageAccountName = Test.Data.Get("StorageAccountName");
            string storageEndPoint = Test.Data.Get("StorageEndPoint").Trim();
            Agent.Context = StorageAccount;

            if (lang == Language.PowerShell)
            {
                PowerShellAgent.SetAnonymousStorageContext(storageAccountName, useHttps, storageEndPoint);
            }
            else
            {
                NodeJSAgent.AgentConfig.ConnectionString = null;
                PreviousUseEnvVar = NodeJSAgent.AgentConfig.UseEnvVar;
                NodeJSAgent.AgentConfig.UseEnvVar = true;
            }
        }

        private static bool PreviousUseEnvVar = false;

        [ClassCleanup()]
        public static void InvalidCredentialCasesClassCleanup()
        {
            CLICommonBVT.RestoreSubScriptionAndEnvConnectionString();
            NodeJSAgent.AgentConfig.UseEnvVar = PreviousUseEnvVar;
            TestBase.TestClassCleanup();
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceCORS)]
        public void SetCORSRuleWithInvalidCredential()
        { 
            Constants.ServiceType serviceType = GetRandomServiceType();
            var corsRules = CORSRuleUtil.GetRandomValidCORSRules(random.Next(1, 5));

            Test.Assert(!CommandAgent.SetAzureStorageCORSRules(serviceType, corsRules), "Set CORS rules with invalid credential should fail.");
            CheckErrorMessage();
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceCORS)]
        public void GetCORSRuleWithInvalidCredential()
        {
            Constants.ServiceType serviceType = GetRandomServiceType();

            Test.Assert(!CommandAgent.GetAzureStorageCORSRules(serviceType), "Get CORS rules with invalid credential should fail.");
            CheckErrorMessage();
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.ServiceCORS)]
        public void RemoveCORSRuleWithInvalidCredential()
        {
            Constants.ServiceType serviceType = GetRandomServiceType();

            Test.Assert(!CommandAgent.RemoveAzureStorageCORSRules(serviceType), "Remove CORS rules with invalid credential should fail.");
            CheckErrorMessage();
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        public void SetShareQuotaWithInvalidCredential()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                Test.Assert(!CommandAgent.SetAzureStorageShareQuota(shareName, random.Next(1, 5120)), "Set quota with invalid credential should fail.");
                CheckErrorMessage();
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void StopFileCopyWithInvalidCredential()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                StorageFile.CloudFile file = fileUtil.CreateFile(share.GetRootDirectoryReference(), Utility.GenNameString(""));
                Test.Assert(!CommandAgent.StopFileCopy(shareName, file.Name, Guid.NewGuid().ToString()), "Stop file copy with invalid credential should fail.");
                CheckErrorMessage();
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void GetAzureStorageUsageInvalidCredential()
        {
            if (isResourceMode)
            {
                PowerShellAgent.RemoveAzureSubscriptionIfExists();
                CommandAgent.Logout();
                Test.Assert(!CommandAgent.GetAzureStorageUsage(), "Get azure storage usage should fail.");
                ExpectedContainErrorMessage("No subscription found in the context.");
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void CheckAccountNameAvailabilityInvalidCredential()
        {
            if (isResourceMode)
            {
                PowerShellAgent.RemoveAzureSubscriptionIfExists();
                CommandAgent.Logout();
                string accountName = AccountUtils.GenerateAvailableAccountName();
                Test.Assert(!CommandAgent.CheckNameAvailability(accountName), "Check name availability should fail.");
                ExpectedContainErrorMessage("No subscription found in the context.");
            }
        }

        private Constants.ServiceType GetRandomServiceType()
        {
            var serviceTypes = Enum.GetValues(typeof(Constants.ServiceType));

            return (Constants.ServiceType)serviceTypes.GetValue(random.Next(0, serviceTypes.Length - 2));
        }

        private void CheckErrorMessage()
        {
            if (lang == Language.PowerShell)
            {
                ExpectedContainErrorMessage("The specified resource does not exist.");
            }
            else
            {
                ExpectedContainErrorMessage("Please set the storage account parameters or one of the following two environment variables to use the storage command");
            }
        }
    }
}
