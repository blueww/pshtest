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
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional test for Snapshot-AzureStorageBlob
    /// </summary>
    [TestClass]
    public class RenewLeaseTest : TestBase
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void RenewLeaseTestClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

#if !DOTNET5_4
        /// <summary>
        /// Renew a container and access it by Lease ID
        /// 8.69 RenewLease-AzureStorageContainer/Blob BVT Cases
        ///     1. Renew a container and access it by Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewContainerLease()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string leaseId = string.Empty;

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, string.Empty), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        leaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.RenewLease(containerName, string.Empty, leaseId), Utility.GenComparisonData("Renew Lease", true));
                        Test.Assert(CommandAgent.RemoveAzureStorageContainer(containerName, leaseId), Utility.GenComparisonData("Remove container", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, leaseId);
            }
        }

        /// <summary>
        /// Renew a blob and access it by Lease ID
        /// 8.69 RenewLease-AzureStorageContainer/Blob BVT Cases
        ///     2. Renew a blob and access it by Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewBlobLease()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string leaseId = string.Empty;

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        leaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.RenewLease(containerName, blobName, leaseId), Utility.GenComparisonData("Renew Lease", true));
                        Test.Assert(CommandAgent.SnapshotAzureStorageBlob(containerName, blobName, leaseId), Utility.GenComparisonData("Snapshot blob", true));
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
        /// Renew a lease of a blob/container with infinite duration
        /// 8.69 RenewLease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Renew a lease of a blob/container with duration
        ///         a. Not set (Infinite)
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewLeaseWithInfiniteDuration()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string containerLease = string.Empty;

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.RenewLease(containerName, null, containerLease), Utility.GenComparisonData("Renew Lease", true));

                        AccessCondition condition = new AccessCondition()
                        {
                            LeaseId = containerLease
                        };
                        container.SetMetadata(condition);

                        Test.Assert((CommandAgent as NodeJSAgent).SetAzureStorageContainerACL(containerName, BlobContainerPublicAccessType.Off, containerLease),
                            Utility.GenComparisonData("Set container ACL with lease ID", true));
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

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.RenewLease(containerName, blobName, blobLease), Utility.GenComparisonData("Renew Lease", true));

                        AccessCondition condition = new AccessCondition()
                        {
                            LeaseId = blobLease
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
                blobUtil.RemoveContainer(containerName, containerLease);
            }
        }

        /// <summary>
        /// Renew a lease of a blob/container with a fixed duration
        /// 8.69 RenewLease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Renew a lease of a blob/container with duration
        ///         b. Random value between 15-60
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewLeaseWithDurationInSeconds()
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

                        Test.Assert(CommandAgent.RenewLease(containerName, null, containerLease), Utility.GenComparisonData("Renew Lease", true));

                        AccessCondition condition = new AccessCondition()
                        {
                            LeaseId = containerLease
                        };
                        container.SetMetadata(condition);

                        Test.Assert((CommandAgent as NodeJSAgent).SetAzureStorageContainerACL(containerName, BlobContainerPublicAccessType.Off, containerLease),
                            Utility.GenComparisonData("Set container ACL with lease ID", true));
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

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: duration), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.RenewLease(containerName, blobName, blobLease), Utility.GenComparisonData("Renew Lease", true));

                        AccessCondition condition = new AccessCondition()
                        {
                            LeaseId = blobLease
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
                blobUtil.RemoveContainer(containerName, containerLease);
            }
        }

        /// <summary>
        /// Renew a lease of a blob/container with a fixed duration by SAS
        /// 8.69 RenewLease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Renew a lease of a blob/container with duration by SAS
        ///         b. Random value between 15-60
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewLeaseBySAS()
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

                        Test.Assert(CommandAgent.RenewLease(containerName, null, containerLease), Utility.GenComparisonData("Renew Lease", true));

                        AccessCondition condition = new AccessCondition()
                        {
                            LeaseId = containerLease
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

                        // Renew with container SAS
                        CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);

                        Test.Assert(CommandAgent.RenewLease(containerName, blobName, blobLease), Utility.GenComparisonData("Renew Lease", true));

                        AccessCondition condition = new AccessCondition()
                        {
                            LeaseId = blobLease
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
                blobUtil.RemoveContainer(containerName, containerLease);
            }
        }

        /// <summary>
        /// Renew a expired lease of a blob/container
        /// 8.69 RenewLease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Renew a expired lease of a blob/container    
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewExpiredLease()
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

                Test.Assert(CommandAgent.RenewLease(containerName, null, containerLease), Utility.GenComparisonData("Renew Container Lease", true));
                Test.Assert(CommandAgent.RenewLease(containerName, blobName, blobLease), Utility.GenComparisonData("Renew Blob Lease", true));

                bool throwException = false;
                try
                {
                    container.Delete();
                }
                catch (StorageException se)
                {
                    throwException = true;
                    Test.Info(string.Format("Expected: {0} error: {1}", MethodBase.GetCurrentMethod().Name, se.Message));
                }

                Test.Assert(throwException, "Should ever throw exception");
                throwException = false;

                try
                {
                    blob.SetMetadata();
                }
                catch (StorageException se)
                {
                    throwException = true;
                    Test.Info(string.Format("Expected: {0} error: {1}", MethodBase.GetCurrentMethod().Name, se.Message));
                }

                Test.Assert(throwException, "Should ever throw exception");
                throwException = false;

                try
                {
                    AccessCondition condition = new AccessCondition()
                    {
                        LeaseId = blobLease
                    };

                    blob.SetMetadata(condition);
                }
                catch (StorageException se)
                {
                    Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, se.Message));
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLease);
            }
        }

        /// <summary>
        /// Renew the lease a non-existing container
        /// 8.69 RenewLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Renew the lease a non-existing container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewLeaseOnNonExistingContainer()
        {
            string containerName = Utility.GenNameString("container");
            string leaseId = Guid.NewGuid().ToString();

            Test.Assert(!CommandAgent.RenewLease(containerName, null, leaseId), Utility.GenComparisonData("Renew Container Lease", false));
            CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
        }

        /// <summary>
        /// Renew the lease a non-existing blob
        /// 8.69 RenewLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Renew the lease a non-existing blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewLeaseOnNonExistingBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string leaseId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(!CommandAgent.RenewLease(containerName, blobName, leaseId), Utility.GenComparisonData("Renew Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Renew the lease against a not leased container
        /// 8.69 RenewLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     2.  Renew the lease against a not leased container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewNotLeasedContainer()
        {
            string containerName = Utility.GenNameString("container");
            string leaseId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(!CommandAgent.RenewLease(containerName, null, leaseId), Utility.GenComparisonData("Renew Container Lease", false)); 
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Renew the lease against a not leased Blob
        /// 8.69 RenewLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     2.  Renew the lease against a not leased Blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewNotLeasedBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string leaseId = Guid.NewGuid().ToString();
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                Test.Assert(!CommandAgent.RenewLease(containerName, blobName, leaseId), Utility.GenComparisonData("Renew Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Renew the lease with invalid lease ID
        /// 8.69 RenewLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3.  Renew the lease with invalid lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewWithInvalidLeaseID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string invalidLeaseId = "1234567890poiuyytrewq";
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                Test.Assert(!CommandAgent.RenewLease(containerName, null, invalidLeaseId), Utility.GenComparisonData("Renew Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseId);

                Test.Assert(!CommandAgent.RenewLease(containerName, blobName, invalidLeaseId), Utility.GenComparisonData("Renew Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseId);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Renew the lease with unmatched lease ID
        /// 8.69 RenewLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3.  Renew the lease with unmatched lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewWithUnmatchLeaseID()
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

                Test.Assert(!CommandAgent.RenewLease(containerName, null, wrongContainerLeaseId), Utility.GenComparisonData("Renew Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                Test.Assert(!CommandAgent.RenewLease(containerName, blobName, wrongBlobLeaseId), Utility.GenComparisonData("Renew Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLeaseId);
            }
        }

        /// <summary>
        /// Renew the lease without enough permission
        /// 8.69 RenewLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     4.  Renew the lease without enough permission
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void RenewWithoutEnoughPermission()
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
                Test.Assert(!CommandAgent.RenewLease(containerName, null, containerLeaseId), Utility.GenComparisonData("Renew Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);
                Test.Assert(!CommandAgent.RenewLease(containerName, blobName, blobLeaseId), Utility.GenComparisonData("Renew Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLeaseId);
            }
        }
#endif
    }
}