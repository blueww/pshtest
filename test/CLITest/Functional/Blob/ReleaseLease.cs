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
using System.Reflection;
using System.Threading;
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageBlobType = Microsoft.WindowsAzure.Storage.Blob.BlobType;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional test for Snapshot-AzureStorageBlob
    /// </summary>
    [TestClass]
    public class ReleaseLeaseTest : TestBase
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void ReleaseLeaseTestClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Release a container lease and access it wihtout Lease ID
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob BVT Cases
        ///     1. Release a container lease and access it wihtout Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseContainerLease()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string leaseId = string.Empty;

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, string.Empty), Utility.GenComparisonData("Acquire Container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        leaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.ReleaseLease(containerName, string.Empty, leaseId), Utility.GenComparisonData("Release Container Lease", true));
                        Test.Assert(!CommandAgent.RemoveAzureStorageContainer(containerName, leaseId), Utility.GenComparisonData("Remove container", false));
                        CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                        Test.Assert(CommandAgent.RemoveAzureStorageContainer(containerName), Utility.GenComparisonData("Remove container", true));
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
        /// Release a blob lease and access it wihtout Lease ID
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob BVT Cases
        ///     2. Release a blob lease and access it wihtout Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseBlobLease()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string leaseId = string.Empty;

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Blob Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        leaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.ReleaseLease(containerName, blobName, leaseId), Utility.GenComparisonData("Release Blob Lease", true));
                        Test.Assert(!CommandAgent.SnapshotAzureStorageBlob(containerName, blobName, leaseId), Utility.GenComparisonData("Snapshot blob", false));
                        CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                        Test.Assert(CommandAgent.SnapshotAzureStorageBlob(containerName, blobName), Utility.GenComparisonData("Snapshot blob", true));
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
        /// Lease again after releasing a lease of a blob/container with infinite duration
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Lease again after releasing a lease of a blob/container with infinite duration
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseAfterReleaseLease()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");

            string destContainerName = Utility.GenNameString("container");
            string destBlobName = Utility.GenNameString("blob");

            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlobContainer destContainer = blobUtil.CreateContainer(destContainerName);
            string containerLease = string.Empty;

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.ReleaseLease(containerName, null, containerLease), Utility.GenComparisonData("Release Lease", true));

                        Test.Assert(!(CommandAgent as NodeJSAgent).SetAzureStorageContainerACL(containerName, BlobContainerPublicAccessType.Off, containerLease),
                            Utility.GenComparisonData("Set container ACL with lease ID", false));
                        CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }

                    Test.Assert(CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Container Lease", true));
                    try
                    {
                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                string blobLease = string.Empty;
                string destBlobLease = string.Empty;
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName, StorageBlobType.BlockBlob);
                CloudBlob destBlob = blobUtil.CreateRandomBlob(destContainer, destBlobName, StorageBlobType.BlockBlob);
                blob.Metadata["test1"] = "m1";
                blob.Metadata["test2"] = "m2";

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Source Blob Lease", true));
                if (lang == Language.NodeJS)
                {
                    try
                    {
                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                Test.Assert(CommandAgent.AcquireLease(destContainerName, destBlobName), Utility.GenComparisonData("Acquire Dest Blob Lease", true));
                if (lang == Language.NodeJS)
                {
                    try
                    {
                        destBlobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                Test.Assert(CommandAgent.StartAzureStorageBlobCopy(containerName, blobName, destContainerName, destBlobName, blobLease, destBlobLease),
                    Utility.GenComparisonData("Start copy blob", true));

                Test.Assert(CommandAgent.ReleaseLease(containerName, blobName, blobLease), Utility.GenComparisonData("Release Source Blob Lease", true));
                Test.Assert(CommandAgent.ReleaseLease(destContainerName, destBlobName, destBlobLease), Utility.GenComparisonData("Release Dest Blob Lease", true));

                Test.Assert(CommandAgent.StartAzureStorageBlobCopy(containerName, blobName, destContainerName, destBlobName),
                    Utility.GenComparisonData("Start copy blob", true));
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLease);
            }
        }

        /// <summary>
        /// Release a lease of a blob/container with a fixed duration by SAS
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Release a lease of a blob/container with duration by SAS
        ///         b. Random value between 15-60
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseLeaseBySAS()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string containerLease = string.Empty;

            Random random = new Random();
            int duration = random.Next(15, 60);

            try
            {
                string containerSasToken = string.Empty;
                string accountSasToken = string.Empty;

                // Create a container SAS
                Test.Assert(CommandAgent.NewAzureStorageContainerSAS(containerName, string.Empty, "rw", DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromMinutes(5)),
                    Utility.GenComparisonData("container sas create", true));
                if (lang == Language.NodeJS)
                {
                    containerSasToken = (CommandAgent as NodeJSAgent).Output[0]["sas"] as string;
                }

                // Use an account SAS
                SharedAccessAccountPolicy policy = new SharedAccessAccountPolicy()
                {
                    Permissions = SharedAccessAccountPermissions.Write,
                    ResourceTypes = SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object,
                    Services = SharedAccessAccountServices.Blob,
                    SharedAccessStartTime = DateTimeOffset.UtcNow,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5)
                };

                accountSasToken = Utility.GenerateAccountSAS(policy);

                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, accountSasToken);

                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: duration), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.ReleaseLease(containerName, null, containerLease), Utility.GenComparisonData("Release Lease", true));

                        bool throwException = false;
                        try
                        {
                            AccessCondition condition = new AccessCondition()
                            {
                                LeaseId = containerLease
                            };
                            container.SetMetadata(condition);
                        }
                        catch (StorageException se)
                        {
                            throwException = true;
                            Test.Info(string.Format("Expected: {0} error: {1}", MethodBase.GetCurrentMethod().Name, se.Message));
                        }
                        Test.Assert(throwException, "Should ever throw exception");
                        throwException = false;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                string blobLease = string.Empty;
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                // Acquire with account SAS
                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: duration), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        // Release with container SAS
                        CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);

                        Test.Assert((CommandAgent as NodeJSAgent).ShowAzureStorageBlob(blobName, containerName, blobLease), Utility.GenComparisonData("Show Blob With Lease", true));

                        Test.Assert(CommandAgent.ReleaseLease(containerName, blobName, blobLease), Utility.GenComparisonData("Release Lease", true));

                        Test.Assert((CommandAgent as NodeJSAgent).ShowAzureStorageBlob(blobName, containerName), Utility.GenComparisonData("Show Blob After Release", true));
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
        /// Release a expired lease of a blob/container
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Positive Functional Cases
        ///     3. Release a expired lease of a blob/container    
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseExpiredLease()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string containerLease = string.Empty;

            Random random = new Random();
            int duration = random.Next(15, 60);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: duration), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                string blobLease = string.Empty;
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                blob.Metadata["test1"] = "m1";
                blob.Metadata["test2"] = "m2";

                // Acquire with account SAS
                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: duration), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                // Wait for the lease expired
                Thread.Sleep(duration * 1000);

                Test.Assert(CommandAgent.ReleaseLease(containerName, null, containerLease), Utility.GenComparisonData("Release Container Lease", true));
                Test.Assert(CommandAgent.ReleaseLease(containerName, blobName, blobLease), Utility.GenComparisonData("Release Blob Lease", true));
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Release the lease a non-existing container
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Release the lease a non-existing container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseLeaseOnNonExistingContainer()
        {
            string containerName = Utility.GenNameString("container");
            string leaseId = Guid.NewGuid().ToString();

            Test.Assert(!CommandAgent.ReleaseLease(containerName, null, leaseId), Utility.GenComparisonData("Release Container Lease", false));
            CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
        }

        /// <summary>
        /// Release the lease a non-existing blob
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Release the lease a non-existing blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseLeaseOnNonExistingBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string leaseId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(!CommandAgent.ReleaseLease(containerName, blobName, leaseId), Utility.GenComparisonData("Release Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Release the lease against a not leased container
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     2.  Release the lease against a not leased container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseNotLeasedContainer()
        {
            string containerName = Utility.GenNameString("container");
            string leaseId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(!CommandAgent.ReleaseLease(containerName, null, leaseId), Utility.GenComparisonData("Release Container Lease", false)); 
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Release the lease against a not leased Blob
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     2.  Release the lease against a not leased Blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseNotLeasedBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string leaseId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                Test.Assert(!CommandAgent.ReleaseLease(containerName, blobName, leaseId), Utility.GenComparisonData("Release Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Release the lease with invalid lease ID
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3.  Release the lease with invalid lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseWithInvalidLeaseID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string invalidLeaseId = "1234567890poiuyytrewq";
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                Test.Assert(!CommandAgent.ReleaseLease(containerName, null, invalidLeaseId), Utility.GenComparisonData("Release Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseId);

                Test.Assert(!CommandAgent.ReleaseLease(containerName, blobName, invalidLeaseId), Utility.GenComparisonData("Release Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseId);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Release the lease with unmatched lease ID
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3.  Release the lease with unmatched lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseWithUnmatchLeaseID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string containerLeaseId = string.Empty;
            string wrongContainerLeaseId = Guid.NewGuid().ToString();
            string wrongBlobLeaseId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: 30), Utility.GenComparisonData("Acquire Container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        containerLeaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: 30), Utility.GenComparisonData("Acquire Container Lease", true));

                Test.Assert(!CommandAgent.ReleaseLease(containerName, null, wrongContainerLeaseId), Utility.GenComparisonData("Release Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                Test.Assert(!CommandAgent.ReleaseLease(containerName, blobName, wrongBlobLeaseId), Utility.GenComparisonData("Release Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLeaseId);
            }
        }

        /// <summary>
        /// Release the lease with out enough permission
        /// 8.71 ReleaseLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     4.  Release the lease with unmatched lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ReleaseWithoutEnoughPermission()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string containerLeaseId = string.Empty;
            string blobLeaseId = string.Empty;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: 30), Utility.GenComparisonData("Acquire Container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        containerLeaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: 30), Utility.GenComparisonData("Acquire blob Lease", true));
                if (lang == Language.NodeJS)
                {
                    try
                    {
                        blobLeaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                string containerSasToken = string.Empty;
                string accountSasToken = string.Empty;

                // Create a container SAS
                Test.Assert(CommandAgent.NewAzureStorageContainerSAS(containerName, string.Empty, "rl", DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromMinutes(5)),
                    Utility.GenComparisonData("container sas create", true));
                if (lang == Language.NodeJS)
                {
                    containerSasToken = (CommandAgent as NodeJSAgent).Output[0]["sas"] as string;
                }

                // Create an account SAS
                SharedAccessAccountPolicy policy = new SharedAccessAccountPolicy()
                {
                    Permissions = SharedAccessAccountPermissions.Read,
                    ResourceTypes = SharedAccessAccountResourceTypes.Container,
                    Services = SharedAccessAccountServices.Blob,
                    SharedAccessStartTime = DateTimeOffset.UtcNow,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5)
                };

                accountSasToken = Utility.GenerateAccountSAS(policy);

                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, accountSasToken);
                Test.Assert(!CommandAgent.ReleaseLease(containerName, null, containerLeaseId), Utility.GenComparisonData("Release Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);
                Test.Assert(!CommandAgent.ReleaseLease(containerName, blobName, blobLeaseId), Utility.GenComparisonData("Release Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLeaseId);
            }
        }
    }
}