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
    public class AcquireLeaseTest : TestBase
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void AcquireLeaseTestClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Lease a container and access it by Lease ID
        /// 8.68 Lease-AzureStorageContainer/Blob BVT Cases
        ///     1. Lease a container and access it by Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseContainer()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string lease = string.Empty;

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, string.Empty), Utility.GenComparisonData("Acquire Lease", true));

#if !DOTNET5_4
                if (lang == Language.NodeJS)
                {
                    try
                    {
                        lease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert((CommandAgent as NodeJSAgent).ShowAzureStorageContainer(containerName), Utility.GenComparisonData("Show container without lease ID", true));
                        Test.Assert((CommandAgent as NodeJSAgent).ShowAzureStorageContainer(containerName, lease), Utility.GenComparisonData("Show container with lease ID", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }
#endif
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, lease);
            }
        }

#if !DOTNET5_4
        /// <summary>
        /// Lease a blob with a proposed id and access it by Lease ID
        /// 8.68 Lease-AzureStorageContainer/Blob BVT Cases
        ///     2. Lease a blob with a proposed id and access it by Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseBlobWithProposedId()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                string proposedId = Guid.NewGuid().ToString();
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, proposedId), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        string lease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert((CommandAgent as NodeJSAgent).ShowAzureStorageBlob(blobName, containerName), Utility.GenComparisonData("Show blob without lease ID", true));
                        Test.Assert((CommandAgent as NodeJSAgent).ShowAzureStorageBlob(blobName, containerName, lease), Utility.GenComparisonData("Show blob with lease ID", true));
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
        /// Lease a blob/container with duration
        /// 8.68 Lease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Lease a blob/container with duration
        ///         a. Not set (Infinite)
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseWithInfiniteDuration()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string containerLease = string.Empty;

            try
            {
                bool throwException = false;
                Test.Assert(CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
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

                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

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

                throwException = false;
                string blobLease = string.Empty;
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                blob.Metadata["test1"] = "m1";
                blob.Metadata["test2"] = "m2";

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
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

                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

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
        /// Lease a blob/container with duration
        /// 8.68 Lease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Lease a blob/container with duration
        ///         b. Random value between 15-60
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseWithDurationInSeconds()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string containerLease = string.Empty;

            try
            {
                bool throwException = false;
                Random random = new Random();
                int duration = random.Next(15, 60);

                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: duration), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
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

                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        AccessCondition condition = new AccessCondition()
                        {
                            LeaseId = containerLease
                        };
                        container.SetMetadata(condition);

                        Test.Assert((CommandAgent as NodeJSAgent).SetAzureStorageContainerACL(containerName, BlobContainerPublicAccessType.Container, containerLease),
                            Utility.GenComparisonData("Set container ACL with lease ID", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                throwException = false;
                string blobLease = string.Empty;
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                blob.Metadata["test1"] = "m1";
                blob.Metadata["test2"] = "m2";

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: duration), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
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

                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        AccessCondition condition = new AccessCondition() {
                            LeaseId = blobLease
                        };
                        blob.SetMetadata(condition);
                    } 
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                // Wait for the lease expired
                Thread.Sleep(duration * 1000);

                // Should be successful without lease ID
                blob.Metadata["test3"] = "m3";
                blob.SetMetadata();
                Test.Assert(CommandAgent.RemoveAzureStorageContainer(containerName), Utility.GenComparisonData("Remove container", true));
            }
            finally
            {
                // Don't need a lease ID as it is supposed to expire when runs to here.
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Lease a blob/container with proposed ID
        /// 8.68 Lease-AzureStorageContainer/Blob Positive Functional Cases
        ///     2. Lease a blob/container with proposed ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseWithProposedId()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string containerLease = Guid.NewGuid().ToString();

            try
            {
                bool throwException = false;
                Test.Assert(CommandAgent.AcquireLease(containerName, null, containerLease), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
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

                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

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

                throwException = false;
                string blobLease = Guid.NewGuid().ToString();
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                blob.Metadata["test1"] = "m1";
                blob.Metadata["test2"] = "m2";

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, blobLease), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
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

                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

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
        /// acquire lease on a blob/container with SAS
        /// 8.68 Lease-AzureStorageContainer/Blob Positive Functional Cases
        ///     3. acquire lease on a blob/container with SAS
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseWithInfiniteDurationBySAS()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string containerLease = string.Empty;
            
            try
            {
                string accountSasToken = string.Empty;
                string containerSasToken = string.Empty;

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
                    ResourceTypes = SharedAccessAccountResourceTypes.Container,
                    Services = SharedAccessAccountServices.Blob,
                    SharedAccessStartTime = DateTimeOffset.UtcNow,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5)
                };

                accountSasToken = Utility.GenerateAccountSAS(policy);

                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, accountSasToken);

                bool throwException = false;
                Test.Assert(CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
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

                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

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

                throwException = false;
                string blobLease = string.Empty;
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                blob.Metadata["test1"] = "m1";
                blob.Metadata["test2"] = "m2";

                // Use a container SAS
                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
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

                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

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
        /// Lease a non-existing container
        /// 8.68 Lease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Lease a non-existing container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseOnNonExistingContainer()
        {
            string containerName = Utility.GenNameString("container");

            Test.Assert(!CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Container Lease", false));
            CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
        }

        /// <summary>
        /// Lease a non-existing container
        /// 8.68 Lease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Lease a non-existing blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseOnNonExistingBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(!CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Lease a leased container
        /// 8.68 Lease-AzureStorageContainer/Blob Negative Functional Cases
        ///     2. Lease a leased container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseOnLeasedContainer()
        {
            string containerName = Utility.GenNameString("container");
            string leaseId = string.Empty;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        leaseId = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }

                    Test.Assert(!CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Lease", false));
                    CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, leaseId);
            }
        }

        /// <summary>
        /// Lease a leased blob
        /// 8.68 Lease-AzureStorageContainer/Blob Negative Functional Cases
        ///     2. Lease a leased blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseOnLeasedBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Lease", true));
                Test.Assert(!CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Lease a container with invalid duration
        /// 8.68 Lease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3. Lease a container with invalid duration: 0, 10, 100
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseContainerWithInvalidDuration()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(!CommandAgent.AcquireLease(containerName, null, duration: 0), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                Test.Assert(!CommandAgent.AcquireLease(containerName, null, duration: 10), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                Test.Assert(!CommandAgent.AcquireLease(containerName, null, duration: 100), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Lease a blob with invalid duration
        /// 8.68 Lease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3. Lease a blob with invalid duration: 0, 10, 100
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseBlobWithInvalidDuration()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(!CommandAgent.AcquireLease(containerName, blobName, duration: 0), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                Test.Assert(!CommandAgent.AcquireLease(containerName, blobName, duration: 10), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                Test.Assert(!CommandAgent.AcquireLease(containerName, blobName, duration: 100), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Lease a container with invalid proposed ID
        /// 8.68 Lease-AzureStorageContainer/Blob Negative Functional Cases
        ///     4. Lease a container with invalid proposed ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseContainerWithInvalidID()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string invalidLeaseID = "abcefggdfdsfsdsddsdds";

            try
            {
                Test.Assert(!CommandAgent.AcquireLease(containerName, null, proposedLeaseId: invalidLeaseID), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseID);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Lease a blob with invalid proposed ID
        /// 8.68 Lease-AzureStorageContainer/Blob Negative Functional Cases
        ///     4. Lease a blob with invalid proposed ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseBlobWithInvalidID()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string invalidLeaseID = "1223545698787879789456465";
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(!CommandAgent.AcquireLease(containerName, blobName, proposedLeaseId: invalidLeaseID), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, invalidLeaseID);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Lease without enough permission
        /// 8.68 Lease-AzureStorageContainer/Blob Negative Functional Cases
        ///     5. Lease a blob without enough permission
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void LeaseWithoutEnoughPermission()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                string containerSasToken = string.Empty;

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

                string accountSasToken = Utility.GenerateAccountSAS(policy);

                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, accountSasToken);

                Test.Assert(!CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
                Test.Assert(!CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);

                Test.Assert(!CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
                Test.Assert(!CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }
#endif
    }
}

