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
using System.Linq;
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using System.Reflection;
using BlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;
using System.Threading.Tasks;
using System.Threading;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional test for Snapshot-AzureStorageBlob
    /// </summary>
    [TestClass]
    public class SnapshotBlob : TestBase
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void RemoveBlobClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Create a snapshot of a blob
        /// 8.14 Snapshot-AzureStorageBlob BVT Cases
        ///     1. Create a snapshot of a blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobSnapshot)]
        public void SnapshotRandomBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.SnapshotAzureStorageBlob(containerName, blobName), Utility.GenComparisonData("Snapshot-AzureStorageBlob", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        string url = (CommandAgent as NodeJSAgent).Output[0]["url"] as string;

                        DateTimeOffset snapshot = new DateTimeOffset((DateTime)(CommandAgent as NodeJSAgent).Output[0]["snapshot"]);
                        CloudBlob snapshotBlob = blobUtil.GetBlobReference(container, blobName, blob.BlobType, snapshot);
                        Test.Assert(snapshotBlob.IsSnapshot, "Expect the the instance is a snapshot");
                        Test.Assert(snapshotBlob.SnapshotQualifiedUri.ToString() == url, 
                            string.Format("Expect the URL to be {0} while get {1}", snapshotBlob.SnapshotQualifiedUri.AbsolutePath, url));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Create snapshot for a blob which is leased
        /// 8.14 Snapshot-AzureStorageBlob Positive Functional Cases
        ///     1. Create snapshot for a blob which is leased
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobSnapshot)]
        public void SnapshotLeasedBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                string leaseId = blob.AcquireLease(TimeSpan.FromMinutes(1), string.Empty);

                Test.Assert(CommandAgent.SnapshotAzureStorageBlob(containerName, blobName, leaseId), Utility.GenComparisonData("Snapshot-AzureStorageBlob", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        string url = (CommandAgent as NodeJSAgent).Output[0]["url"] as string;

                        DateTimeOffset snapshot = new DateTimeOffset((DateTime)(CommandAgent as NodeJSAgent).Output[0]["snapshot"]);
                        CloudBlob snapshotBlob = blobUtil.GetBlobReference(container, blobName, blob.BlobType, snapshot);
                        Test.Assert(snapshotBlob.IsSnapshot, "Expect the the instance is a snapshot");
                        Test.Assert(snapshotBlob.SnapshotQualifiedUri.ToString() == url,
                            string.Format("Expect the URL to be {0} while get {1}", snapshotBlob.SnapshotQualifiedUri.AbsolutePath, url));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Create snapshot for a blob which is being written
        /// 8.14 Snapshot-AzureStorageBlob Positive Functional Cases
        ///     2. Create snapshot for a blob which is being written
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobSnapshot)]
        public void SnapshotBlobInWriting()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task createTask = blobUtil.CreatePageBlobAsync(container, blobName, cts.Token, true);
                Test.Assert(CommandAgent.SnapshotAzureStorageBlob(containerName, blobName), Utility.GenComparisonData("Snapshot-AzureStorageBlob", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        string url = (CommandAgent as NodeJSAgent).Output[0]["url"] as string;

                        DateTimeOffset snapshot = new DateTimeOffset((DateTime)(CommandAgent as NodeJSAgent).Output[0]["snapshot"]);
                        CloudBlob snapshotBlob = blobUtil.GetBlobReference(container, blobName, BlobType.PageBlob, snapshot);
                        Test.Assert(snapshotBlob.IsSnapshot, "Expect the the instance is a snapshot");
                        Test.Assert(snapshotBlob.SnapshotQualifiedUri.ToString() == url,
                            string.Format("Expect the URL to be {0} while get {1}", snapshotBlob.SnapshotQualifiedUri.AbsolutePath, url));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                cts.Cancel();
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Create snapshot for a lease blob without lease ID
        /// 8.14 Snapshot-AzureStorageBlob Positive Functional Cases
        ///     3. Create snapshot for a lease blob without lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobSnapshot)]
        public void SnapshotLeasedBlobWithoutLeaseID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                blob.AcquireLease(TimeSpan.FromMinutes(1), string.Empty);

                Test.Assert(CommandAgent.SnapshotAzureStorageBlob(containerName, blobName), Utility.GenComparisonData("Snapshot-AzureStorageBlob", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        string url = (CommandAgent as NodeJSAgent).Output[0]["url"] as string;

                        DateTimeOffset snapshot = new DateTimeOffset((DateTime)(CommandAgent as NodeJSAgent).Output[0]["snapshot"]);
                        CloudBlob snapshotBlob = blobUtil.GetBlobReference(container, blobName, blob.BlobType, snapshot);
                        Test.Assert(snapshotBlob.IsSnapshot, "Expect the the instance is a snapshot");
                        Test.Assert(snapshotBlob.SnapshotQualifiedUri.ToString() == url,
                            string.Format("Expect the URL to be {0} while get {1}", snapshotBlob.SnapshotQualifiedUri.AbsolutePath, url));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Create snapshot for a blob which doesn't exist
        /// 8.14 Snapshot-AzureStorageBlob Negative Functional Cases
        ///     1. Create snapshot for a blob doesn't exist
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobSnapshot)]
        public void SnapshotNonExistingBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");

            Test.Assert(!CommandAgent.SnapshotAzureStorageBlob(containerName, blobName), Utility.GenComparisonData("Snapshot-AzureStorageBlob", false));
            CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, blobName, containerName);
        }

        /// <summary>
        /// Create snapshot for a blob which is leased
        /// 8.14 Snapshot-AzureStorageBlob Negative Functional Cases
        ///     1. Create snapshot for a blob which is leased
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobSnapshot)]
        public void SnapshotLeaseBlobWithWrongLeaseID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                blob.AcquireLease(TimeSpan.FromMinutes(1), string.Empty);
                string wrongLeaseId = Guid.NewGuid().ToString();

                Test.Assert(!CommandAgent.SnapshotAzureStorageBlob(containerName, blobName, wrongLeaseId), Utility.GenComparisonData("Snapshot-AzureStorageBlob", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }
    }
}

