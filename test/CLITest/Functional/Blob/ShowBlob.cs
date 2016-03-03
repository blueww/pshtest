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
using System.Reflection;
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using BlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional test for show blob (only for NodeJS)
    /// </summary>
    [TestClass]
    public class ShowBlob : TestBase
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
        /// get blob with lease
        /// 8.12	show blob Positive Functional Cases (only for nodejs)
        ///     9.	Validate that the lease data could be listed correctly
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        public void ShowBlobWithLease()
        {
            string containerName = Utility.GenNameString("container");
            string pageBlobName = Utility.GenNameString("page");
            string blockBlobName = Utility.GenNameString("block");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            NodeJSAgent nodejsAgent = (NodeJSAgent)CommandAgent;
            try
            {
                CloudBlob pageBlob = blobUtil.CreatePageBlob(container, pageBlobName);
                CloudBlob blockBlob = blobUtil.CreateBlockBlob(container, blockBlobName);
                ((CloudPageBlob)pageBlob).AcquireLease(null, string.Empty);
                ((CloudBlockBlob)blockBlob).AcquireLease(null, string.Empty);
                pageBlob.FetchAttributes();
                blockBlob.FetchAttributes();

                Test.Assert(nodejsAgent.ShowAzureStorageBlob(pageBlobName, containerName), Utility.GenComparisonData("show blob with lease", true));
                nodejsAgent.OutputValidation(new List<CloudBlob>() { pageBlob });

                Test.Assert(nodejsAgent.ShowAzureStorageBlob(blockBlobName, containerName), Utility.GenComparisonData("show blob with lease", true));
                nodejsAgent.OutputValidation(new List<CloudBlob>() { blockBlob });
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// show blob with lease
        /// 8.12	show blob Positive Functional Cases (only for nodejs)
        ///     10.	Write Metadata to the specific blob Get the Metadata from the specific blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        public void ShowBlobWithMetadata()
        {
            string containerName = Utility.GenNameString("container");
            string pageBlobName = Utility.GenNameString("page");
            string blockBlobName = Utility.GenNameString("block");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            NodeJSAgent nodejsAgent = (NodeJSAgent)CommandAgent;
            try
            {
                CloudBlob pageBlob = blobUtil.CreatePageBlob(container, pageBlobName);
                CloudBlob blockBlob = blobUtil.CreateBlockBlob(container, blockBlobName);

                int count = Utility.GetRandomTestCount();
                for (int i = 0; i < count; i++)
                {
                    pageBlob.Metadata.Add(Utility.GenNameString("ShowBlobWithMetadata"), Utility.GenNameString("ShowBlobWithMetadata"));
                    pageBlob.SetMetadata();
                    blockBlob.Metadata.Add(Utility.GenNameString("ShowBlobWithMetadata"), Utility.GenNameString("ShowBlobWithMetadata"));
                    blockBlob.SetMetadata();
                }

                Test.Assert(nodejsAgent.ShowAzureStorageBlob(pageBlobName, containerName), Utility.GenComparisonData("show blob with metadata", true));
                Test.Assert(nodejsAgent.Output.Count == 1, String.Format("Expect to retrieve {0} blobs, but retrieved {1} blobs", 1, nodejsAgent.Output.Count));
                nodejsAgent.OutputValidation(new List<CloudBlob>() { pageBlob });

                Test.Assert(nodejsAgent.ShowAzureStorageBlob(blockBlobName, containerName), Utility.GenComparisonData("show blob with metadata", true));
                Test.Assert(nodejsAgent.Output.Count == 1, String.Format("Expect to retrieve {0} blobs, but retrieved {1} blobs", 1, nodejsAgent.Output.Count));
                nodejsAgent.OutputValidation(new List<CloudBlob>() { blockBlob });
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// 8.9	Show Blob 
        ///     8.	Show a blob name with special chars
        /// </summary>
        [TestMethod()]
        [TestCategory(PsTag.Blob)]
        [TestCategory(PsTag.SetBlobContent)]
        [TestCategory(CLITag.NodeJSFT)]
        public void ShowBlobWithSpecialChars()
        {
            ShowBlobWithSpecialChars(BlobType.BlockBlob);
            ShowBlobWithSpecialChars(BlobType.PageBlob);
        }

        public void ShowBlobWithSpecialChars(BlobType blobType)
        {
            CloudBlobContainer container = blobUtil.CreateContainer();
            string blobName = SpecialChars;
            CloudBlob blob = blobUtil.CreateBlob(container, blobName, blobType);

            NodeJSAgent nodejsAgent = (NodeJSAgent)CommandAgent;

            try
            {
                Test.Assert(nodejsAgent.ShowAzureStorageBlob(blobName, container.Name), "show blob name with special chars should succeed");
                blob.FetchAttributes();

                nodejsAgent.OutputValidation(new List<CloudBlob>() { blob });
            }
            finally
            {
                blobUtil.RemoveContainer(container.Name);
            }
        }

        /// <summary>
        /// get blob in subdirectory
        /// 8.12	Show-AzureStorageBlob Negative Functional Cases
        ///     1.	Show a non-existing blob 
        /// </summary>
        [TestMethod()]
        [TestCategory(PsTag.Blob)]
        [TestCategory(CLITag.NodeJSFT)]
        public void ShowNonExistingBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob", 12);
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            NodeJSAgent nodejsAgent = (NodeJSAgent)CommandAgent;
            try
            {
                string notExistingBlobName = "notexistingblob";
                string BLOB_NAME = Utility.GenNameString("nonexisting");

                Test.Assert(!nodejsAgent.ShowAzureStorageBlob(notExistingBlobName, containerName), Utility.GenComparisonData("show blob with not existing blob", false));
                nodejsAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, notExistingBlobName, containerName);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }
    }
}
