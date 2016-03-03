// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using Management.Storage.ScenarioTest.BVT;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional
{
    /// <summary>
    /// function test for storage context
    /// </summary>
    [TestClass]
    class StorageContext: TestBase
    {
        private string[] ExpectedErrorMsgs = new string[] { "The remote name could not be resolved:", "The remote server returned an error:" };

        [ClassInitialize()]
        public static void GetBlobClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void GetBlobClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// get containers from multiple storage contexts
        /// 8.19	New-AzureStorageContext Cmdlet Parameters Positive Functional Cases
        ///     9.	Use pipeline to run PowerShell cmdlets for two valid accounts
        /// </summary>
        //TODO should add more test about context and pipeline in each cmdlet
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StorageContext)]
        public void GetContainerFromMultipleStorageContext()
        {
            PowerShellAgent psAgent = (PowerShellAgent)CommandAgent;
            CloudStorageAccount account1 = TestBase.GetCloudStorageAccountFromConfig();
            CloudStorageAccount account2 = TestBase.GetCloudStorageAccountFromConfig("Secondary");
            string connectionString1 = account1.ToString(true);
            string connectionString2 = account2.ToString(true);
            Test.Assert(connectionString1 != connectionString2, "Use two different connection string {0} != {1}", connectionString1, connectionString2);
            
            CloudBlobUtil blobUtil1 = new CloudBlobUtil(account1);
            CloudBlobUtil blobUtil2 = new CloudBlobUtil(account2);
            string containerName = Utility.GenNameString("container");

            try
            {
                CloudBlobContainer container1 = blobUtil1.CreateContainer(containerName);
                CloudBlobContainer container2 = blobUtil2.CreateContainer(containerName);
                int containerCount = 2;

                string cmd = String.Format("$context1 = new-azurestoragecontext -connectionstring '{0}';$context2 = new-azurestoragecontext -connectionstring '{1}';($context1, $context2)", connectionString1, connectionString2);
                psAgent.UseContextParam = false;
                psAgent.AddPipelineScript(cmd);

                Test.Assert(CommandAgent.GetAzureStorageContainer(containerName), Utility.GenComparisonData("Get-AzureStorageContainer using multiple storage contexts", true));
                Test.Assert(CommandAgent.Output.Count == containerCount, String.Format("Want to retrieve {0} page blob, but retrieved {1} page blobs", containerCount, CommandAgent.Output.Count));

                CommandAgent.OutputValidation(new List<CloudBlobContainer>() { container1, container2 });
            }
            finally
            {
                blobUtil1.RemoveContainer(containerName);
                blobUtil2.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// get containers from valid and invalid storage contexts
        /// 8.19 New-AzureStorageContext Negative Functional Cases
        ///     3.	Use pipeline to run PowerShell cmdlets for one valid account and one invalid account
        /// </summary>
        //TODO should add more test about context and pipeline in each cmdlet
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StorageContext)]
        public void GetContainerFromValidAndInvalidStorageContext()
        {
            PowerShellAgent psAgent = (PowerShellAgent)CommandAgent;
            CloudStorageAccount account1 = TestBase.GetCloudStorageAccountFromConfig();
            string connectionString1 = account1.ToString(true);
            string randomAccountName = Utility.GenNameString("account");
            string randomAccountKey = Utility.GenNameString("key");
            randomAccountKey = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(randomAccountKey));

            string containerName = Utility.GenNameString("container");

            try
            {
                CloudBlobContainer container1 = blobUtil.CreateContainer(containerName);
                string cmd = String.Format("$context1 = new-azurestoragecontext -connectionstring '{0}';$context2 = new-azurestoragecontext -StorageAccountName '{1}' -StorageAccountKey '{2}';($context1, $context2)",
                    connectionString1, randomAccountName, randomAccountKey);
                psAgent.UseContextParam = false;
                psAgent.AddPipelineScript(cmd);

                Test.Assert(!CommandAgent.GetAzureStorageContainer(containerName), Utility.GenComparisonData("Get-AzureStorageContainer using valid and invalid storage contexts", false));
                Test.Assert(CommandAgent.ErrorMessages.Count == 1, "invalid storage context should return error");

                //the same error may output different error messages in different environments
                bool expectedError = CommandAgent.ErrorMessages[0].Contains("The remote server returned an error: (502) Bad Gateway") ||
                    CommandAgent.ErrorMessages[0].Contains("The remote name could not be resolved") || CommandAgent.ErrorMessages[0].Contains("The operation has timed out");
                Test.Assert(expectedError, "use invalid storage account should return 502 or could not be resolved exception or The operation has timed out, actually {0}", CommandAgent.ErrorMessages[0]);
            }
            finally
            {
                //TODO test the invalid storage account in subscription
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// run cmdlet without storage context
        /// 8.19 New-AzureStorageContext Negative Functional Cases
        ///     1. Do not specify the context parameter in the parameter set for each cmdlet
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StorageContext)]
        public void RunCmdletWithoutStorageContext()
        {
            PowerShellAgent.RemoveAzureSubscriptionIfExists();

            CLICommonBVT.SaveAndCleanSubScriptionAndEnvConnectionString();

            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                bool terminated = false;

                try
                {
                    CommandAgent.GetAzureStorageContainer(containerName);
                }
                catch (CmdletInvocationException e)
                {
                    terminated = true;
                    Test.Info(e.Message);
                    Test.Assert(e.Message.StartsWith("Can not find your azure storage credential."), "Can not find your azure storage credential.");
                }
                finally
                {
                    if (!terminated)
                    {
                        Test.AssertFail("without storage context should return a terminating error");
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }

            CLICommonBVT.RestoreSubScriptionAndEnvConnectionString();
        }

        /// <summary>
        /// Get storage context with specified storage account name/key/endpoint
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StorageContext)]
        public void GetStorageContextWithNameKeyEndPoint()
        {
            PowerShellAgent psAgent = (PowerShellAgent)CommandAgent;
            string accountName = Utility.GenNameString("account");
            string accountKey = Utility.GenBase64String("key");
            string endPoint = Utility.GenNameString("core.abc.def");

            Test.Assert(psAgent.NewAzureStorageContext(accountName, accountKey, endPoint), "New storage context with specified name/key/endpoint should succeed");
            // Verification for returned values
            Collection<Dictionary<string, object>> comp = GetContextCompareData(accountName, endPoint);
            CommandAgent.OutputValidation(comp);
        }

        /// <summary>
        /// Create anonymous storage context with specified end point
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StorageContext)]
        public void GetAnonymousStorageContextEndPoint()
        {
            PowerShellAgent psAgent = (PowerShellAgent)CommandAgent;
            string accountName = Utility.GenNameString("account");
            string accountKey = string.Empty;
            string endPoint = Utility.GenNameString("core.abc.def");

            Test.Assert(psAgent.NewAzureStorageContext(accountName, accountKey, endPoint), "New storage context with specified name/key/endpoint should succeed");
            // Verification for returned values
            Collection<Dictionary<string, object>> comp = GetContextCompareData(accountName, endPoint);
            comp[0]["StorageAccountName"] = "[Anonymous]";
            CommandAgent.OutputValidation(comp);
        }

        /// <summary>
        /// Get Container with invalid endpoint
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StorageContext)]
        public void GetContainerWithInvalidEndPoint()
        {
            PowerShellAgent psAgent = (PowerShellAgent)CommandAgent;
            string accountName = Utility.GenNameString("account");
            string accountKey = Utility.GenBase64String("key");
            string endPoint = Utility.GenNameString("core.abc.def");

            string cmd = String.Format("new-azurestoragecontext -StorageAccountName {0} " +
                "-StorageAccountKey {1} -Endpoint {2}", accountName, accountKey, endPoint);
            psAgent.AddPipelineScript(cmd);
            psAgent.UseContextParam = false;
            Test.Assert(!CommandAgent.GetAzureStorageContainer(string.Empty),
                "Get containers with invalid endpoint should fail");
            ExpectedContainErrorMessage(ExpectedErrorMsgs);
        }

        /// <summary>
        /// Generate storage context compare data
        /// </summary>
        /// <param name="StorageAccountName">Storage Account Name</param>
        /// <param name="endPoint">end point</param>
        /// <returns>storage context compare data</returns>
        private Collection<Dictionary<string, object>> GetContextCompareData(string StorageAccountName, string endPoint)
        {
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            string[] endPoints = Utility.GetStorageEndPoints(StorageAccountName, true, endPoint);
            comp.Add(new Dictionary<string, object>{
                {"StorageAccountName", StorageAccountName},
                {"BlobEndPoint", endPoints[0]},
                {"QueueEndPoint", endPoints[1]},
                {"TableEndPoint", endPoints[2]}
            });
            return comp;
        }
        
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StorageContext)]
        public void GetMooncakeStorageContext()
        {
            CLICommonBVT.SaveAndCleanSubScriptionAndEnvConnectionString();

            PowerShellAgent.ImportAzureSubscriptionAndSetStorageAccount(
                Test.Data.Get("MooncakeSubscriptionPath"),
                Test.Data.Get("MooncakeSubscriptionName"),
                null);

            ValidateStorageContext(Test.Data.Get("MooncakeStorageAccountName"), Test.Data.Get("MooncakeStorageAccountKey"), "core.chinacloudapi.cn");
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.StorageContext)]
        public void GetStorageContextWithoutSubscription()
        {
            CLICommonBVT.SaveAndCleanSubScriptionAndEnvConnectionString();
            
            ValidateStorageContext(Test.Data.Get("StorageAccountName"), Test.Data.Get("StorageAccountKey"), "core.windows.net");
        }

        private void ValidateStorageContext(string accountName, string accountKey, string endpointSuffix)
        {
            object context = PowerShellAgent.GetStorageContext(accountName, accountKey);

            CloudStorageAccount storageAccount = null;

            Type myType = context.GetType();
            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());

            foreach (PropertyInfo prop in props)
            {
                if (string.Equals(prop.Name, "BlobEndPoint"))
                {
                    string blobEndPoint = prop.GetValue(context, null) as string;

                    Test.Assert(blobEndPoint.Contains(string.Format("{0}.blob.{1}", accountName, endpointSuffix)), "BlobEndPoint should be correct.");
                }
                else if (string.Equals(prop.Name, "TableEndPoint"))
                {
                    string tableEndpoint = prop.GetValue(context, null) as string;

                    Test.Assert(tableEndpoint.Contains(string.Format("{0}.table.{1}", accountName, endpointSuffix)), "TableEndPoint should be correct.");
                }
                else if (string.Equals(prop.Name, "QueueEndPoint"))
                {
                    string queueEndPoint = prop.GetValue(context, null) as string;

                    Test.Assert(queueEndPoint.Contains(string.Format("{0}.queue.{1}", accountName, endpointSuffix)), "QueueEndPoint should be correct.");
                }
                else if (string.Equals(prop.Name, "EndPointSuffix"))
                {
                    string contextEndPointSuffix = prop.GetValue(context, null) as string;

                    Test.Assert(contextEndPointSuffix.Contains(endpointSuffix), "EndPointSuffix should be correct.");
                }
                else if (string.Equals(prop.Name, "StorageAccountName"))
                {
                    string storageAccountName = prop.GetValue(context, null) as string;

                    Test.Assert(string.Equals(storageAccountName, accountName), "StorageAccountName should be correct.");
                }
                else if (string.Equals(prop.Name, "StorageAccount"))
                {
                    storageAccount = prop.GetValue(context, null) as CloudStorageAccount;
                    Test.Assert(string.Equals(storageAccount.Credentials.AccountName, accountName), "StorageAccount name should be correct.");
                }
            }
            
            PowerShellAgent.SetStorageContext(accountName, accountKey);

            UploadBlobWithAccount(storageAccount);
        }

        private void UploadBlobWithAccount(CloudStorageAccount storageAccount)
        {
            string uploadDirRoot = Test.Data.Get("UploadDir");
            Test.Verbose("Create Upload dir {0}", uploadDirRoot);
            FileUtil.CreateDirIfNotExits(uploadDirRoot);
            FileUtil.CleanDirectory(uploadDirRoot);
            CloudBlobUtil blobUtil = new CloudBlobUtil(storageAccount);
            CloudBlobContainer container = blobUtil.CreateContainer(Utility.GenNameString("container"));

            try
            {
                string filePath = Path.Combine(uploadDirRoot, Utility.GenNameString("fileName"));

                FileUtil.GenerateSmallFile(filePath, 1024);

                string blobName = Utility.GenNameString("BlobName");

                Test.Assert(CommandAgent.SetAzureStorageBlobContent(filePath, container.Name, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob, blobName), "Upload blob should succeed.");

                ICloudBlob blob = container.GetBlobReferenceFromServer(blobName);
                string localMd5 = FileUtil.GetFileContentMD5(filePath);

                Test.Assert(localMd5 == blob.Properties.ContentMD5, string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, blob.Properties.ContentMD5));
            }
            finally
            {
                FileUtil.CleanDirectory(uploadDirRoot);
                blobUtil.RemoveContainer(container);
            }
        }
    }
}
