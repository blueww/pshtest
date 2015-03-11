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
using System.Text;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional tests for Start-CopyBlob
    /// </summary>
    [TestClass]
    public class StartCopy : TestBase
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void SetBlobContentClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Cross storage account copy
        /// 8.20	Start-AzureStorageBlob Positive Functional Cases
        ///    3. Cross account copy and Properties and metadata could be copied correctly
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.StartCopyBlob)]
        [TestCategory(CLITag.StartCopyBlob)]
        [TestCategory(CLITag.NodeJSFT)]
        public void StartCrossAccountCopyWithMetaAndPropertiesTest()
        {
            if (lang == Language.PowerShell)
            {
                blobUtil.SetupTestContainerAndBlob();
            }
            else
            {
                blobUtil.SetupTestContainerAndBlob(blobNamePrefix: "blob");
            }

            try
            {
                CloudStorageAccount secondaryAccount = TestBase.GetCloudStorageAccountFromConfig("Secondary");
                object destContext = null;
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
                AssertCopyBlobCrossContainer(blobUtil.Blob, destContainer, string.Empty, destContext);
                destBlobUtil.RemoveContainer(destContainer.Name);
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// Cross storage account copy
        /// 8.20	Start-AzureStorageBlob Positive Functional Cases
        ///    2.	Root container case
        ///     1.	Root -> Non-Root
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.StartCopyBlob)]
        [TestCategory(CLITag.StartCopyBlob)]
        [TestCategory(CLITag.NodeJSFT)]
        public void StartCopyFromRootToNonRootContainerTest()
        {
            CloudBlobContainer rootContainer = blobUtil.CreateContainer("$root");

            string srcBlobName = Utility.GenNameString("src");
            CloudBlob srcBlob = blobUtil.CreateRandomBlob(rootContainer, srcBlobName);
            CloudBlobContainer destContainer = blobUtil.CreateContainer();

            try
            {
                AssertCopyBlobCrossContainer(srcBlob, destContainer, Utility.GenNameString("dest"), PowerShellAgent.Context);
            }
            finally
            {
                //Keep the $root container since it may cause many confict exceptions
                blobUtil.RemoveContainer(destContainer.Name);
            }
        }

        /// <summary>
        /// Cross storage account copy
        /// 8.20	Start-AzureStorageBlob Positive Functional Cases
        ///    2.	Root container case
        ///     2.	Non-Root -> Root
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.StartCopyBlob)]
        [TestCategory(CLITag.StartCopyBlob)]
        [TestCategory(CLITag.NodeJSFT)]
        public void StartCopyFromNonRootToRootContainerTest()
        {
            CloudBlobContainer rootContainer = blobUtil.CreateContainer("$root");

            string srcBlobName = Utility.GenNameString("src");
            CloudBlobContainer srcContainer = blobUtil.CreateContainer();
            CloudBlob srcBlob = blobUtil.CreateRandomBlob(srcContainer, srcBlobName);

            try
            {
                AssertCopyBlobCrossContainer(srcBlob, rootContainer, string.Empty, PowerShellAgent.Context);
            }
            finally
            {
                //Keep the $root container since it may cause many confict exceptions
                blobUtil.RemoveContainer(srcContainer.Name);
            }
        }

        /// <summary>
        /// Cross storage account copy
        /// 8.20	Start-AzureStorageBlob Positive Functional Cases
        ///    2.	Root container case
        ///     3.	Root -> Root
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.StartCopyBlob)]
        [TestCategory(CLITag.StartCopyBlob)]
        [TestCategory(CLITag.NodeJSFT)]
        public void StartCopyFromRootToRootContainerTest()
        {
            CloudBlobContainer rootContainer = blobUtil.CreateContainer("$root");

            string srcBlobName = Utility.GenNameString("src");
            CloudBlob srcBlob = blobUtil.CreateRandomBlob(rootContainer, srcBlobName);

            try
            {
                AssertCopyBlobCrossContainer(srcBlob, rootContainer, Utility.GenNameString("dest"), PowerShellAgent.Context);
            }
            finally
            {
                //Keep the $root container since it may cause many confict exceptions
            }
        }

        /// <summary>
        /// Cross storage account copy
        /// 8.20	Start-AzureStorageBlob Positive Functional Cases
        ///    2.	Root container case
        ///     3.	Root -> Root
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.StartCopyBlob)]
        public void StartCopyFromSnapshotTest()
        {
            CloudBlobContainer srcContainer = blobUtil.CreateContainer();
            CloudBlobContainer destContainer = blobUtil.CreateContainer();

            string srcBlobName = Utility.GenNameString("src");
            CloudBlob srcBlob = blobUtil.CreateRandomBlob(srcContainer, srcBlobName);
            CloudBlob snapshot = null;

            if (srcBlob.BlobType == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob)
            {
                snapshot = ((CloudBlockBlob)srcBlob).CreateSnapshot();
            }
            else if (srcBlob.BlobType == Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob)
            {
                snapshot = ((CloudPageBlob)srcBlob).CreateSnapshot();
            }
            else if (srcBlob.BlobType == Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob)
            {
                snapshot = ((CloudAppendBlob)srcBlob).CreateSnapshot();
            }
            else
            {
                throw new InvalidOperationException(string.Format("Invalid blob type: {0}", srcBlob.BlobType));
            }

            try
            {
                Func<bool> StartCopyUsingCloudBlob = delegate()
                {
                    return agent.StartAzureStorageBlobCopy(snapshot, destContainer.Name, string.Empty, PowerShellAgent.Context);
                };

                CloudBlob destBlob = AssertCopyBlobCrossContainer(snapshot, destContainer, string.Empty, PowerShellAgent.Context);
                Test.Assert(snapshot.SnapshotTime != null, "The snapshot time of destination blob should be not null");
                Test.Assert(destBlob.SnapshotTime == null, "The snapshot time of destination blob should be null");
            }
            finally
            {
                blobUtil.RemoveContainer(srcContainer.Name);
                blobUtil.RemoveContainer(destContainer.Name);
            }
        }

        /// <summary>
        /// Cross storage account copy
        /// 8.20	Start-AzureStorageBlob Negative Functional Cases
        ///   5.	Copy blob to itself
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.StartCopyBlob)]
        [TestCategory(CLITag.StartCopyBlob)]
        [TestCategory(CLITag.NodeJSFT)]
        public void StartCopyFromSelfTest()
        {
            CloudBlobContainer srcContainer = blobUtil.CreateContainer();

            string srcBlobName = Utility.GenNameString("src");
            CloudBlob srcBlob = blobUtil.CreateRandomBlob(srcContainer, srcBlobName);

            try
            {
                if (lang == Language.PowerShell)
                {
                    Test.Assert(!agent.StartAzureStorageBlobCopy(srcBlob.Container.Name, srcBlob.Name, srcContainer.Name, string.Empty, PowerShellAgent.Context), "blob copy should failed when copy itself");
                    string errorMessage = "Source and destination cannot be the same.";
                    Test.Assert(errorMessage == agent.ErrorMessages[0], String.Format("Expected error message: {0}, and actually it's {1}", errorMessage, agent.ErrorMessages[0]));
                }
                else
                {
                    Test.Assert(agent.StartAzureStorageBlobCopy(srcBlob.Container.Name, srcBlob.Name, srcContainer.Name, string.Empty, PowerShellAgent.Context), "blob copy should succeed when copy itself");
                }
            }
            finally
            {
                blobUtil.RemoveContainer(srcContainer.Name);
            }
        }

        /// <summary>
        /// Cross storage account copy
        /// 8.20	Start-AzureStorageBlob Negative Functional Cases
        ///     1.	Copy the blob with invalid container name or blob name
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.StartCopyBlob)]
        [TestCategory(CLITag.StartCopyBlob)]
        [TestCategory(CLITag.NodeJSFT)]
        public void StartCopyWithInvalidNameTest()
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
                invalidBlobErrorMessage = "One of the request inputs is out of range";
            }

            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            try
            {
                CloudBlobContainer container = blobUtil.CreateContainer(containerName);
                blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(!agent.StartAzureStorageBlobCopy(invalidContainerName, Utility.GenNameString("blob"), containerName, Utility.GenNameString("blob")), "Start copy should failed with invalid src container name");
                ExpectedStartsWithErrorMessage(invalidContainerErrorMessage);

                Test.Assert(!agent.StartAzureStorageBlobCopy(containerName, Utility.GenNameString("blob"), invalidContainerName, Utility.GenNameString("blob")), "Start copy should failed with invalid dest container name");
                ExpectedStartsWithErrorMessage(invalidContainerErrorMessage);

                Test.Assert(!agent.StartAzureStorageBlobCopy(containerName, invalidBlobName, containerName, Utility.GenNameString("blob")), "Start copy should failed with invalid src blob name");
                ExpectedStartsWithErrorMessage(invalidBlobErrorMessage);

                Test.Assert(!agent.StartAzureStorageBlobCopy(containerName, blobName, containerName, invalidBlobName), "Start copy should failed with invalid dest blob name");
                ExpectedStartsWithErrorMessage(invalidBlobErrorMessage);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Cross storage account copy
        /// 8.20	Start-AzureStorageBlob Negative Functional Cases
        ///     2.	Copy the blob that the container doesn't exist
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.StartCopyBlob)]
        [TestCategory(CLITag.StartCopyBlob)]
        [TestCategory(CLITag.NodeJSFT)]
        public void StartCopyWithNotExistsContainerAndBlobTest()
        {
            string srcContainerName = Utility.GenNameString("copy");
            string destContainerName = Utility.GenNameString("dest");
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
                errorMessage = "The specified blob does not exist";
                validator = ExpectedStartsWithErrorMessage;
            }

            CloudBlobContainer container = blobUtil.CreateContainer(srcContainerName);
            Test.Assert(!agent.StartAzureStorageBlobCopy(srcContainerName, blobName, destContainerName, string.Empty), "Start copy should failed with not existing src container");
            validator(errorMessage);

            try
            {
                CloudBlobContainer srcContainer = blobUtil.CreateContainer(srcContainerName);
                Test.Assert(!agent.StartAzureStorageBlobCopy(srcContainerName, blobName, destContainerName, string.Empty), "Start copy should failed with not existing blob");
                validator(errorMessage);
                
                blobUtil.CreateRandomBlob(srcContainer, blobName);
                Test.Assert(!agent.StartAzureStorageBlobCopy(srcContainerName, blobName, destContainerName, string.Empty), "Start copy should failed with not existing dest container");
                if (lang == Language.PowerShell)
                {
                    string[] expectedErrorMsgs = new string[] { "The specified container does not exist.", "The remote server returned an error: (404) Not Found." };
                    ExpectedContainErrorMessage(expectedErrorMsgs);
                }
                else
                {
                    errorMessage = "The specified container does not exist";
                    validator(errorMessage);
                }
            }
            finally
            {
                blobUtil.RemoveContainer(srcContainerName);
            }
        }

        /// <summary>
        /// Cross storage account copy
        /// 8.20	Start-AzureStorageBlob Negative Functional Cases
        ///     4.	If the existing destination blob type is not same as BlobType parameter
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.StartCopyBlob)]
        [TestCategory(CLITag.StartCopyBlob)]
        [TestCategory(CLITag.NodeJSFT)]
        public void StartCopyWithMismatchedBlobTypeTest()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            CloudBlob blockBlob = blobUtil.CreateBlockBlob(container, Utility.GenNameString("block"));
            CloudBlob pageBlob = blobUtil.CreatePageBlob(container, Utility.GenNameString("page"));
            CloudBlob appendBlob = blobUtil.CreateAppendBlob(container, Utility.GenNameString("append"));

            string copyBlobError;
            Validator validator;
            if (lang == Language.PowerShell)
            {
                copyBlobError = "Blob type of source and destination must be the same.";
                validator = ExpectedEqualErrorMessage;
            }
            else
            {
                copyBlobError = "The blob type is invalid for this operation";
                validator = ExpectedStartsWithErrorMessage;
            }
            try
            {
                Test.Assert(!agent.StartAzureStorageBlobCopy(blockBlob, container.Name, pageBlob.Name), "Start copy should failed with mismatched blob type");
                validator(copyBlobError);
                Test.Assert(!agent.StartAzureStorageBlobCopy(container.Name, pageBlob.Name, container.Name, blockBlob.Name), "Start copy should failed with mismatched blob type");
                validator(copyBlobError);
                Test.Assert(!agent.StartAzureStorageBlobCopy(container.Name, pageBlob.Name, container.Name, appendBlob.Name), "Start copy should failed with mismatched blob type");
                validator(copyBlobError);
                Test.Assert(!agent.StartAzureStorageBlobCopy(container.Name, blockBlob.Name, container.Name, appendBlob.Name), "Start copy should failed with mismatched blob type");
                validator(copyBlobError);
                Test.Assert(!agent.StartAzureStorageBlobCopy(container.Name, appendBlob.Name, container.Name, blockBlob.Name), "Start copy should failed with mismatched blob type");
                validator(copyBlobError);
                Test.Assert(!agent.StartAzureStorageBlobCopy(container.Name, appendBlob.Name, container.Name, pageBlob.Name), "Start copy should failed with mismatched blob type");
                validator(copyBlobError);
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
            }
        }

        /// <summary>
        /// Copy to an existing blob without force parameter
        /// </summary>
        ////[TestMethod()]
        ////[TestCategory(Tag.Function)]
        ////[TestCategory(PsTag.Blob)]
        ////[TestCategory(PsTag.StartCopyBlob)]
        public void StartCopyToExistsBlobWithoutForce()
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            string srcBlobName = Utility.GenNameString("src");
            CloudBlob srcBlob = blobUtil.CreateRandomBlob(container, srcBlobName, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            string destBlobName = Utility.GenNameString("dest");
            CloudBlob destBlob = blobUtil.CreateRandomBlob(container, destBlobName, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            string filePath = FileUtil.GenerateOneTempTestFile();

            try
            {
                Test.Assert(!agent.StartAzureStorageBlobCopy(srcBlob, container.Name, destBlob.Name, null, false), "copy to existing blob without force parameter should fail");
                ExpectedContainErrorMessage(ConfirmExceptionMessage);
                srcBlob.FetchAttributes();
                destBlob.FetchAttributes();
                ExpectNotEqual(srcBlob.Properties.ContentMD5, destBlob.Properties.ContentMD5, "content md5");
            }
            finally
            {
                blobUtil.RemoveContainer(container);
                FileUtil.RemoveFile(filePath);
            }
        }

        private CloudBlob AssertCopyBlobCrossContainer(CloudBlob srcBlob, CloudBlobContainer destContainer, string destBlobName, object destContext, Func<bool> StartFunc = null)
        {
            if (StartFunc == null)
            {
                Test.Assert(agent.StartAzureStorageBlobCopy(srcBlob.Container.Name, srcBlob.Name, destContainer.Name, destBlobName, destContext), "blob copy should start successfully");
            }
            else
            {
                Test.Assert(StartFunc(), "blob copy should start successfully");
            }

            int expectedBlobCount = 1;
            Test.Assert(agent.Output.Count == expectedBlobCount, String.Format("Expected get {0} copy state, and actually it's {1}", expectedBlobCount, agent.Output.Count));
            
            string expectedBlobName = destBlobName;
            if (string.IsNullOrEmpty(expectedBlobName))
            {
                expectedBlobName = srcBlob.Name;
            }

            CloudBlob destBlob = null;
            string actualBlobName;
            string expectedSourceUri = CloudBlobUtil.ConvertCopySourceUri(srcBlob.Uri.ToString());
            if (lang == Language.PowerShell)
            {
                destBlob = (CloudBlob)agent.Output[0]["ICloudBlob"];
                destBlob.FetchAttributes();
                actualBlobName = destBlob.Name;

                Test.Assert(CloudBlobUtil.WaitForCopyOperationComplete(destBlob), "Copy Operation should finished"); 
                string sourceUri = destBlob.CopyState.Source.ToString();
                Test.Assert(sourceUri.StartsWith(expectedSourceUri), String.Format("source uri should start with {0}, and actually it's {1}", expectedSourceUri, sourceUri));
                Test.Assert(destBlob.Metadata.Count > 0, "destination blob should contain meta data");
                Test.Assert(destBlob.Metadata.SequenceEqual(srcBlob.Metadata), "Copied blob's meta data should be equal with origin metadata");
                Test.Assert(destBlob.Properties.ContentEncoding == srcBlob.Properties.ContentEncoding, String.Format("expected content encoding is {0}, and actually it's {1}", srcBlob.Properties.ContentEncoding, destBlob.Properties.ContentEncoding));
            }
            else
            {
                actualBlobName = (string)agent.Output[0]["blob"];

                string copyid = (string)agent.Output[0]["copyId"];
                Test.Assert(!string.IsNullOrEmpty(copyid), string.Format("Expected copy Id is not empty, and actually it's {0}", copyid));
            }

            Test.Assert(expectedBlobName == actualBlobName, string.Format("Expected destination blob name is {0}, and actually it's {1}", expectedBlobName, actualBlobName));
            
            return destBlob;
        }
    }
}
