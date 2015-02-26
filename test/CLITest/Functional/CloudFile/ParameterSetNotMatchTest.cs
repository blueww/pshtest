namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    using System;
    using Management.Storage.ScenarioTest.Common;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.File;
    using StorageTestLib;

    [TestClass]
    internal class ParameterSetNotMatchTest : TestBase
    {
        private Random randomProvider = new Random();

        private CloudFileShare fileShare;

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

        public override void OnTestSetup()
        {
            this.fileShare = fileUtil.EnsureFileShareExists(CloudFileUtil.GenerateUniqueFileShareName());
        }

        public override void OnTestCleanUp()
        {
            this.agent.Dispose();
            fileUtil.DeleteFileShareIfExists(this.fileShare.Name);
        }

        /// <summary>
        /// Negative functional test case 5.15.1
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryWithNoPath_FileShare()
        {
            this.agent.SetVariable("share", this.fileShare);
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("New-AzureStorageDirectory -Share $share");
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryWithNoPath_FileShareName()
        {
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("New-AzureStorageDirectory -ShareName " + this.fileShare.Name);
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.3
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryWithNoPath_Directory()
        {
            var dir = fileUtil.EnsureDirectoryExists(this.fileShare, CloudFileUtil.GenerateUniqueDirectoryName());
            this.agent.SetVariable("dir", dir);
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("New-AzureStorageDirectory -Directory $dir");
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveDirectoryWithNoPath_FileShare()
        {
            this.agent.SetVariable("share", this.fileShare);
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("Remove-AzureStorageDirectory -Share $share");
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveDirectoryWithNoPath_FileShareName()
        {
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("Remove-AzureStorageDirectory -ShareName " + this.fileShare.Name);
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void DownloadFileWithNoPath_FileShare()
        {
            this.agent.SetVariable("share", this.fileShare);
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("Get-AzureStorageFileContent -Share $share");
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void DownloadFileWithNoPath_FileShareName()
        {
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("Get-AzureStorageFileContent -ShareName " + this.fileShare.Name);
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.8
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void DownloadFileWithNoPath_Directory()
        {
            var dir = fileUtil.EnsureDirectoryExists(this.fileShare, CloudFileUtil.GenerateUniqueDirectoryName());
            this.agent.SetVariable("dir", dir);
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("Get-AzureStorageFileContent -Directory $dir");
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.9
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void UploadFileWithNoPath_FileShare()
        {
            this.agent.SetVariable("share", this.fileShare);
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("Set-AzureStorageFileContent -Share $share");
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.10
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void UploadFileWithNoPath_FileShareName()
        {
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("Set-AzureStorageFileContent -ShareName " + this.fileShare.Name);
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.11
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveFileWithNoPath_FileShare()
        {
            this.agent.SetVariable("share", this.fileShare);
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("Remove-AzureStorageFile -Share $share");
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveFileWithNoPath_FileShareName()
        {
            ((PowerShellAgent)this.agent).PowerShellSession.AddScript("Remove-AzureStorageFile -ShareName " + this.fileShare.Name);
            var result = this.agent.Invoke();
            result.AssertNoResult();
            this.agent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }
    }
}
