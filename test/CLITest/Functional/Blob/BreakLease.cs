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
    public class BreakLeaseTest : TestBase
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void BreakLeaseTestClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Break a container lease
        /// 8.72 BreakRelease-AzureStorageContainer/Blob BVT Cases
        ///     1. Break a container lease
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakContainerLease()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string lease = string.Empty;

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, string.Empty), Utility.GenComparisonData("Acquire Container Lease", true));

#if !DOTNET5_4
                if (lang == Language.NodeJS)
                {
                    try
                    {
                        lease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert((CommandAgent as NodeJSAgent).ShowAzureStorageContainer(containerName), Utility.GenComparisonData("Show container without lease ID", true));

                        Test.Assert(CommandAgent.BreakLease(containerName, string.Empty), Utility.GenComparisonData("Break Container Lease", true));
                        Test.Assert(!(CommandAgent as NodeJSAgent).ShowAzureStorageContainer(containerName, lease), Utility.GenComparisonData("Show container with lease ID", false));
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
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break a blob lease with a proposed id and access it by Lease ID
        /// 8.72 BreakRelease-AzureStorageContainer/Blob BVT Cases
        ///     2. Break a blob lease with a proposed id and access it by Lease ID
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakBlobLease()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                string proposedId = Guid.NewGuid().ToString();
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, proposedId), Utility.GenComparisonData("Acquire Blob Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        string lease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.BreakLease(containerName, blobName), Utility.GenComparisonData("Break Blob Lease", true));

                        Test.Assert((CommandAgent as NodeJSAgent).ShowAzureStorageBlob(blobName, containerName), Utility.GenComparisonData("Show blob without lease ID", true));
                        Test.Assert(!(CommandAgent as NodeJSAgent).ShowAzureStorageBlob(blobName, containerName, lease), Utility.GenComparisonData("Show blob with lease ID", false));
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
        /// Break a blob lease with a proposed duration
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Break a blob lease with a proposed duration
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakBlobLeaseAndCheckDuration()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            int duration = 30;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                string proposedId = Guid.NewGuid().ToString();
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, proposedId, duration), Utility.GenComparisonData("Acquire Blob Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        string lease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.BreakLease(containerName, blobName), Utility.GenComparisonData("Break Blob Lease", true));

                        int remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime > duration -15 && remainingTime < duration, Utility.GenComparisonData("Validate remaining time", true));
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
        /// Break a infinite lease
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     2. Break a infinite lease
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakInfiniteLease()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                string proposedId = Guid.NewGuid().ToString();
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, proposedId), Utility.GenComparisonData("Acquire Blob Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        string lease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.BreakLease(containerName, blobName), Utility.GenComparisonData("Break Blob Lease", true));

                        int remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime == 0, Utility.GenComparisonData("Validate remaining time", true));
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
        /// Break a container lease with a proposed duration
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     3. Break a container lease with a proposed duration
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakContainerLeaseWithDuration()
        {
            string containerName = Utility.GenNameString("container");
            int[] durations = { 0, 0, 60 };
            Random random = new Random();
            durations[1] = random.Next(0, 60);
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                foreach (int duration in durations)
                {
                    Test.Assert(CommandAgent.AcquireLease(containerName, null), Utility.GenComparisonData("Acquire Container Lease", true));

                    if (lang == Language.NodeJS)
                    {
                        try
                        {
                            Test.Assert(CommandAgent.BreakLease(containerName, null, duration), Utility.GenComparisonData("Break Container Lease", true));

                            int remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                            Test.Assert(remainingTime <= duration, Utility.GenComparisonData("Validate remaining time", true));
                            Thread.Sleep((remainingTime + 1) * 1000);
                        }
                        catch (Exception e)
                        {
                            Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                        }
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break a blob lease with a proposed duration
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     3. Break a blob lease with a proposed duration
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakBlobLeaseWithDuration()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            int[] durations = { 0, 0, 60 };
            Random random = new Random();
            durations[1] = random.Next(0, 60);
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                foreach (int duration in durations)
                {
                    Test.Assert(CommandAgent.AcquireLease(containerName, blobName), Utility.GenComparisonData("Acquire Blob Lease", true));

                    if (lang == Language.NodeJS)
                    {
                        try
                        {
                            Test.Assert(CommandAgent.BreakLease(containerName, blobName, duration), Utility.GenComparisonData("Break Blob Lease", true));

                            int remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                            Test.Assert(remainingTime <= duration, Utility.GenComparisonData("Validate remaining time", true));
                            Thread.Sleep((remainingTime + 1) * 1000);
                        }
                        catch (Exception e)
                        {
                            Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                        }
                    }
                }
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break a container lease with a proposed duration longer than the remaining time
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     4. Break a container lease with a proposed duration longer than the remaining time
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakContainerLeaseWithDurationLongerThanRemainingTime()
        {
            string containerName = Utility.GenNameString("container");
            int leaseDuration = 15;
            int breakDuration = 60;
            int remainingTime = breakDuration;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: leaseDuration), Utility.GenComparisonData("Acquire Container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        Test.Assert(CommandAgent.BreakLease(containerName, null, breakDuration), Utility.GenComparisonData("Break Container Lease", true));

                        remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime <= leaseDuration, Utility.GenComparisonData("Validate remaining time", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                Thread.Sleep((remainingTime + 1) * 1000);
                container.Delete();
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break a blob lease with a proposed duration longer than the remaining time
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     4. Break a blob lease with a proposed duration longer than the remaining time
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakBlobLeaseWithDurationLongerThanRemainingTime()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            int leaseDuration = 15;
            int breakDuration = 60;
            int remainingTime = breakDuration;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: leaseDuration), Utility.GenComparisonData("Acquire Blob Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        Test.Assert(CommandAgent.BreakLease(containerName, blobName, breakDuration), Utility.GenComparisonData("Break Blob Lease", true));

                        remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime <= leaseDuration, Utility.GenComparisonData("Validate remaining time", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                Thread.Sleep((remainingTime + 1) * 1000);
                blob.Delete();
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break a container lease with a proposed duration shoter than the remaining time
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     5. Break a container lease with a proposed duration shorter than the remaining time
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakContainerLeaseWithDurationShorterThanRemainingTime()
        {
            string containerName = Utility.GenNameString("container");
            int leaseDuration = 30;
            int breakDuration = 5;
            int remainingTime = breakDuration;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: leaseDuration), Utility.GenComparisonData("Acquire Container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        Test.Assert(CommandAgent.BreakLease(containerName, null, breakDuration), Utility.GenComparisonData("Break Container Lease", true));

                        remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime <= breakDuration, Utility.GenComparisonData("Validate remaining time", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                Thread.Sleep((remainingTime + 1) * 1000);
                container.Delete();
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break a blob lease with a proposed duration longer than the remaining time
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     5. Break a blob lease with a proposed duration longer than the remaining time
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakBlobLeaseWithDurationShorterThanRemainingTime()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            int leaseDuration = 30;
            int breakDuration = 5;
            int remainingTime = breakDuration;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: leaseDuration), Utility.GenComparisonData("Acquire Blob Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        Test.Assert(CommandAgent.BreakLease(containerName, blobName, breakDuration), Utility.GenComparisonData("Break Blob Lease", true));

                        remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime <= breakDuration, Utility.GenComparisonData("Validate remaining time", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                Thread.Sleep((remainingTime + 1) * 1000);
                blob.Delete();
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break a container lease and acquire again
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     6. Break a container lease and acquire again
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakContainerLeaseAndAcquireAgain()
        {
            string containerName = Utility.GenNameString("container");
            string containerLease = string.Empty;
            int leaseDuration = 15;
            int breakDuration = 30;
            int remainingTime = breakDuration;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: leaseDuration), Utility.GenComparisonData("Acquire Container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        Test.Assert(CommandAgent.BreakLease(containerName, null, breakDuration), Utility.GenComparisonData("Break Container Lease", true));

                        remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime <= leaseDuration, Utility.GenComparisonData("Validate remaining time", true));

                        Test.Assert(!CommandAgent.AcquireLease(containerName, null, duration: leaseDuration), Utility.GenComparisonData("Acquire Container Lease", false));
                        CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                        Thread.Sleep((remainingTime + 1) * 1000);
                        Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: leaseDuration), Utility.GenComparisonData("Acquire Container Lease", true));
                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
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
        /// Break a blob lease and acquire again
        /// 8.72 BreakRelease-AzureStorageContainer/Blob Positive Functional Cases
        ///     6. Break a blob lease and acquire again
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakBlobLeaseAndAcquireAgain()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string blobLease = string.Empty;
            int leaseDuration = 15;
            int breakDuration = 30;
            int remainingTime = breakDuration;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);
                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: leaseDuration), Utility.GenComparisonData("Acquire Blob Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        Test.Assert(CommandAgent.BreakLease(containerName, blobName, breakDuration), Utility.GenComparisonData("Break Blob Lease", true));

                        remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime <= leaseDuration, Utility.GenComparisonData("Validate remaining time", true));

                        Test.Assert(!CommandAgent.AcquireLease(containerName, blobName, duration: leaseDuration), Utility.GenComparisonData("Acquire Blob Lease", false));
                        CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                        Thread.Sleep((remainingTime + 1) * 1000);
                        Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: leaseDuration), Utility.GenComparisonData("Acquire Blob Lease", true));
                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;
                        Test.Assert(CommandAgent.RemoveAzureStorageBlob(blobName, containerName, leaseId: blobLease), Utility.GenComparisonData("Delete Blob With Lease", true));
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
        /// Break a lease of a blob/container by SAS
        /// 8.72 BreakLease-AzureStorageContainer/Blob Positive Functional Cases
        ///     1. Break a lease of a blob/container by SAS
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakLeaseBySAS()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string containerLease = string.Empty;

            Random random = new Random();
            int duration = random.Next(15, 60);
            int remainingTime = duration;

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

                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: duration), Utility.GenComparisonData("Acquire Container Lease", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        containerLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        Test.Assert(CommandAgent.BreakLease(containerName, null), Utility.GenComparisonData("Break Container Lease", true));
                        remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime <= duration, Utility.GenComparisonData("Validate remaining time", true));
                    }
                    catch (Exception e)
                    {
                        Test.Error(string.Format("{0} error: {1}", MethodBase.GetCurrentMethod().Name, e.Message));
                    }
                }

                string blobLease = string.Empty;
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                // Acquire with account SAS
                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: duration), Utility.GenComparisonData("Acquire Blob Lease With Account SAS", true));

                if (lang == Language.NodeJS)
                {
                    try
                    {
                        blobLease = (CommandAgent as NodeJSAgent).Output[0]["id"] as string;

                        // Break with container SAS
                        CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);

                        Test.Assert(CommandAgent.BreakLease(containerName, blobName), Utility.GenComparisonData("Break Blob Lease With Container SAS", true));
                        remainingTime = int.Parse((CommandAgent as NodeJSAgent).Output[0]["time"].ToString());
                        Test.Assert(remainingTime <= duration, Utility.GenComparisonData("Validate remaining time", true));
                        Thread.Sleep((remainingTime + 1) * 1000);
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
        /// Break the lease a non-existing container
        /// 8.72 BreakLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Break the lease a non-existing container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakLeaseOnNonExistingContainer()
        {
            string containerName = Utility.GenNameString("container");

            Test.Assert(!CommandAgent.BreakLease(containerName, null), Utility.GenComparisonData("Break Container Lease", false));
            CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
        }

        /// <summary>
        /// Break the lease a non-existing blob
        /// 8.72 BreakLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     1. Break the lease a non-existing blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakLeaseOnNonExistingBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(!CommandAgent.BreakLease(containerName, blobName), Utility.GenComparisonData("Break Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break the lease against a not leased container
        /// 8.72 BreakLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     2.  Break the lease against a not leased container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakNotLeasedContainer()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(!CommandAgent.BreakLease(containerName, null), Utility.GenComparisonData("Break Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break the lease against a not leased Blob
        /// 8.72 BreakLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     2.  Break the lease against a not leased Blob
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakNotLeasedBlob()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                Test.Assert(!CommandAgent.BreakLease(containerName, blobName), Utility.GenComparisonData("Break Blob Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break the cotainer lease with invalid duration
        /// 8.72 BreakLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3. Break the cotainer lease with invalid duration
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakContainerLeaseWithInvalidDuration()
        {
            string containerName = Utility.GenNameString("container");
            string containerLease = string.Empty;
            int leaseDuration = 15;
            int breakDuration = 61;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: leaseDuration), Utility.GenComparisonData("Acquire Container Lease", true));

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

                Test.Assert(!CommandAgent.BreakLease(containerName, null, breakDuration), Utility.GenComparisonData("Break Container Lease", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLease);
            }
        }

        /// <summary>
        /// Break the blob lease with invalid duration
        /// 8.72 BreakLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     3. Break the blob lease with invalid duration
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakBlobLeaseWithInvalidDuration()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            int leaseDuration = 15;
            int breakDuration = 61;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

            try
            {
                Test.Assert(CommandAgent.AcquireLease(containerName, blobName, duration: leaseDuration), Utility.GenComparisonData("Acquire Blob Lease", true));

                Test.Assert(!CommandAgent.BreakLease(containerName, blobName, breakDuration), Utility.GenComparisonData("Break Blob Lease With Invalid Duration", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }

        /// <summary>
        /// Break lease without enough permission
        /// 8.72 BreakLease-AzureStorageContainer/Blob Negative Functional Cases
        ///     4. Break lease without enough permission
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.BlobLease)]
        public void BreakLeaseWithoutEnoughPermission()
        {
            string containerName = Utility.GenNameString("container");
            string blobName = Utility.GenNameString("blob");
            string containerLeaseId = string.Empty;
            string blobLeaseId = string.Empty;
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            try
            {
                CloudBlob blob = blobUtil.CreateRandomBlob(container, blobName);

                Test.Assert(CommandAgent.AcquireLease(containerName, null, duration: 60), Utility.GenComparisonData("Acquire Container Lease", true));

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

#if !DOTNET5_4
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
#endif

                // Create a container SAS
                string containerSasToken = string.Empty;
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

                Test.Assert(!CommandAgent.BreakLease(containerName, null), Utility.GenComparisonData("Break Container Lease with SAS", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                Test.Assert(!CommandAgent.BreakLease(containerName, blobName), Utility.GenComparisonData("Break Blob Lease with SAS", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);

                CommandAgent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, containerSasToken);

                Test.Assert(!CommandAgent.BreakLease(containerName, null), Utility.GenComparisonData("Break Container Lease with SAS", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
                Test.Assert(!CommandAgent.BreakLease(containerName, blobName), Utility.GenComparisonData("Break Blob Lease with SAS", false));
                CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName, containerLeaseId);
            }
        }
    }
}

