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

namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using StorageTestLib;

    [TestClass]
    public class RemoveAzureStorageFileShareTest : TestBase
    {
        private Random randomProvider = new Random();

        [ClassInitialize]
        public static void NewAzureStorageFileShareTestInitialize(TestContext context)
        {
            TestBase.TestClassInitialize(context);
        }

        [ClassCleanup]
        public static void NewAzureStorageFileShareTestCleanup()
        {
            TestBase.TestClassCleanup();
        }

        /// <summary>
        /// Positive functional test case 5.4.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void PipelineMultipleShareNamesToRemoveTest()
        {
            // TODO: Generate more random names for file shares after the
            // naming rules is settled down.
            int numberOfShares = this.randomProvider.Next(2, 33);
            string[] names = Enumerable.Range(0, numberOfShares)
                    .Select(i => CloudFileUtil.GenerateUniqueFileShareName()).ToArray();
            foreach (var name in names)
            {
                fileUtil.EnsureFileShareExists(name);
            }

            try
            {
                CommandAgent.RemoveFileShareFromPipeline();
                var result = CommandAgent.Invoke(names);

                CommandAgent.AssertNoError();
                result.AssertNoResult();
            }
            finally
            {
                foreach (string fileShareName in names)
                {
                    fileUtil.DeleteFileShareIfExists(fileShareName);
                }
            }
        }

        /// <summary>
        /// Negative functional test case 5.4.1 with invalid account
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveFileShareWithInvalidAccountTest()
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.EnsureFileShareExists(fileShareName);
            try
            {
                // Creates an storage context object with invalid account
                // name.
                var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountName: true);
                object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
                CommandAgent.RemoveFileShareByName(fileShareName, false, invalidStorageContextObject);
                var result = CommandAgent.Invoke();
                CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AccountIsDisabledFullQualifiedErrorId, AssertUtil.NameResolutionFailureFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId, AssertUtil.InvalidResourceFullQualifiedErrorId));
                fileUtil.AssertFileShareExists(fileShareName, "File share should not be removed when providing invalid credentials.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// Negative functional test case 5.4.1 with invalid key value
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveFileShareWithInvalidKeyValueTest()
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.EnsureFileShareExists(fileShareName);
            try
            {
                // Creates an storage context object with invalid key value
                var invalidAccount = CloudFileUtil.MockupStorageAccount(StorageAccount, mockupAccountKey: true);
                object invalidStorageContextObject = CommandAgent.CreateStorageContextObject(invalidAccount.ToString(true));
                CommandAgent.RemoveFileShareByName(fileShareName, false, invalidStorageContextObject);
                var result = CommandAgent.Invoke();
                CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.AuthenticationFailedFullQualifiedErrorId, AssertUtil.ProtocolErrorFullQualifiedErrorId));
                fileUtil.AssertFileShareExists(fileShareName, "File share should not be removed when providing invalid credentials.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// Negative functional test case 5.4.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveNonExistingFileShareTest()
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.DeleteFileShareIfExists(fileShareName);

            try
            {
                CommandAgent.RemoveFileShareByName(fileShareName);
                var result = CommandAgent.Invoke();
                CommandAgent.AssertErrors(record => record.AssertError(AssertUtil.ShareNotFoundFullQualifiedErrorId));
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// Negative functional test case 5.4.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.NodeJSFT)]
        public void RemoveFileShareWhileAFileIsUploading()
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);

            var stream = new BlockReadUntilSetStream();
            Task uploadTask = null;
            try
            {
                string fileName = CloudFileUtil.GenerateUniqueFileName();
                var file = fileUtil.CreateFile(fileShare, fileName);

                // Creates a stream which would block the read operation.
                uploadTask = Task.Factory.StartNew(() => file.UploadFromStream(stream));
                CommandAgent.RemoveFileShareByName(fileShareName);
                var result = CommandAgent.Invoke();
                CommandAgent.AssertNoError();
            }
            finally
            {
                stream.StopBlockingReadOperation();
                if (uploadTask != null)
                {
                    try
                    {
                        uploadTask.Wait();
                    }
                    catch
                    {
                    }
                }

                stream.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }
    }
}
