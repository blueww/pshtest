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
using Management.Storage.ScenarioTest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using System.Reflection;

namespace Management.Storage.ScenarioTest.Functional.Blob
{
    /// <summary>
    /// functional tests for Set-ContainerAcl
    /// </summary>
    [TestClass]
    public class SetContainerAcl : TestBase
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            TestBase.TestClassInitialize(testContext);
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// set container acl with invalid container name
        /// 8.5	Set-AzureStorageContainerACL Negative Functional Cases
        ///     1.Set the PublicAccess of a blob container with an invalid blob container name  
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(PsTag.Container)]
        [TestCategory(PsTag.GetContainer)]
        [TestCategory(CLITag.NodeJSFT)]
        public void SetContainerAclWithInvalidName()
        {
            string containerName = "ContainerName";

            Test.Assert(!CommandAgent.SetAzureStorageContainerACL(containerName, BlobContainerPublicAccessType.Blob), "SetAzureStorageContainerACL with invalid operation should fail");
            Test.Assert(CommandAgent.ErrorMessages.Count == 1, "set container acl with invalid name should only throw one exception");
            CommandAgent.ValidateErrorMessage(MethodBase.GetCurrentMethod().Name, containerName);
        }
    }
}
