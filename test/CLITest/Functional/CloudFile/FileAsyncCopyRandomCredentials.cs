using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Management.Storage.ScenarioTest.Common;
using Management.Storage.ScenarioTest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using StorageFile = Microsoft.WindowsAzure.Storage.File;

namespace Management.Storage.ScenarioTest.Functional.CloudFile
{
    [TestClass]
    public class FileAsyncCopyRandomCredentials : TestBase
    {
        private static CloudStorageAccount StorageAccount2;
        private static CloudFileUtil fileUtil2;

        [ClassInitialize]
        public static void FileAsyncCopyRandomCredentialsInitialize(TestContext context)
        {
            TestBase.TestClassInitialize(context);
            StorageAccount2 = TestBase.GetCloudStorageAccountFromConfig("Secondary");
            fileUtil2 = new CloudFileUtil(StorageAccount2);
        }

        [ClassCleanup]
        public static void FileAsyncCopyRandomCredentialsCleanup()
        {
            TestBase.TestClassCleanup();
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyCrossAccountFromFile2SASFile()
        {
            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil2.EnsureFileShareExists(destShareName);

            string sourceShareName = Utility.GenNameString("sourceshare");
            CloudFileShare sourceShare = fileUtil.EnsureFileShareExists(sourceShareName);

            try
            {
                StorageFile.CloudFile sourceFile = fileUtil.CreateFile(sourceShare.GetRootDirectoryReference(), Utility.GenNameString("SourceFile"));

                string destFileName = Utility.GenNameString("destfile");
                string destSasToken = destShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
                });

                object destContext = this.agent.GetStorageContextWithSASToken(StorageAccount2, destSasToken);

                Test.Assert(!agent.StartFileCopy(sourceFile, destShareName, destFileName, destContext), "Copy to file with sas token credential should fail.");

                ExpectedContainErrorMessage("The specified resource does not exist.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(sourceShareName);
                fileUtil2.DeleteFileShareIfExists(destShareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyFromFile2SASFile()
        {
            CopyFromFile2SASFile((sourceShare) =>
            {
                if (lang == Language.PowerShell)
                {
                    PowerShellAgent.SetStorageContext(StorageAccount.ToString(true));
                }
                else
                {
                    NodeJSAgent.SetStorageContext(StorageAccount.ToString(true));
                }
            });

            CopyFromFile2SASFile((sourceShare) =>
            {
                string sasToken = sourceShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
                });

                this.agent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sasToken);
            });
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyFromSASFile()
        {
            CopyFromFile2File((sourceShare) =>
            {
                string sasToken = sourceShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
                });

                this.agent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sasToken);
            },
                (destShare) =>
                {
                    if (lang == Language.PowerShell)
                    {
                        return PowerShellAgent.GetStorageContext(StorageAccount.ToString(true));
                    }
                    else
                    {
                        return NodeJSAgent.GetStorageContext(StorageAccount.ToString(true));
                    }
                });
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyFromBlob2SASFile()
        {
            CopyFromBlob2SASFile((sourceContainer) =>
            {
                if (lang == Language.PowerShell)
                {
                    PowerShellAgent.SetStorageContext(StorageAccount.ToString(true));
                }
                else
                {
                    NodeJSAgent.SetStorageContext(StorageAccount.ToString(true));
                };
            });

            CopyFromBlob2SASFile((sourceContainer) =>
            {
                string sasToken = sourceContainer.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
                });

                this.agent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sasToken);
            });
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        public void CopyFromBlob2File()
        {
            CopyFromBlob2File((sourceContainer) =>
            {
                string sasToken = sourceContainer.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
                });

                this.agent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sasToken);
            });

            CopyFromBlob2File((sourceContainer) =>
            {
                var permissions = sourceContainer.GetPermissions();
                permissions.PublicAccess = BlobContainerPublicAccessType.Blob;

                if (lang == Language.PowerShell)
                {
                    PowerShellAgent.SetAnonymousStorageContext(StorageAccount.Credentials.AccountName, false);
                }
                else
                {
                    Agent.Context = null;
                }
            });
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void StartCopyFromNonPublicUriCrossAccount()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil2.EnsureFileShareExists(shareName);

            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                StorageFile.CloudFile sourceFile = fileUtil2.CreateFile(share, fileName);

                StorageFile.CloudFile destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), fileName);

                Agent.Context = null;

                Test.Assert(!agent.StartFileCopy(sourceFile.Uri.ToString(), destFile), "Copy from non public non sas uri should fail.");
                ExpectedContainErrorMessage("The specified resource does not exist.");
            }
            finally
            {
                fileUtil2.DeleteFileShareIfExists(shareName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void StartCopyFromInvalidContext()
        {
            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            string sourceShareName = Utility.GenNameString("sourceshare");
            CloudFileShare sourceShare = fileUtil.EnsureFileShareExists(sourceShareName);

            try
            {
                StorageFile.CloudFile sourceFile = fileUtil.CreateFile(sourceShare.GetRootDirectoryReference(), Utility.GenNameString("SourceFile"));

                object destContext;
                if (lang == Language.PowerShell)
                {
                    destContext = PowerShellAgent.GetStorageContext(StorageAccount.ToString(true));
                }
                else
                {
                    destContext = NodeJSAgent.GetStorageContext(StorageAccount.ToString(true));
                }

                string sasToken = sourceShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Write,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
                });

                this.agent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sasToken);

                string destFileName = Utility.GenNameString("destfile");
                Test.Assert(!agent.StartFileCopyFromFile(sourceShareName, sourceFile.Name, destShareName, destFileName, destContext), "Copy to file with invalid sas token credential should fail.");
                ExpectedContainErrorMessage("The specified resource does not exist.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(sourceShareName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        [TestMethod()]
        [TestCategory(Tag.Function)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSFT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void GetCopyStateWithSAS()
        {
            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                string fileName = Utility.GenNameString("DestFile");
                StorageFile.CloudFile destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), fileName);

                object destContext;
                if (lang == Language.PowerShell)
                {
                    destContext = PowerShellAgent.GetStorageContext(StorageAccount.ToString(true));
                }
                else
                {
                    destContext = NodeJSAgent.GetStorageContext(StorageAccount.ToString(true));
                }

                string bigBlobUri = Test.Data.Get("BigBlobUri");

                Test.Assert(agent.StartFileCopy(bigBlobUri, destShareName, fileName, destContext), "Copy to file should succeed.");

                string sasToken = destShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
                });

                this.agent.SetStorageContextWithSASToken(StorageAccount.Credentials.AccountName, sasToken);

                Test.Assert(agent.GetFileCopyState(destShareName, fileName, destContext), "Get copy state with sas token should succeed.");

                string copyId = null;
                if (lang == Language.NodeJS)
                {
                    copyId = agent.Output[0]["copyId"] as string;
                }
                Test.Assert(agent.StopFileCopy(destFile, copyId), "Stop file copy should succeed.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        private void CopyFromFile2SASFile(Action<CloudFileShare> SetSourceContext)
        {
            CopyFromFile2File(SetSourceContext,
                (destShare) =>
                {
                    string destSasToken = destShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
                    {
                        Permissions = SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write,
                        SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
                    });

                    return this.agent.GetStorageContextWithSASToken(StorageAccount, destSasToken);
                });
        }

        private void CopyFromFile2File(Action<CloudFileShare> SetSourceContext, Func<CloudFileShare, object> getDestContext)
        {
            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            string sourceShareName = Utility.GenNameString("sourceshare");
            CloudFileShare sourceShare = fileUtil.EnsureFileShareExists(sourceShareName);

            try
            {
                StorageFile.CloudFile sourceFile = fileUtil.CreateFile(sourceShare.GetRootDirectoryReference(), Utility.GenNameString("SourceFile"));

                object destContext = getDestContext(destShare);

                SetSourceContext(sourceShare);

                string destFileName = Utility.GenNameString("destfile");
                Test.Assert(agent.StartFileCopyFromFile(sourceShareName, sourceFile.Name, destShareName, destFileName, destContext), "Copy to file with sas token credential should succeed.");

                var destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFileName);

                Test.Assert(agent.GetFileCopyState(destFile, destContext, true), "Get file copy state should succeed.");

                CloudFileUtil.ValidateCopyResult(sourceFile, destFile);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(sourceShareName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        private void CopyFromBlob2SASFile(Action<CloudBlobContainer> SetSourceContext)
        {
            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            string sourceContainerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(sourceContainerName);

            try
            {
                CloudBlob sourceBlob = blobUtil.CreateRandomBlob(container, Utility.GenNameString("BlobName"));

                string destSasToken = destShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1)
                });

                object destContext = this.agent.GetStorageContextWithSASToken(StorageAccount, destSasToken);

                SetSourceContext(container);

                Test.Assert(!agent.StartFileCopyFromBlob(sourceContainerName, sourceBlob.Name, destShareName, sourceBlob.Name, destContext), "Copy to file with sas token credential should succeed.");

                ExpectedContainErrorMessage("The specified resource does not exist.");

            }
            finally
            {
                blobUtil.RemoveContainer(sourceContainerName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        private void CopyFromBlob2File(Action<CloudBlobContainer> SetSourceContext)
        {
            string destShareName = Utility.GenNameString("destshare");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            string sourceContainerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(sourceContainerName);

            try
            {
                CloudBlob sourceBlob = blobUtil.CreateRandomBlob(container, Utility.GenNameString("BlobName"));

                object destContext;
                if (lang == Language.PowerShell)
                {
                    destContext = PowerShellAgent.GetStorageContext(StorageAccount.ToString(true));
                }
                else
                {
                    destContext = NodeJSAgent.GetStorageContext(StorageAccount.ToString(true));
                }

                SetSourceContext(container);

                string destFileName = Utility.GenNameString("destfile");
                Test.Assert(agent.StartFileCopyFromBlob(sourceContainerName, sourceBlob.Name, destShareName, destFileName, destContext), "Copy to file with sas token credential should succeed.");

                var destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFileName);

                Test.Assert(agent.GetFileCopyState(destFile, destContext, true), "Get file copy state should succeed.");

                CloudFileUtil.ValidateCopyResult(sourceBlob, destFile);

            }
            finally
            {
                blobUtil.RemoveContainer(sourceContainerName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }
    }
}
