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
            CommandAgent.SetVariable("share", this.fileShare);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("New-AzStorageDirectory -Share $share");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("New-AzureStorageDirectory -Share $share");
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.2
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void CreateDirectoryWithNoPath_FileShareName()
        {
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("New-AzStorageDirectory -ShareName " + this.fileShare.Name);
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("New-AzureStorageDirectory -ShareName " + this.fileShare.Name);
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
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
            CommandAgent.SetVariable("dir", dir);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("New-AzStorageDirectory -Directory $dir");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("New-AzureStorageDirectory -Directory $dir");
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.4
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveDirectoryWithNoPath_FileShare()
        {
            CommandAgent.SetVariable("share", this.fileShare);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Remove-AzStorageDirectory -Share $share");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Remove-AzureStorageDirectory -Share $share");
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.5
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveDirectoryWithNoPath_FileShareName()
        {
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Remove-AzStorageDirectory -ShareName " + this.fileShare.Name);
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Remove-AzureStorageDirectory -ShareName " + this.fileShare.Name);
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.6
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void DownloadFileWithNoPath_FileShare()
        {
            CommandAgent.SetVariable("share", this.fileShare);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Get-AzStorageFileContent -Share $share");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Get-AzureStorageFileContent -Share $share");
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.7
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void DownloadFileWithNoPath_FileShareName()
        {
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Get-AzStorageFileContent -ShareName " + this.fileShare.Name);
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Get-AzureStorageFileContent -ShareName " + this.fileShare.Name);
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
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
            CommandAgent.SetVariable("dir", dir);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Get-AzStorageFileContent -Directory $dir");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Get-AzureStorageFileContent -Directory $dir");
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.9
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void UploadFileWithNoPath_FileShare()
        {
            CommandAgent.SetVariable("share", this.fileShare);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Set-AzStorageFileContent -Share $share");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Set-AzureStorageFileContent -Share $share");
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.10
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void UploadFileWithNoPath_FileShareName()
        {
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Set-AzStorageFileContent -ShareName " + this.fileShare.Name);
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Set-AzureStorageFileContent -ShareName " + this.fileShare.Name);
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.11
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveFileWithNoPath_FileShare()
        {
            CommandAgent.SetVariable("share", this.fileShare);
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Remove-AzStorageFile -Share $share");
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Remove-AzureStorageFile -Share $share");
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }

        /// <summary>
        /// Negative functional test case 5.15.12
        /// </summary>
        [TestMethod]
        [TestCategory(PsTag.File)]
        [TestCategory(Tag.Function)]
        public void RemoveFileWithNoPath_FileShareName()
        {
#if NEW_CMDLET_NAME
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Remove-AzStorageFile -ShareName " + this.fileShare.Name);
#else
            ((PowerShellAgent)CommandAgent).PowerShellSession.AddScript("Remove-AzureStorageFile -ShareName " + this.fileShare.Name);
#endif
            var result = CommandAgent.Invoke();
            result.AssertNoResult();
            CommandAgent.AssertErrors(err => err.AssertError(AssertUtil.MissingMandatoryParameterFullQualifiedErrorId));
        }
    }
}
