﻿namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;

    [TestClass]
    internal class InvalidParameterTest : TestBase
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
        /// Negative functional test case 5.14.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void NewShareWithNullNameTest()
        {
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("New-AzureStorageShare -Name $null");
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ParameterArgumentValidationErrorFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.14.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void NewShareWithEmptyNameTest()
        {
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("New-AzureStorageShare -Name \"\"");
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ParameterArgumentValidationErrorFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.14.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void NewDirectoryWithNullShareObjectTest()
        {
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("New-AzureStorageDirectory -Share $null -Path dir");
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ParameterArgumentValidationErrorFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.14.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void ListFilesUsingNullDirectoryObjectTest()
        {
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Get-AzureStorageFile -Directory $null");
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ParameterArgumentValidationErrorFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.14.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void DownloadFileUsingNullFileObjectTest()
        {
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Get-AzureStorageFileContent -File $null -Destination .");
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.ParameterArgumentValidationErrorFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.14.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void DownloadAFileUsingFileObjectWhichHasJustBeenDeletedTest()
        {
            var share = fileUtil.EnsureFileShareExists(CloudFileUtil.GenerateUniqueFileShareName());
            try
            {
                var file = fileUtil.CreateFile(share, CloudFileUtil.GenerateUniqueFileName());
                file.Delete();
                CommandAgent.DownloadFile(file, Test.Data.Get("TempDir"), true);
                var result = CommandAgent.Invoke();
                result.AssertNoResult();
                CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.InvalidOperationExceptionFullQualifiedErrorId, AssertUtil.InvalidOperationExceptionFullQualifiedErrorId, AssertUtil.ResourceNotFoundFullQualifiedErrorId));
            }
            finally
            {
                if (share != null)
                {
                    share.DeleteIfExists();
                }
            }
        }
    }
}
