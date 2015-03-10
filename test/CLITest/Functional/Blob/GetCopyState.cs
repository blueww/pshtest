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

using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using StorageBlob = Microsoft.WindowsAzure.Storage.Blob;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional tests for Get-CopyState
    /// </summary>
    [TestClass]
    public class GetCopyState : TestBase
    {
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
        /// monitor mulitple copy progress
        /// 8.21	Get-AzureStorageBlobCopyState Positive Functional Cases
        ///     3.	Monitor a list of copying blobs
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobCopyState)]
        public void GetCopyStateFromMultiBlobsTest()
        {
            CloudBlobContainer srcContainer = blobUtil.CreateContainer();
            CloudBlobContainer destContainer = blobUtil.CreateContainer();

            List<CloudBlob> blobs = blobUtil.CreateRandomBlob(srcContainer);

            try
            {
                ((PowerShellAgent)agent).AddPipelineScript(String.Format("Get-AzureStorageBlob -Container {0}", srcContainer.Name));
                ((PowerShellAgent)agent).AddPipelineScript(String.Format("Start-AzureStorageBlobCopy -DestContainer {0}", destContainer.Name));

                Test.Assert(agent.GetAzureStorageBlobCopyState(string.Empty, string.Empty, true), "Get copy state for many blobs should succeed.");
                Test.Assert(agent.Output.Count == blobs.Count, String.Format("Expected get {0} copy state, and actually get {1} copy state", blobs.Count, agent.Output.Count));
                List<IListBlobItem> destBlobs = destContainer.ListBlobs().ToList();
                Test.Assert(destBlobs.Count == blobs.Count, String.Format("Expected get {0} copied blobs, and actually get {1} copy state", blobs.Count, destBlobs.Count));

                for (int i = 0, count = agent.Output.Count; i < count; i++)
                {
                    AssertFinishedCopyState(blobs[i].Uri, i);
                }
            }
            finally
            {
                blobUtil.RemoveContainer(srcContainer.Name);
                blobUtil.RemoveContainer(destContainer.Name);
            }
        }

        /// <summary>
        /// monitor mulitple copy progress
        /// 8.21	Get-AzureStorageBlobCopyState Positive Functional Cases
        ///     3.	Monitor a list of copying blobs
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobCopyState)]
        [TestCategory(CLITag.GetBlobCopyState)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetCopyStateWithInvalidNameTest()
        {
            string invalidContainerName = "Invalid";
            int maxBlobNameLength = 1024;
            string invalidBlobName = new string('a', maxBlobNameLength + 1);
            string invalidContainerErrorMessage;
            string invalidBlobErrorMessage;
            if (lang == Language.PowerShell)
            {
                invalidContainerErrorMessage = String.Format("Container name '{0}' is invalid.", invalidContainerName);
                invalidBlobErrorMessage = String.Format("Blob name '{0}' is invalid.", invalidBlobName);
            }
            else
            {
                invalidContainerErrorMessage = "Container name format is incorrect";
                invalidBlobErrorMessage = "BadRequest";
            }
            Test.Assert(!agent.GetAzureStorageBlobCopyState(invalidContainerName, Utility.GenNameString("blob"), false), "get copy state should failed with invalid container name");
            ExpectedStartsWithErrorMessage(invalidContainerErrorMessage);
            Test.Assert(!agent.GetAzureStorageBlobCopyState(Utility.GenNameString("container"), invalidBlobName, false), "get copy state should failed with invalid blob name");
            ExpectedStartsWithErrorMessage(invalidBlobErrorMessage);
        }

        /// <summary>
        /// monitor mulitple copy progress
        /// 8.21	Get-AzureStorageBlobCopyState Positive Functional Cases
        ///     3.	Monitor a list of copying blobs
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobCopyState)]
        [TestCategory(CLITag.GetBlobCopyState)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetCopyStateWithNotExistContainerAndBlobTest()
        {
            string srcContainerName = Utility.GenNameString("copy");
            string blobName = Utility.GenNameString("blob");

            string errorMessage = string.Empty;
            Validator validator;
            if (lang == Language.PowerShell)
            {
                errorMessage = string.Format("Can not find blob '{0}' in container '{1}', or the blob type is unsupported.", blobName, srcContainerName);
                validator = ExpectedEqualErrorMessage;
            }
            else
            {
                errorMessage = string.Format("Blob {0} in Container {1} doesn't exist", blobName, srcContainerName);
                validator = ExpectedStartsWithErrorMessage;
            }
            Test.Assert(!agent.GetAzureStorageBlobCopyState(srcContainerName, blobName, false), "Get copy state should fail with not existing container");
            validator(errorMessage);

            try
            {
                CloudBlobContainer srcContainer = blobUtil.CreateContainer(srcContainerName);
                Test.Assert(!agent.GetAzureStorageBlobCopyState(srcContainerName, blobName, false), "Get copy state should fail with not existing blob");
                validator(errorMessage);
            }
            finally
            {
                blobUtil.RemoveContainer(srcContainerName);
            }
        }

        /// <summary>
        /// monitor mulitple copy progress
        /// 8.21	Get-AzureStorageBlobCopyState Positive Functional Cases
        ///    4.	Monitor copying status of the blob in root container
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobCopyState)]
        [TestCategory(CLITag.GetBlobCopyState)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetCopyStateFromRootContainerTest()
        {
            CloudBlobContainer rootContainer = blobUtil.CreateContainer("$root");

            string srcBlobName = Utility.GenNameString("src");
            CloudBlob srcBlob = blobUtil.CreateRandomBlob(rootContainer, srcBlobName);
            CloudBlob destBlob = blobUtil.CreateBlob(rootContainer, Utility.GenNameString("dest"), srcBlob.BlobType);

            if (destBlob.BlobType == StorageBlob.BlobType.BlockBlob)
            {
                ((CloudBlockBlob)destBlob).StartCopyFromBlob((CloudBlockBlob)srcBlob);
            }
            else
            {
                ((CloudPageBlob)destBlob).StartCopyFromBlob((CloudPageBlob)srcBlob);
            }

            Test.Assert(agent.GetAzureStorageBlobCopyState("$root", destBlob.Name, true), "Get copy state in $root container should succeed.");
            AssertFinishedCopyState(srcBlob.Uri);
        }

        /// <summary>
        /// monitor copy progress for cross account copy
        /// 8.21	Get-AzureStorageBlobCopyState Positive Functional Cases
        ///     5.	Get the copy status (on-going) on specified blob for cross account copying
        /// This test use the start-copy pipeline. so It also validate the start-copy cmdlet
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobCopyState)]
        [TestCategory(CLITag.GetBlobCopyState)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetCopyStateFromCrossAccountCopyTest()
        {
            CloudStorageAccount secondaryAccount = TestBase.GetCloudStorageAccountFromConfig("Secondary");
            object destContext;
            if (lang == Language.PowerShell)
            {
                destContext = PowerShellAgent.GetStorageContext(secondaryAccount.ToString(true));
            }
            else
            {
                destContext = secondaryAccount; 
            }
            CloudBlobUtil destBlobUtil = new CloudBlobUtil(secondaryAccount);
            string destContainerName = Utility.GenNameString("secondary");
            CloudBlobContainer destContainer = destBlobUtil.CreateContainer(destContainerName);
            if (lang == Language.PowerShell)
            {
                blobUtil.SetupTestContainerAndBlob();
            }
            else
            {
                blobUtil.SetupTestContainerAndBlob(blobNamePrefix: "blob");
            }
            //remove the same name container in source storage account, so we could avoid some conflicts.
            if (!blobUtil.Blob.ServiceClient.BaseUri.Host.Equals(destContainer.ServiceClient.BaseUri.Host))
            {
                blobUtil.RemoveContainer(destContainer.Name);
            }

            try
            {
                Test.Assert(agent.StartAzureStorageBlobCopy(blobUtil.Blob, destContainer.Name, string.Empty, destContext), "Start cross account copy should succeed");
                int expectedBlobCount = 1;
                Test.Assert(agent.Output.Count == expectedBlobCount, String.Format("Expected get {0} copy blob, and actually it's {1}", expectedBlobCount, agent.Output.Count));

                CloudBlob destBlob;
                object context;
                if (lang == Language.PowerShell)
                {
                    destBlob = (CloudBlob)agent.Output[0]["ICloudBlob"];
                    //make sure this context is different from the PowerShell.Context
                    context = agent.Output[0]["Context"];
                    Test.Assert(PowerShellAgent.Context != context, "make sure you are using different context for cross account copy");
                }
                else
                {
                    destBlob = StorageExtensions.GetBlobReferenceFromServer(destContainer, (string)agent.Output[0]["blob"]);
                    context = destContext;
                }
                Test.Assert(agent.GetAzureStorageBlobCopyState(destBlob, context, true), "Get copy state in dest container should succeed.");
                AssertFinishedCopyState(blobUtil.Blob.Uri);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
                destBlobUtil.RemoveContainer(destContainer.Name);
            }
        }

        /// <summary>
        /// monitor copy progress for cross account copy
        /// 8.21	Get-AzureStorageBlobCopyState Positive Functional Cases
        ///     5.	6.	Get the copy status (on-going) on specified blob for Uri copying
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobCopyState)]
        [TestCategory(CLITag.GetBlobCopyState)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetCopyStateFromUriTest()
        {
            blobUtil.SetupTestContainerAndBlob();
            string copiedName = Utility.GenNameString("copied");

            //Set the blob permission, so the copy task could directly copy by uri
            BlobContainerPermissions permission = new BlobContainerPermissions();
            permission.PublicAccess = BlobContainerPublicAccessType.Blob;
            blobUtil.Container.SetPermissions(permission);

            try
            {
                Test.Assert(agent.StartAzureStorageBlobCopy(blobUtil.Blob.Uri.ToString(), blobUtil.ContainerName, copiedName, PowerShellAgent.Context), Utility.GenComparisonData("Start copy blob using source uri", true));
                Test.Assert(agent.GetAzureStorageBlobCopyState(blobUtil.ContainerName, copiedName, true), "Get copy state in dest container should succeed.");
                AssertFinishedCopyState(blobUtil.Blob.Uri);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// monitor copy progress for cross account copy
        /// 8.21	Get-AzureStorageBlobCopyState Positive Functional Cases
        ///     5.	6.	Get the copy status (on-going) on specified blob for Uri copying
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.GetBlobCopyState)]
        [TestCategory(CLITag.GetBlobCopyState)]
        [TestCategory(CLITag.NodeJSFT)]
        public void GetCopyStateWhenCopyingTest()
        {
            CloudBlobContainer Container = blobUtil.CreateContainer();
            string ContainerName = Container.Name;
            string BlobName = Utility.GenNameString("blob");
            CloudBlob Blob = blobUtil.CreateRandomBlob(Container, BlobName);
            
            string uri = Test.Data.Get("BigFileUri");
            Test.Assert(!String.IsNullOrEmpty(uri), string.Format("Big file uri should be not empty, actually it's {0}", uri));

            if (String.IsNullOrEmpty(uri))
            {
                return;
            }

            Blob.StartCopyFromBlob(new Uri(uri));

            int maxMonitorCount = 3; 
            int checkCount = 0;
            int sleepInterval = 1000; //ms

            CopyStatus status = CopyStatus.Pending;

            try
            {
                int expectedCopyStateCount = 1;

                do
                {
                    Test.Info(String.Format("{0}th check current copy state", checkCount));
                    Test.Assert(agent.GetAzureStorageBlobCopyState(ContainerName, BlobName, false), "Get copy state in dest container should succeed.");
                    
                    Test.Assert(agent.Output.Count == expectedCopyStateCount, String.Format("Should contain {0} copy state, and actually it's {1}", expectedCopyStateCount, agent.Output.Count));

                    if (lang == Language.PowerShell)
                    {
                        status = (CopyStatus)agent.Output[0]["Status"];
                        Test.Assert(status == CopyStatus.Pending, String.Format("Copy status should be Pending, actually it's {0}", status));
                    }
                    else
                    {
                        if (((string)agent.Output[0]["copyStatus"]).Equals("pending"))
                        {
                            status = CopyStatus.Pending;
                        }
                        else
                        {
                            status = CopyStatus.Invalid;
                        }
                    }
                    
                    checkCount++;
                    Thread.Sleep(sleepInterval);
                }
                while (status == CopyStatus.Pending && checkCount < maxMonitorCount);

                Test.Info("Finish the monitor loop and try to abort copy");

                try
                {
                    Blob.AbortCopy(Blob.CopyState.CopyId);
                }
                catch (StorageException e)
                {
                    //TODO use extension method
                    if (e.RequestInformation != null && e.RequestInformation.HttpStatusCode == 409)
                    {
                        Test.Info("Skip 409 abort conflict exception. Error:{0}", e.Message);
                        Test.Info("Detail Error Message: {0}", e.RequestInformation.HttpStatusMessage);
                    }
                    else
                    {
                        Test.AssertFail(String.Format("Can't abort copy. Error: {0}", e.Message));
                    }
                }

                Test.Assert(agent.GetAzureStorageBlobCopyState(ContainerName, BlobName, false), "Get copy state in dest container should succeed.");
                Test.Assert(agent.Output.Count == expectedCopyStateCount, String.Format("Should contain {0} copy state, and actually it's {1}", expectedCopyStateCount, agent.Output.Count));
                if (lang == Language.PowerShell)
                {
                    status = (CopyStatus)agent.Output[0]["Status"];
                }
                else
                {
                    if (((string)agent.Output[0]["copyStatus"]).Equals("aborted"))
                    {
                        status = CopyStatus.Aborted;
                    }
                    else
                    {
                        status = CopyStatus.Pending;
                    }
                }
                Test.Assert(status == CopyStatus.Aborted, String.Format("Copy status should be Aborted, actually it's {0}", status));
            }
            finally
            {
                blobUtil.RemoveContainer(Container.Name);
            }
        }

        private void AssertFinishedCopyState(Uri SourceUri, int startIndex = 0)
        {
            string expectedSourceUri = CloudBlobUtil.ConvertCopySourceUri(SourceUri.ToString());
            Test.Assert(agent.Output.Count > startIndex, String.Format("Should contain the great than {0} copy state, and actually it's {1}", startIndex, agent.Output.Count));
            if (lang == Language.PowerShell)
            {
                string sourceUri = ((Uri)agent.Output[startIndex]["Source"]).ToString();
                Test.Assert(sourceUri.StartsWith(expectedSourceUri), String.Format("source uri should start with {0}, and actually it's {1}", expectedSourceUri, sourceUri));
                CopyStatus status = (CopyStatus)agent.Output[startIndex]["Status"];
                Test.Assert(status != CopyStatus.Pending, String.Format("Copy status should not be Pending, actually it's {0}", status));
                string copyId = (string)agent.Output[startIndex]["CopyId"];
                Test.Assert(!String.IsNullOrEmpty(copyId), "Copy ID should be not empty");
            }
            else
            {
                string status = (string)agent.Output[startIndex]["copyStatus"];
                Test.Assert(status != "pending", String.Format("Copy status should not be Pending, actually it's {0}", status));
                string copyId = (string)agent.Output[startIndex]["copyId"];
                Test.Assert(!String.IsNullOrEmpty(copyId), "Copy ID should be not empty");
            }
        }
    }
}
