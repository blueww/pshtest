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
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional test for Snapshot-AzureStorageBlob
    /// </summary>
    [TestClass]
    public class ChangeLeaseTest : TestBase
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void ChangeLeaseTestClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Change a container and access it by Lease ID
        /// 8.70 ChangeLease-AzureStorageContainer/Blob BVT Cases
        ///     1. Change a container and access it by Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeContainerLease()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string leaseId = string.Empty;
            string porposedId = Guid.NewGuid().ToString();

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, string.Empty), Utility.GenComparisonData("Acquire Container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        leaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.ChangeLease(containerName, string.Empty, leaseId, porposedId), Utility.GenComparisonData("Change Container Lease", true));

                        Test.Assert(!CommandAgent.RemoveAzureStorageContainer(containerName, leaseId), Utility.GenComparisonData("Remove container", false));
                        Test.Assert(CommandAgent.RemoveAzureStorageContainer(containerName, porposedId), Utility.GenComparisonData("Remove container", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, porposedId);
            }
        }

        /// <summary>
        /// Change a blob and access it by Lease ID
        /// 8.70 ChangeLease-AzureStorageContainer/Blob BVT Cases
        ///     2. Change a blob and access it by Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeBlobLease()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string leaseId = string.Empty;
            string porposedId = Guid.NewGuid().ToString();

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire blob Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        leaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.ChangeLease(containerName, blobName, leaseId, porposedId), Utility.GenComparisonData("Change blob Lease", true));

                        Test.Assert(!CommandAgent.SnapshotAzureStorageBlob(containerName, blobName, leaseId), Utility.GenComparisonData("Snapshot blob", false));
                        Test.Assert(CommandAgent.SnapshotAzureStorageBlob(containerName, blobName, porposedId), Utility.GenComparisonData("Snapshot blob", true));
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
        /// Change a lease of a blob/container with a fixed duration by SAS
        /// 8.70 ChangeLease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Change a lease of a blob/container with duration
        ///         b. Random value between 15-60
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeLeaseBySAS()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string containerLease = string.Empty;
            string porposedId = Guid.NewGuid().ToString();

            Random random = new Random();
            int duration = random.Next(15, 60);

            try
            {
                string containerSasToken = string.Empty;
                string accountSasToken = string.Empty;

                // Create a container SAS
                Test.Assert(CommandAgent.NewAzureStorageContainerSAS(containerName, string.Empty, "w", DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromMinutes(5)),
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

                        Test.Assert(CommandAgent.ChangeLease(containerName, null, containerLease, porposedId), Utility.GenComparisonData("Change Lease", true));

                        AccessCondition condition = new AccessCondition()
                        {
                            LeaseId = porposedId
                        };
                        container.SetMetadata(condition);
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

                        // Change with container SAS
                        CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);

                        Test.Assert(CommandAgent.ChangeLease(containerName, blobName, blobLease, porposedId), Utility.GenComparisonData("Change Lease", true));

                        AccessCondition condition = new AccessCondition()
                        {
                            LeaseId = porposedId
                        };
                        blob.SetMetadata(condition);
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, porposedId);
            }
        }

        /// <summary>
        /// Change a container and access it by Lease ID
        /// 8.70 ChangeLease-AzureStorageContainer/Blob BVT Cases
        ///     1. Change a container and access it by Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeLeaseWithSameID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string leaseId = string.Empty;
            string porposedId = leaseId;

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire blob Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        leaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                        porposedId = leaseId;

                        Test.Assert(CommandAgent.ChangeLease(containerName, blobName, leaseId, porposedId), Utility.GenComparisonData("Change blob Lease", true));
                        Test.Assert(CommandAgent.SnapshotAzureStorageBlob(containerName, blobName, porposedId), Utility.GenComparisonData("Snapshot blob", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                Test.Assert(CommandAgent.AcquireLease(containerName, string.Empty), Utility.GenComparisonData("Acquire container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        leaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                        porposedId = leaseId;

                        Test.Assert(CommandAgent.ChangeLease(containerName, string.Empty, leaseId, porposedId), Utility.GenComparisonData("Change Lease", true));
                        Test.Assert(CommandAgent.RemoveAzureStorageContainer(containerName, porposedId), Utility.GenComparisonData("Remove container", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, porposedId);
            }
        }

        /// <summary>
        /// Change the lease a non-existing container
        /// 8.70 ChangeLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Change the lease a non-existing container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeLeaseOnNonExistingContainer()
        {
            string containerName = Utility.GenNameString("container");
            string leaseId = Guid.NewGuid().ToString();
            string proposedId = Guid.NewGuid().ToString();

            Test.Assert(!CommandAgent.ChangeLease(containerName, null, leaseId, proposedId), Utility.GenComparisonData("Change Container Lease", false));
            CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
        }

        /// <summary>
        /// Change the lease a non-existing blob
        /// 8.70 ChangeLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Change the lease a non-existing blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeLeaseOnNonExistingBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string leaseId = Guid.NewGuid().ToString();
            string proposedId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(!CommandAgent.ChangeLease(containerName, blobName, leaseId, proposedId), Utility.GenComparisonData("Change Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Change the lease with invalid lease ID
        /// 8.70 ChangeLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3.  Change the lease with invalid lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeWithInvalidLeaseID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string containerLease = string.Empty;
            string blobLease = string.Empty;
            string invalidLeaseId = "1234567890poiuyytrewq";
            string proposedId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, string.Empty, duration: 30), Utility.GenComparisonData("Acquire container Lease", true));
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
                Test.Assert(!CommandAgent.ChangeLease(containerName, null, invalidLeaseId, proposedId), Utility.GenComparisonData("Change Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseId);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire blob Lease", true));
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

                Test.Assert(!CommandAgent.ChangeLease(containerName, blobName, invalidLeaseId, proposedId), Utility.GenComparisonData("Change Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseId);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLease);
            }
        }

        /// <summary>
        /// Change the lease with unmatched lease ID
        /// 8.70 ChangeLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3.  Change the lease with unmatched lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeWithUnmatchLeaseID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string containerLeaseId = string.Empty;
            string blobLeaseId = string.Empty;
            string proposedId = Guid.NewGuid().ToString();
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

                Test.Assert(!CommandAgent.ChangeLease(containerName, null, wrongContainerLeaseId, proposedId), Utility.GenComparisonData("Change Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                Test.Assert(!CommandAgent.ChangeLease(containerName, blobName, wrongBlobLeaseId, proposedId), Utility.GenComparisonData("Change Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLeaseId);
            }
        }

        /// <summary>
        /// Change the lease with invalid porposed lease ID
        /// 8.70 ChangeLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3.  Change the lease with invalid porposed lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeWithInvalidProposedLeaseID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string containerLease = string.Empty;
            string blobLease = string.Empty;
            string invalidLeaseId = "1234567890poiuyytrewq";
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, string.Empty), Utility.GenComparisonData("Acquire container Lease", true));
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
                Test.Assert(!CommandAgent.ChangeLease(containerName, null, containerLease, invalidLeaseId), Utility.GenComparisonData("Change Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseId);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire blob Lease", true));
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

                Test.Assert(!CommandAgent.ChangeLease(containerName, blobName, blobLease, invalidLeaseId), Utility.GenComparisonData("Change Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseId);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLease);
            }
        }

        /// <summary>
        /// Change the lease without enough permission
        /// 8.70 ChangeLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     4.  Change the lease without enough permission
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void ChangeWithoutEnoughPermission()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string containerLeaseId = string.Empty;
            string blobLeaseId = string.Empty;
            string proposedId = Guid.NewGuid().ToString();
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
                Test.Assert(!CommandAgent.ChangeLease(containerName, null, containerLeaseId, proposedId), Utility.GenComparisonData("Change Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);
                Test.Assert(!CommandAgent.ChangeLease(containerName, blobName, blobLeaseId, proposedId), Utility.GenComparisonData("Change Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLeaseId);
            }
        }
    }
}

