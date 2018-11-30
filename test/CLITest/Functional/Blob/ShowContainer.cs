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
using System.Collections.ObjectModel;
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using System.Reflection;
using System.Collections.Generic;
using Management.Storage.ScenarioTest.Util;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional tests for show container (only for nodejs)
    /// </summary>
    [TestClass]
    public class ShowContainer : TestBase
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void GetContainerClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Positive Functional Cases : show the $root container (only for nodejs)
        /// 1. Show the root container (Positive 2)
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        public void ShowRootContainer()
        {
            const string rootContainerName = "$root";
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, rootContainerName);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetRootContainerReference();
            container.CreateIfNotExists();

            NodeJSAgent nodejsAgent = (NodeJSAgent)CommandAgent;
            //--------------Show operation--------------
            Test.Assert(nodejsAgent.ShowAzureStorageContainer(rootContainerName), Utility.GenComparisonData("show $root container", true));

            container.FetchAttributes();
            CloudBlobUtil.PackContainerCompareData(container, dic);
            // Verification for returned values
            nodejsAgent.OutputValidation(comp);
        }


        /// <summary>
        /// Positive Functional Cases : show the $logs container (only for nodejs)
        /// 1. Show the $logs container (Positive 2)
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        public void ShowLogsContainer()
        {
            const string containerName = "$logs";
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, containerName);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetContainerReference(containerName);

            NodeJSAgent nodejsAgent = (NodeJSAgent)CommandAgent;
            //--------------Show operation--------------
            Test.Assert(nodejsAgent.ShowAzureStorageContainer(containerName), Utility.GenComparisonData("show $logs container", true));

            container.FetchAttributes();
            CloudBlobUtil.PackContainerCompareData(container, dic);
            // Verification for returned values
            nodejsAgent.OutputValidation(comp);
        }

        /// <summary>
        /// Negative Functional Cases : show non existing container (only for nodejs)
        /// 1. Show a non-existing blob container (Negative 1)
        /// </summary>
        [TestMethod]
        [TestCategory(CLITag.NodeJSFT)]
        public void ShowNonExistingContainer()
        {
            string containerName = Utility.GenNameString("nonexisting");

            // Delete the container if it exists
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.DeleteIfExists();

            NodeJSAgent nodejsAgent = (NodeJSAgent)CommandAgent;
            //--------------Show operation--------------
            Test.Assert(!nodejsAgent.ShowAzureStorageContainer(containerName), Utility.GenComparisonData("show container", false));
            // Verification for returned values
            nodejsAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, containerName);
        }

        /// <summary>
        /// show container with sas policy (only for nodejs)
        /// Positive Functional Cases : 
        ///     8.	Show SharedAccessPolicies for a specific container
        /// </summary>
        [TestMethod()]
        [TestCategory(CLITag.NodeJSFT)]
        public void ShowContainerWithSasPolicy()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);

            NodeJSAgent nodejsAgent = (NodeJSAgent)CommandAgent;
            try
            {
                TimeSpan sasLifeTime = TimeSpan.FromMinutes(10);
                BlobContainerPermissions permission = new BlobContainerPermissions();
                int count = random.Next(1, 5);

                for (int i = 0; i < count; i++)
                {
                    permission.SharedAccessPolicies.Add(Utility.GenNameString("saspolicy"), new SharedAccessBlobPolicy
                    {
                        SharedAccessExpiryTime = DateTime.Now.Add(sasLifeTime),
                        Permissions = SharedAccessBlobPermissions.Read,
                    });

                }

                container.SetPermissions(permission);

                Test.Assert(nodejsAgent.ShowAzureStorageContainer(containerName), Utility.GenComparisonData("show container", true));
                Test.Assert(nodejsAgent.Output.Count == 1, String.Format("Create {0} containers, actually retrieved {1} containers", 1, nodejsAgent.Output.Count));

                nodejsAgent.OutputValidation(new List<BlobContainerPermissions>() { permission });
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
            }
        }
    }
}
