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

            if (lang == Language.PowerShell)
            {
                PowerShellAgent.SetAnonymousStorageContext(storageAccountName, useHttps, storageEndPoint);
            }
        }

        [ClassCleanup()]
        public static void InvalidCredentialCasesClassCleanup()
        {
            CLICommonBVT.RestoreSubScriptionAndEnvConnectionString();
            TestBase.TestClassCleanup();
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void SetCORSRuleWithInvalidCredential()
        { 
            Constants.ServiceType serviceType = GetRandomServiceType();
            var corsRules = CORSRuleUtil.GetRandomValidCORSRules(random.Next(1, 5));

            Test.Assert(!agent.SetAzureStorageCORSRules(serviceType, corsRules), "Set CORS rules with invalid credential should fail.");
            ExpectedContainErrorMessage("The specified resource does not exist.");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void GetCORSRuleWithInvalidCredential()
        {
            Constants.ServiceType serviceType = GetRandomServiceType();

            Test.Assert(!agent.GetAzureStorageCORSRules(serviceType), "Get CORS rules with invalid credential should fail.");
            ExpectedContainErrorMessage("The specified resource does not exist.");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void RemoveCORSRuleWithInvalidCredential()
        {
            Constants.ServiceType serviceType = GetRandomServiceType();

            Test.Assert(!agent.RemoveAzureStorageCORSRules(serviceType), "Remove CORS rules with invalid credential should fail.");
            ExpectedContainErrorMessage("The specified resource does not exist.");
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void SetShareQuotaWithInvalidCredential()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                Test.Assert(!agent.SetAzureStorageShareQuota(shareName, random.Next(1, 5120)), "Set quota with invalid credential should fail.");
                ExpectedContainErrorMessage("The specified resource does not exist.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        [TestMethod]
        [TestCategory(Tag.Function)]
        public void StopFileCopyWithInvalidCredential()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                StorageFile.CloudFile file = fileUtil.CreateFile(share.GetRootDirectoryReference(), Utility.GenNameString(""));
                Test.Assert(!agent.StopFileCopy(shareName, file.Name, null), "Stop file copy with invalid credential should fail.");
                ExpectedContainErrorMessage("The specified resource does not exist.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        private Constants.ServiceType GetRandomServiceType()
        {
            var serviceTypes = Enum.GetValues(typeof(Constants.ServiceType));

            return (Constants.ServiceType)serviceTypes.GetValue(random.Next(0, serviceTypes.Length - 1));
        }
    }
}
