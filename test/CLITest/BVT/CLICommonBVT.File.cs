namespace Management.Storage.ScenarioTest.BVT
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;
    using StorageTestLib;
    using Microsoft.WindowsAzure.Storage.Blob;
    using StorageBlob = Microsoft.WindowsAzure.Storage.Blob;
    using HttpConnectionStringBVT = Management.Storage.ScenarioTest.BVT.HTTP.ConnectionStringBVT;
    using HttpEnvConnectionStringBVT = Management.Storage.ScenarioTest.BVT.HTTP.EnvConnectionStringBVT;
    using HttpsConnectionStringBVT = Management.Storage.ScenarioTest.BVT.HTTPS.ConnectionStringBVT;
    using HttpsEnvConnectionStringBVT = Management.Storage.ScenarioTest.BVT.HTTPS.EnvConnectionStringBVT;

    /// <summary>
    /// Contains BVT test cases for file services
    /// </summary>
    public partial class CLICommonBVT
    {
        /// <summary>
        /// Stores a list of allowed configuration sets. Notice that they are
        /// all derived classes of CLICommonBVT.
        /// </summary>
        private static readonly Type[] AllowedConfigurationSets = new Type[]{
            typeof(HttpConnectionStringBVT),
            typeof(HttpsConnectionStringBVT),
            typeof(HttpEnvConnectionStringBVT),
            typeof(HttpsEnvConnectionStringBVT),
        };

        /// <summary>
        /// BVT case 5.2.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void NewFileShareTest()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.DeleteFileShareIfExistsWithSleep(fileShareName);

            try
            {
                agent.NewFileShare(fileShareName);
                var result = agent.Invoke();

                agent.AssertNoError();
                result.AssertObjectCollection(obj => result.AssertCloudFileContainer(obj, fileShareName), 1);
                fileUtil.AssertFileShareExists(fileShareName, "Container should exist after created.");
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// BVT case 5.3.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void GetExistingFileShareTest()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.EnsureFileShareExists(fileShareName);

            try
            {
                agent.GetFileShareByName(fileShareName);

                var result = agent.Invoke();

                agent.AssertNoError();
                result.AssertObjectCollection(obj => result.AssertCloudFileContainer(obj, fileShareName), 1);
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// BVT case 5.4.1
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void RemoveFileShareTest()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            fileUtil.EnsureFileShareExists(fileShareName);

            try
            {
                agent.RemoveFileShareByName(fileShareName);

                var result = agent.Invoke();

                agent.AssertNoError();
                result.AssertNoResult();
                fileUtil.AssertFileShareNotExists(fileShareName, "Container should not exist after removed.");
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// BVT case 5.5.1 using parameter set FileShare
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void NewDirectoryTest_FileShareParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            NewDirectoryTest((fileShare, directoryName) =>
            {
                agent.NewDirectory(fileShare, directoryName);
            });
        }

        /// <summary>
        /// BVT case 5.5.1 using parameter set FileShareName
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void NewDirectoryTest_FileShareNameParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            NewDirectoryTest((fileShare, directoryName) =>
            {
                agent.NewDirectory(fileShare.Name, directoryName);
            });
        }

        /// <summary>
        /// BVT case 5.6.1 using parameter set FileShare
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void RemoveDirectoryTest_FileShareParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            RemoveDirectoryTest((directory) =>
            {
                agent.RemoveDirectory(directory.Share, CloudFileUtil.GetFullPath(directory));
            });
        }

        /// <summary>
        /// BVT case 5.6.1 using parameter set FileShareName
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void RemoveDirectoryTest_FileShareNameParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            RemoveDirectoryTest((directory) =>
            {
                agent.RemoveDirectory(directory.Share.Name, CloudFileUtil.GetFullPath(directory));
            });
        }

        /// <summary>
        /// BVT case 5.7.1 using parameter set FileShareName
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void GetFileTest_FileShareNameParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            GetFileTest((fileShare) =>
            {
                agent.ListFiles(fileShare.Name);
            });
        }

        /// <summary>
        /// BVT case 5.7.1 using parameter set FileShare
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void GetFileTest_FileShareParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            GetFileTest((fileShare) =>
            {
                agent.ListFiles(fileShare);
            });
        }

        /// <summary>
        /// BVT case 5.7.1 using parameter set Directory
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void GetFileTest_DirectoryParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            string directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);
            var directory = fileUtil.EnsureDirectoryExists(fileShare, directoryName);
            var file = fileUtil.CreateFile(directory, fileName);

            try
            {
                agent.ListFiles(directory);

                var result = (PowerShellExecutionResult)agent.Invoke();

                agent.AssertNoError();
                result.AssertObjectCollection(obj => obj.AssertCloudFile(fileName, directoryName), 1);
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// BVT case 5.8.1 using parameter set FileShare
        /// Positive functional test case 5.8.4.
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void GetFileContentTest_FileShareParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            Test.Info("Testing against medium file.");
            GetFileContentTest(
                CommonMediumFilePath,
                MediumFileMD5,
                (file, destination) =>
                {
                    agent.DownloadFile(file.Share, file.Name, destination);
                });

            Test.Info("Testing against small file.");
            GetFileContentTest(
                CommonSmallFilePath,
                SmallFileMD5,
                (file, destination) =>
                {
                    agent.DownloadFile(file.Share, file.Name, destination);
                });
        }

        /// <summary>
        /// BVT case 5.8.1 using parameter set FileShareName
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void GetFileContentTest_FileShareNameParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            Test.Info("Testing against medium file.");
            GetFileContentTest(
                CommonMediumFilePath,
                MediumFileMD5,
                (file, destination) =>
                {
                    agent.DownloadFile(file.Share.Name, file.Name, destination);
                });

            Test.Info("Testing against small file.");
            GetFileContentTest(
                CommonSmallFilePath,
                SmallFileMD5,
                (file, destination) =>
                {
                    agent.DownloadFile(file.Share.Name, file.Name, destination);
                });
        }

        /// <summary>
        /// BVT case 5.8.1 using parameter set File
        /// Positive functional test case 5.8.13.
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void GetFileContentTest_FileParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            Test.Info("Testing against medium file.");
            GetFileContentTest(
                CommonMediumFilePath,
                MediumFileMD5,
                (file, destination) =>
                {
                    agent.DownloadFile(file, destination);
                });

            Test.Info("Testing against small file.");
            GetFileContentTest(
                CommonSmallFilePath,
                SmallFileMD5,
                (file, destination) =>
                {
                    agent.DownloadFile(file, destination);
                });
        }

        /// <summary>
        /// BVT case 5.9.1 using parameter set FileShare
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void SetFileContentTest_FileShareParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            Test.Info("Testing against medium file.");
            SetFileContentTest(
                CommonMediumFilePath,
                MediumFileMD5,
                (fileShare, path) =>
                {
                    agent.UploadFile(fileShare, CommonMediumFilePath, path);
                });

            Test.Info("Testing against small file.");
            SetFileContentTest(
                CommonSmallFilePath,
                SmallFileMD5,
                (fileShare, path) =>
                {
                    agent.UploadFile(fileShare, CommonSmallFilePath, path);
                });
        }

        /// <summary>
        /// BVT case 5.9.1 using parameter set FileShareName
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void SetFileContentTest_FileShareNameParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            Test.Info("Testing against medium file.");
            SetFileContentTest(
                CommonMediumFilePath,
                MediumFileMD5,
                (fileShare, path) =>
                {
                    agent.UploadFile(fileShare.Name, CommonMediumFilePath, path);
                });

            Test.Info("Testing against small file.");
            SetFileContentTest(
                CommonSmallFilePath,
                SmallFileMD5,
                (fileShare, path) =>
                {
                    agent.UploadFile(fileShare.Name, CommonSmallFilePath, path);
                });
        }

        /// <summary>
        /// BVT case 5.10.1 using parameter set FileShare
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void RemoveFileTest_FileShareParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            RemoveFileTest((file) =>
            {
                agent.RemoveFile(file.Share, file.Name);
            });
        }

        /// <summary>
        /// BVT case 5.10.1 using parameter set FileShareName
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.NodeJSBVT)]
        public void RemoveFileTest_FileShareNameParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            RemoveFileTest((file) =>
            {
                agent.RemoveFile(file.Share.Name, file.Name);
            });
        }

        /// <summary>
        /// BVT case 5.10.1 using parameter set File
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void RemoveFileTest_FileParameterSet()
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            RemoveFileTest((file) =>
            {
                agent.RemoveFile(file);
            });
        }

        /// <summary>
        /// Test Plan 8.48 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void NewShareStoredPolicyTest()
        {
            SharedAccessPolicyTest((share, samplePolicies) =>
            {
                var samplePolicy = samplePolicies[0];
                Test.Assert(agent.NewAzureStorageShareStoredAccessPolicy(share.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in file share should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                SharedAccessFilePolicies expectedPolicies = new SharedAccessFilePolicies();
                expectedPolicies.Add(samplePolicy.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission));

                Utility.WaitForPolicyBecomeValid<CloudFileShare>(share, samplePolicy);

                Utility.ValidateStoredAccessPolicies<SharedAccessFilePolicy>(share.GetPermissions().SharedAccessPolicies, expectedPolicies);
            });
        }

        /// <summary>
        /// Test Plan 8.49 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void GetShareStoredPolicyTest()
        {
            SharedAccessPolicyTest((share, samplePolicies) =>
            {
                var samplePolicy = samplePolicies[0];
                Test.Assert(agent.NewAzureStorageShareStoredAccessPolicy(share.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in file share should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudFileShare>(share, samplePolicy);

                Test.Assert(agent.GetAzureStorageShareStoredAccessPolicy(share.Name, samplePolicy.PolicyName),
                    "Get stored access policy in file share should succeed");
                Test.Info("Get stored access policy:{0}", samplePolicy.PolicyName);

                SharedAccessFilePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(samplePolicy.StartTime, samplePolicy.ExpiryTime, samplePolicy.Permission);
                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessFilePolicy>(policy, samplePolicy.PolicyName));
                agent.OutputValidation(comp);
            });
        }

        /// <summary>
        /// Test Plan 8.50 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void RemoveShareStoredPolicyTest()
        {
            SharedAccessPolicyTest((share, samplePolicies) =>
            {
                var samplePolicy = samplePolicies[0];
                Test.Assert(agent.NewAzureStorageShareStoredAccessPolicy(share.Name, samplePolicy.PolicyName, samplePolicy.Permission, samplePolicy.StartTime, samplePolicy.ExpiryTime),
                    "Create stored access policy in file share should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudFileShare>(share, samplePolicy);

                Test.Assert(agent.RemoveAzureStorageShareStoredAccessPolicy(share.Name, samplePolicy.PolicyName),
                    "Remove stored access policy in file share should succeed");
                Test.Info("Removed stored access policy:{0}", samplePolicy.PolicyName);

                Thread.Sleep(30000);

                FileSharePermissions permissions = share.GetPermissions();
                Test.Assert(!permissions.SharedAccessPolicies.ContainsKey(samplePolicy.PolicyName), "Policy {0} should not exist anymore.", samplePolicy.PolicyName);
            });
        }

        /// <summary>
        /// Test Plan 8.50 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(PsTag.StoredAccessPolicy)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StoredAccessPolicy)]
        public void SetShareStoredPolicyTest()
        {
            SharedAccessPolicyTest((share, samplePolicies) =>
            {
                var samplePolicy1 = samplePolicies[0];
                var samplePolicy2 = samplePolicies[1];
                Test.Assert(agent.NewAzureStorageShareStoredAccessPolicy(share.Name, samplePolicy1.PolicyName, samplePolicy1.Permission,
                    samplePolicy1.StartTime, samplePolicy1.ExpiryTime),
                    "Create stored access policy in file share should succeed");
                Test.Info("Created stored access policy:{0}", samplePolicy1.PolicyName);

                Utility.WaitForPolicyBecomeValid<CloudFileShare>(share, samplePolicy1);

                Test.Assert(agent.SetAzureStorageShareStoredAccessPolicy(share.Name, samplePolicy1.PolicyName, samplePolicy2.Permission,
                    samplePolicy2.StartTime, samplePolicy2.ExpiryTime),
                    "Set stored access policy in file share should succeed");
                Test.Info("Set stored access policy:{0}", samplePolicy1.PolicyName);

                Utility.RawStoredAccessPolicy expectedPolicy = Utility.GetExpectedStoredAccessPolicy(samplePolicy1, samplePolicy2);
                Utility.WaitForPolicyBecomeValid<CloudFileShare>(share, expectedPolicy);

                //get the policy and validate
                SharedAccessFilePolicies expectedPolicies = new SharedAccessFilePolicies();

                expectedPolicies.Add(samplePolicy1.PolicyName, Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(expectedPolicy.StartTime, expectedPolicy.ExpiryTime, expectedPolicy.Permission));
                Utility.ValidateStoredAccessPolicies<SharedAccessFilePolicy>(share.GetPermissions().SharedAccessPolicies, expectedPolicies);

                //validate the output
                SharedAccessFilePolicy policy = Utility.SetupSharedAccessPolicy<SharedAccessFilePolicy>(expectedPolicy.StartTime, expectedPolicy.ExpiryTime, expectedPolicy.Permission);

                Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
                comp.Add(Utility.ConstructGetPolicyOutput<SharedAccessFilePolicy>(policy, expectedPolicy.PolicyName));
                agent.OutputValidation(comp);
            });
        }

        /// <summary>
        /// Test Plan 8.61 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void StartCopyFromBlobToFile()
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName);
            string destShareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                CloudBlob blob = blobUtil.CreateRandomBlob(container, fileName, StorageBlob.BlobType.BlockBlob);
                CloudFile destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), fileName);

                Test.Assert(agent.StartFileCopyFromBlob(containerName, fileName, destShareName, fileName, Agent.Context),
                    "Start copy from blob to file shoule succeed.");

                Test.Assert(agent.GetFileCopyState(destShareName, fileName, Agent.Context, true),
                    "Get copy state of file should succeed.");

                CloudFileUtil.ValidateCopyResult(blob, destFile);

                fileName = Utility.GenNameString("fileName");
                blob = blobUtil.CreateRandomBlob(container, fileName, StorageBlob.BlobType.AppendBlob);
                destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), fileName);

                Test.Assert(agent.StartFileCopy(container, fileName, destShareName, fileName, Agent.Context),
                    "Start copy from blob to file shoule succeed.");

                Test.Assert(agent.GetFileCopyState(destShareName, fileName, Agent.Context, true),
                    "Get copy state of file should succeed.");

                CloudFileUtil.ValidateCopyResult(blob, destFile);

                string blobName = Utility.GenNameString("blobName");
                fileName = Utility.GenNameString("fileName");
                blob = blobUtil.CreateRandomBlob(container, fileName, StorageBlob.BlobType.PageBlob);
                destFile = fileUtil.GetFileReference(share.GetRootDirectoryReference(), fileName);
                Test.Assert(agent.StartFileCopy(container, fileName, destShareName, fileName, Agent.Context),
                    "Start copy from blob to file shoule succeed.");

                Test.Assert(agent.GetFileCopyState(destShareName, fileName, Agent.Context, true),
                    "Get copy state of file should succeed.");
                CloudFileUtil.ValidateCopyResult(blob, destFile);

            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        /// <summary>
        /// Test Plan 8.61 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void StartCopyFromFileToFile()
        {
            this.ValidateFileCopyFromFile((srcFile, destFile) =>
            {
                Test.Assert(agent.StartFileCopyFromFile(srcFile.Share.Name, CloudFileUtil.GetFullPath(srcFile), destFile.Share.Name, CloudFileUtil.GetFullPath(destFile), Agent.Context),
                    "Start copy from file to file should succeed.");

                Test.Assert(agent.GetFileCopyState(destFile, Agent.Context, true), "Get file copy state should succeed.");
            });


            this.ValidateFileCopyFromFile((srcFile, destFile) =>
            {
                Test.Assert(agent.StartFileCopy(srcFile.Share, CloudFileUtil.GetFullPath(srcFile), destFile.Share.Name, CloudFileUtil.GetFullPath(destFile), Agent.Context),
                    "Start copy from file to file should succeed.");

                Test.Assert(agent.GetFileCopyState(destFile, Agent.Context, true), "Get file copy state should succeed.");
            });

            this.ValidateFileCopyFromFile((srcFile, destFile) =>
            {
                string fileUri = agent.GetAzureStorageFileSasFromCmd(srcFile.Share.Name, CloudFileUtil.GetFullPath(srcFile), null, "r", null, DateTime.UtcNow.AddHours(1), true);

                Test.Assert(agent.StartFileCopy(fileUri, destFile), "Copy file to file with absolute URI should succeed.");
                Test.Assert(agent.GetFileCopyState(destFile, Agent.Context, true), "Get file copy state should succeed.");
            });
        }

        /// <summary>
        /// Anonymous storage context should work with specified end point
        /// </summary>
        [TestMethod()]
        [TestCategory(Tag.BVT)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StartCopyFile)]
        public void CopyFromPublicBlobToFile()
        {
            this.CopyFromPublicBlobToFile(StorageBlob.BlobType.AppendBlob);
            this.CopyFromPublicBlobToFile(StorageBlob.BlobType.BlockBlob);
            this.CopyFromPublicBlobToFile(StorageBlob.BlobType.PageBlob);
        }

        private void CopyFromPublicBlobToFile(StorageBlob.BlobType blobType)
        {
            string containerName = Utility.GenNameString("container");
            CloudBlobContainer container = blobUtil.CreateContainer(containerName, BlobContainerPublicAccessType.Blob);

            string destShareName = Utility.GenNameString("share");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                CloudBlob blob = blobUtil.CreateRandomBlob(container, fileName, blobType);

                var file = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), fileName);

                Test.Assert(agent.StartFileCopy(blob.Uri.ToString(), destShareName, fileName, Agent.Context),
                    "Start copying from public blob URI to file should succeed.");

                Test.Assert(agent.GetFileCopyState(file, Agent.Context, true), "Get file copying state should succeed.");

                CloudFileUtil.ValidateCopyResult(blob, file);
            }
            finally
            {
                blobUtil.RemoveContainer(containerName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        /// <summary>
        /// Test Plan 8.21 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.Blob)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.GetBlobCopyState)]
        public void CopyFromFileToBlob()
        {
            ValidateCopyFromFileToBlob((srcFile, destBlob) =>
            {
                Test.Info("Starting copying with share name and file path to blob");
                Test.Assert(agent.StartAzureStorageBlobCopyFromFile(srcFile.Share.Name, CloudFileUtil.GetFullPath(srcFile), destBlob.Container.Name, destBlob.Name, Agent.Context),
                    "Start copying from file to blob should succeed.");

                Test.Assert(agent.GetAzureStorageBlobCopyState(destBlob, null, true), "Get blob copy state should succeed.");
            });


            ValidateCopyFromFileToBlob((srcFile, destBlob) =>
            {
                Test.Info("Starting copying with share instance and file path to blob");
                Test.Assert(agent.StartAzureStorageBlobCopy(srcFile.Share, CloudFileUtil.GetFullPath(srcFile), destBlob.Container.Name, destBlob.Name, Agent.Context),
                    "Start copying from file to blob should succeed.");

                Test.Assert(agent.GetAzureStorageBlobCopyState(destBlob, null, true), "Get blob copy state should succeed.");
            });

            ValidateCopyFromFileToBlob((srcFile, destBlob) =>
            {
                Test.Info("Starting copying with file absolute uri to blob");
                string fileUriWithSAS = agent.GetAzureStorageFileSasFromCmd(srcFile.Share.Name, CloudFileUtil.GetFullPath(srcFile), null, "r", null, DateTime.UtcNow.AddHours(1), true);

                Test.Assert(agent.StartAzureStorageBlobCopy(fileUriWithSAS, destBlob.Container.Name, destBlob.Name, Agent.Context),
                    "Start copying from file to blob should succeed.");

                Test.Assert(agent.GetAzureStorageBlobCopyState(destBlob, null, true), "Get blob copy state should succeed.");
            });
        }

        /// <summary>
        /// Test Plan 8.21 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        public void CopyFromShareToBlob()
        {
            string srcShareName = Utility.GenNameString("share");
            CloudFileShare srcShare = fileUtil.EnsureFileShareExists(srcShareName);

            string destContainerName = Utility.GenNameString("container");
            CloudBlobContainer destContainer = blobUtil.CreateContainer(destContainerName);

            try
            {
                List<CloudFile> files = new List<CloudFile>();

                for (int i = 0; i < random.Next(1, 5); ++i)
                {
                    string fileName = Utility.GenNameString(string.Format("fileName{0}", i));
                    files.Add(fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), fileName));
                }

                PowerShellAgent psAgent = agent as PowerShellAgent;
                psAgent.AddPipelineScript(string.Format("Get-AzureStorageFile -ShareName {0}", srcShareName));

                Test.Assert(agent.StartAzureStorageBlobCopy(srcFile: null, destContainerName: destContainerName, destBlobName: null, destContext: Agent.Context),
                    "Start copying from file to blob should succeed.");

                psAgent.AddPipelineScript(string.Format("Get-AzureStorageBlob -Container {0}", destContainerName));

                Test.Assert(agent.GetAzureStorageBlobCopyState(blob: null, context: null, waitForComplete: true),
                    "Get blob copying state should succeed.");

                foreach (var file in files)
                {
                    ValidateCopyingResult(file, destContainer.GetBlobReference(CloudFileUtil.GetFullPath(file).Substring(1)));
                }
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(srcShareName);
                blobUtil.RemoveContainer(destContainerName);
            }
        }

        /// <summary>
        /// Test Plan 8.21 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.Blob)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.GetBlobCopyState)]
        public void GetStateOfBlobCopyFromFileTest()
        {
            string srcShareName = Utility.GenNameString("share");
            CloudFileShare srcShare = fileUtil.EnsureFileShareExists(srcShareName);

            string destContainerName = Utility.GenNameString("container");
            CloudBlobContainer destContainer = blobUtil.CreateContainer(destContainerName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                CloudFile file = fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), fileName);

                CloudBlockBlob blob = destContainer.GetBlockBlobReference(Utility.GenNameString("destBlobName"));

                Test.Assert(agent.StartAzureStorageBlobCopy(file, destContainer.Name, blob.Name, Agent.Context), "Start azure storage copy from file to blob should succeed.");

                Utility.WaitCopyToFinish(() =>
                {
                    blob.FetchAttributes();
                    return blob.CopyState;
                });


                VerifyGetCopyStateFinishInTime(() =>
                {
                    Test.Assert(agent.GetAzureStorageBlobCopyState(blob, null, true), "Get blob copy state should succeed.");
                });

                Utility.CLICopyState actualCopyState = Utility.GetCopyState(agent, lang);
                CopyState expectedCopyState = blob.CopyState;

                Utility.VerifyCopyState(expectedCopyState, actualCopyState);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(srcShareName);
                blobUtil.RemoveContainer(destContainerName);
            }
        }

        /// <summary>
        /// Test Plan 8.21 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.Blob)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StopCopyBlob)]
        public void StopBlobCopyFromFileTest()
        {
            string destContainerName = Utility.GenNameString("container");
            CloudBlobContainer destContainer = blobUtil.CreateContainer(destContainerName);

            try
            {
                string bigFileUri = Test.Data.Get("BigAzureFileUri");

                CloudBlockBlob blob = destContainer.GetBlockBlobReference(Utility.GenNameString("destBlobName"));

                Test.Assert(agent.StartAzureStorageBlobCopy(bigFileUri, destContainer.Name, blob.Name, Agent.Context), "Start azure storage copy from file to blob should succeed.");

                Test.Assert(agent.StopAzureStorageBlobCopy(destContainerName, blob.Name, null, true), "Stop blob copy should succeed.");

                blob.FetchAttributes();

                Test.Assert(blob.CopyState.Status == CopyStatus.Aborted, "Copy status should be aborted.");
            }
            finally
            {
                blobUtil.RemoveContainer(destContainerName);
            }
        }

        /// <summary>
        /// Test Plan 8.62 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.GetFileCopyState)]
        public void GetFileCopyStateTest()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                CloudFile file = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);

                Test.Assert(!agent.GetFileCopyState(shareName, fileName, Agent.Context), "Get file copy state should fail.");
                ExpectedContainErrorMessage("Can not find copy task on the specified file");

                VerifyGetCopyStateFinishInTime(() =>
                {
                    Test.Assert(!agent.GetFileCopyState(shareName, fileName, Agent.Context, true), "Get file copy state should fail.");
                });

                ExpectedContainErrorMessage("Can not find copy task on the specified file");

                string destFileName = Utility.GenNameString("destFileName");
                CloudFile destFile = fileUtil.CreateFile(share.GetRootDirectoryReference(), fileName);
                Test.Assert(agent.StartFileCopy(file, destFile), "Start file copy should succeed.");

                Utility.WaitCopyToFinish(() =>
                {
                    destFile.FetchAttributes();
                    return destFile.CopyState;
                });

                Test.Assert(agent.GetFileCopyState(file, Agent.Context), "Get file copy state should succeed.");

                Utility.VerifyCopyState(destFile.CopyState, Utility.GetCopyState(agent, lang));
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        /// <summary>
        /// Test Plan 8.63 BVT
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.File)]
        [TestCategory(PsTag.FileBVT)]
        [TestCategory(CLITag.File)]
        [TestCategory(CLITag.NodeJSBVT)]
        [TestCategory(CLITag.StopCopyFile)]
        public void StopFileCopyTest()
        {
            string shareName = Utility.GenNameString("share");
            CloudFileShare share = fileUtil.EnsureFileShareExists(shareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                CloudFile file = fileUtil.GetFileReference(share.GetRootDirectoryReference(), fileName);

                string bigBlobUri = Test.Data.Get("BigBlobUri");
                Test.Assert(agent.StartFileCopy(bigBlobUri, file), "Start file copy should succeed.");

                Test.Assert(agent.StopFileCopy(shareName, fileName, null), "Stop file copy should succeed.");

                file.FetchAttributes();
                Test.Assert(file.CopyState.Status == CopyStatus.Aborted, "copy state of the destination file should be aborted.");

                fileName = Utility.GenNameString("fileName2");
                file = fileUtil.GetFileReference(share.GetRootDirectoryReference(), fileName);

                string bigFileUri = Test.Data.Get("BigAzureFileUri");
                Test.Assert(agent.StartFileCopy(bigFileUri, file), "Start file copy from big file should succeed.");

                Test.Assert(agent.StopFileCopy(file, null), "Stop file copy should succeed.");

                file.FetchAttributes();
                Test.Assert(file.CopyState.Status == CopyStatus.Aborted, "copy state of the destination file should be aborted.");
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(shareName);
            }
        }

        private void VerifyGetCopyStateFinishInTime(Action getCopyState)
        {
            DateTimeOffset beginTime = DateTimeOffset.UtcNow;
            getCopyState();
            DateTimeOffset endTime = DateTimeOffset.UtcNow;
            Test.Assert(endTime - beginTime < TimeSpan.FromSeconds(5), "Get copy state should finish immediately");
        }

        private void ValidateCopyingResult(CloudFile srcFile, CloudBlob destBlob)
        {
            srcFile.FetchAttributes();
            destBlob.FetchAttributes();

            Test.Assert(destBlob.Properties.ContentMD5 == srcFile.Properties.ContentMD5, "MD5 should be the same.");
            Test.Assert(destBlob.Properties.ContentType == srcFile.Properties.ContentType, "Content type should be the same.");
            Test.Assert(destBlob.Properties.BlobType == StorageBlob.BlobType.BlockBlob, "Destination blob should be a block blob.");
        }

        private void ValidateCopyFromFileToBlob(Action<CloudFile, CloudBlob> blobCopyAction)
        {
            string srcShareName = Utility.GenNameString("share");
            CloudFileShare srcShare = fileUtil.EnsureFileShareExists(srcShareName);

            string destContainerName = Utility.GenNameString("container");
            CloudBlobContainer destContainer = blobUtil.CreateContainer(destContainerName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                CloudFile srcFile = fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), fileName);

                string blobName = Utility.GenNameString("blobName");
                CloudBlob destBlob = destContainer.GetBlockBlobReference(blobName);

                blobCopyAction(srcFile, destBlob);

                ValidateCopyingResult(srcFile, destBlob);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(srcShareName);
                blobUtil.RemoveContainer(destContainerName);
            }
        }

        private void ValidateFileCopyFromFile(Action<CloudFile, CloudFile> fileCopyAction)
        {
            string srcShareName = Utility.GenNameString("share");
            CloudFileShare srcShare = fileUtil.EnsureFileShareExists(srcShareName);

            string destShareName = Utility.GenNameString("share");
            CloudFileShare destShare = fileUtil.EnsureFileShareExists(destShareName);

            try
            {
                string fileName = Utility.GenNameString("fileName");
                CloudFile srcFile = fileUtil.CreateFile(srcShare.GetRootDirectoryReference(), fileName);

                string destFileName = Utility.GenNameString("fileName");
                CloudFile destFile = fileUtil.GetFileReference(destShare.GetRootDirectoryReference(), destFileName);

                fileCopyAction(srcFile, destFile);

                CloudFileUtil.ValidateCopyResult(srcFile, destFile);
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(srcShareName);
                fileUtil.DeleteFileShareIfExists(destShareName);
            }
        }

        private void NewDirectoryTest(Action<CloudFileShare, string> newDirectoryAction)
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            string directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);

            try
            {
                newDirectoryAction(fileShare, directoryName);

                var result = this.agent.Invoke();

                agent.AssertNoError();
                result.AssertObjectCollection(obj => result.AssertCloudFileDirectory(obj, directoryName), 1);
                fileUtil.AssertDirectoryExists(fileShare, directoryName, "Container should exist after created.");
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        private void RemoveDirectoryTest(Action<CloudFileDirectory> removeDirectoryAction)
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            string directoryName = CloudFileUtil.GenerateUniqueDirectoryName();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);
            var directory = fileUtil.EnsureDirectoryExists(fileShare, directoryName);

            try
            {
                removeDirectoryAction(directory);

                var result = agent.Invoke();

                agent.AssertNoError();
                result.AssertNoResult();
                fileUtil.AssertDirectoryNotExists(fileShare, directoryName, "Directory should not exist after removed.");
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        private void RemoveFileTest(Action<CloudFile> removeFileAction)
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            string fileName = CloudFileUtil.GenerateUniqueFileName();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);
            var file = fileUtil.CreateFile(fileShare, fileName);

            try
            {
                removeFileAction(file);

                var result = agent.Invoke();

                agent.AssertNoError();
                result.AssertNoResult();
                fileUtil.AssertFileNotExists(fileShare, fileName, "File should not exist after removed.");
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        private void GetFileTest(Action<CloudFileShare> getFileAction)
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            var fileNames = Enumerable.Range(0, random.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueFileName()).ToList();
            var directoryNames = Enumerable.Range(0, random.Next(5, 20)).Select(x => CloudFileUtil.GenerateUniqueDirectoryName()).ToList();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);
            var files = fileNames.Select(name => fileUtil.CreateFile(fileShare, name)).ToList();
            var directories = directoryNames.Select(name => fileUtil.EnsureDirectoryExists(fileShare, name)).ToList();

            try
            {
                getFileAction(fileShare);

                var result = agent.Invoke();

                agent.AssertNoError();
                result.AssertFileListItems(files, directories);
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        private void GetFileContentTest(string localFileName, string md5Checksum, Action<CloudFile, string> getContentAction)
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);
            var cloudFile = fileUtil.CreateFile(fileShare, cloudFileName, localFileName);
            var destination = Path.Combine(Test.Data.Get("TempDir"), FileUtil.GetSpecialFileName());

            try
            {
                agent = AgentFactory.CreateAgent(TestContext.Properties);
                getContentAction(cloudFile, destination);
                var result = agent.Invoke();
                agent.AssertNoError();
                if (lang == Language.NodeJS)
                {
                    result.AssertObjectCollection(obj => result.AssertCloudFile(obj, "/" + cloudFileName));
                }
                else
                {
                    result.AssertNoResult();
                }

                string destinationMD5 = FileUtil.GetFileContentMD5(destination);
                Test.Assert(
                    destinationMD5.Equals(md5Checksum, StringComparison.Ordinal),
                    "MD5 checksum of the downloaded file mismatches. Expected: {0}, Actural: {1}.",
                    md5Checksum,
                    destinationMD5);
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
                FileUtil.RemoveFile(destination);
            }
        }

        private void SetFileContentTest(string localFileName, string md5Checksum, Action<CloudFileShare, string> setContentAction)
        {
            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            string cloudFileName = CloudFileUtil.GenerateUniqueFileName();
            var fileShare = fileUtil.EnsureFileShareExists(fileShareName);

            try
            {
                agent = AgentFactory.CreateAgent(TestContext.Properties);
                setContentAction(fileShare, cloudFileName);
                var result = agent.Invoke();
                agent.AssertNoError();
                if (lang == Language.NodeJS)
                {
                    result.AssertObjectCollection(obj => result.AssertCloudFile(obj, "/" + cloudFileName));
                }
                else
                {
                    result.AssertNoResult();
                }

                var file = fileShare.GetRootDirectoryReference().GetFileReference(cloudFileName);
                Test.Assert(file.Exists(), "File should exist after setting content.");
                string contentMD5 = fileUtil.FetchFileMD5(file);
                Test.Assert(
                    contentMD5.Equals(md5Checksum, StringComparison.Ordinal),
                    "MD5 checksum of the uploaded file mismatches. Expected: {0}, Actural: {1}.",
                    md5Checksum,
                    contentMD5);
            }
            finally
            {
                agent.Dispose();
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        private void SharedAccessPolicyTest(Action<CloudFileShare, List<Utility.RawStoredAccessPolicy>> testAction)
        {
            if (!this.ShouldRunFileTest())
            {
                return;
            }

            if (this.TestContext.FullyQualifiedTestClassName.Contains("AzureEmulatorBVT"))
            {
                Test.Info("skip NewShareStoredPolicyTest as Azure emulator does not support stored access policy");
                return;
            }

            string fileShareName = CloudFileUtil.GenerateUniqueFileShareName();
            CloudFileShare share = fileUtil.EnsureFileShareExists(fileShareName);
            Utility.ClearStoredAccessPolicy<CloudFileShare>(share);

            try
            {
                testAction(share, Utility.SetUpStoredAccessPolicyData<SharedAccessFilePolicy>(lang == Language.NodeJS));
            }
            finally
            {
                fileUtil.DeleteFileShareIfExists(fileShareName);
            }
        }

        /// <summary>
        /// Determine whether the current configuration set allows to run
        /// test cases for cloud file services.
        /// </summary>
        /// <returns>
        /// Returns a value indicating whether the current configuration set
        /// allows cloud file services test cases to run.
        /// </returns>
        /// <remarks>
        /// For perview, the cloud file service does not support subscriptions
        /// and azure environments. So these test cases from BVT would be
        /// ignored.
        /// </remarks>
        private bool ShouldRunFileTest()
        {
            if (AllowedConfigurationSets.Contains(this.GetType()))
            {
                return true;
            }
            else
            {
                Test.Warn("Test case for cloud file services will not run since it does not support the configuration set {0}.", this.GetType().Name);
                return false;
            }
        }
    }
}
