namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;

    public class CloudFileUtil : UtilBase
    {
        public static readonly char[] PathSeparators = new char[] { '/', '\\' };

        private const string FileShareNameFormat = "fileshare{0}";

        private const string DirectoryNameForamt = "directory{0}";

        private const string FileNameForamt = "file{0}";

        private const string MockupAccountName = "InavlidAccount";

        private const string MockupAccountKey = @"FjUfNl1KiJttbXlsdkMzBTC7WagvrRM1/g6UPBuy0ypCpAbYTL6/KA+dI/7gyoWvLFYmah3IviUP1jykOHHOlA==";

        private static int seed;

        private CloudStorageAccount account;

        private CloudFileClient client;

        public CloudFileUtil(CloudStorageAccount account)
        {
            this.account = account;
            this.client = account.CreateCloudFileClient();
        }

        public CloudFileClient Client
        {
            get { return this.client; }
        }

        /// <summary>
        /// Gets the full path of a file/directory.
        /// </summary>
        /// <param name="item">Indicating the file/directory object.</param>
        /// <returns>Returns the full path.</returns>
        public static string GetFullPath(IListFileItem item)
        {
            UriBuilder shareUri = new UriBuilder(item.Share.Uri);
            if (!shareUri.Path.EndsWith("/", StringComparison.Ordinal))
            {
                shareUri.Path = string.Concat(shareUri.Path, "/");
            }

            return shareUri.Uri.MakeRelativeUri(item.Uri).ToString();
        }

        /// <summary>
        /// Gets the base name of a CloudFile item.
        /// </summary>
        /// <param name="file">Indicating the CloudFile item.</param>
        /// <returns>Returns the base name.</returns>
        /// <remarks>
        /// This is to work around XSCL bug 1391878 where CloudFile.Name
        /// sometimes returns base name and sometimes returns full path.
        /// </remarks>
        public static string GetBaseName(CloudFile file)
        {
            Test.Assert(!string.IsNullOrEmpty(file.Name), "CloudFile.Name should never return null.");
            return file.Name.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        /// <summary>
        /// Gets the full path of a directory.
        /// </summary>
        /// <param name="item">Indicating the directory object.</param>
        /// <returns>Returns the full path.</returns>
        public static string GetFullPath(CloudFileDirectory item)
        {
            return GetFullPathInternal(item, new List<string>());
        }

        public static CloudStorageAccount MockupStorageAccount(CloudStorageAccount baseAccount, bool mockupAccountName = false, bool mockupAccountKey = false)
        {
            string accountName = mockupAccountName ? MockupAccountName : baseAccount.Credentials.AccountName;
            string key = mockupAccountKey ? MockupAccountKey : baseAccount.Credentials.ExportBase64EncodedKey();
            var account = new CloudStorageAccount(
                new StorageCredentials(accountName, key),
                mockupAccountName ? MockupStorageUri(baseAccount.BlobStorageUri, baseAccount.Credentials.AccountName, accountName) : baseAccount.BlobStorageUri,
                mockupAccountName ? MockupStorageUri(baseAccount.QueueStorageUri, baseAccount.Credentials.AccountName, accountName) : baseAccount.QueueStorageUri,
                mockupAccountName ? MockupStorageUri(baseAccount.TableStorageUri, baseAccount.Credentials.AccountName, accountName) : baseAccount.TableStorageUri,
                mockupAccountName ? MockupStorageUri(baseAccount.FileStorageUri, baseAccount.Credentials.AccountName, accountName) : baseAccount.FileStorageUri);

            return account;
        }

        public static string GenerateUniqueFileShareName()
        {
            return string.Format(FileShareNameFormat, Interlocked.Increment(ref seed));
        }

        public static string GenerateUniqueDirectoryName()
        {
            return string.Format(DirectoryNameForamt, Interlocked.Increment(ref seed));
        }

        public static string GenerateUniqueFileName()
        {
            return string.Format(FileNameForamt, Interlocked.Increment(ref seed));
        }

        public void AssertFileShareExists(string fileShareName, string message)
        {
            var container = this.client.GetShareReference(fileShareName);
            Test.Assert(container.Exists(), message);
        }

        public void AssertDirectoryExists(CloudFileShare share, string directoryPath, string message)
        {
            Test.Assert(share.GetRootDirectoryReference().GetDirectoryReference(directoryPath).Exists(), message);
        }

        public void AssertDirectoryNotExists(CloudFileShare share, string directoryPath, string message)
        {
            Test.Assert(!share.GetRootDirectoryReference().GetDirectoryReference(directoryPath).Exists(), message);
        }

        public void AssertFileNotExists(CloudFileShare share, string filePath, string message)
        {
            Test.Assert(!share.GetRootDirectoryReference().GetFileReference(filePath).Exists(), message);
        }

        public void AssertFileExists(CloudFileShare share, string filePath, string message)
        {
            Test.Assert(share.GetRootDirectoryReference().GetFileReference(filePath).Exists(), message);
        }

        public void DeleteFileShareIfExists(string fileShareName)
        {
            var container = this.client.GetShareReference(fileShareName);
            container.DeleteIfExists();
        }

        public void DeleteDirectoryIfExists(CloudFileShare fileShare, string directoryName)
        {
            fileShare.GetRootDirectoryReference().GetDirectoryReference(directoryName).DeleteIfExists();
        }

        public CloudFile DeleteFileIfExists(CloudFileShare fileShare, string fileName)
        {
            var file = fileShare.GetRootDirectoryReference().GetFileReference(fileName);
            file.DeleteIfExists();
            return file;
        }

        public IEnumerable<CloudFileShare> ListShares(string prefix)
        {
            return this.client.ListShares(prefix, ShareListingDetails.All);
        }

        public CloudFileShare EnsureFileShareExists(string fileShareName)
        {
            const int retryInterval = 5000;
            const int retryLimit = 10;
            var container = this.client.GetShareReference(fileShareName);

            bool succeeded = false;
            for (int i = 0; i < retryLimit; i++)
            {
                try
                {
                    container.CreateIfNotExists();
                    succeeded = true;
                    break;
                }
                catch (StorageException e)
                {
                    if (e.RequestInformation != null &&
                        e.RequestInformation.ExtendedErrorInformation != null &&
                        e.RequestInformation.ExtendedErrorInformation.ErrorCode == AssertUtil.ShareBeingDeletedFullQualifiedErrorId)
                    {
                        Test.Info("Round {0}: Failed to prepare file share {1} because it is being deleted. Wait for {2} milliseconds and retry.", i, fileShareName, retryInterval);
                        Thread.Sleep(retryInterval);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (!succeeded)
            {
                Test.Error("Failed to prepare file share {0} within {1} retries.", fileShareName, retryLimit);
                throw new InvalidOperationException("Failed to prepare file share.");
            }

            return container;
        }

        public CloudFileDirectory EnsureFolderStructure(CloudFileShare share, string directoryPath)
        {
            var directory = share.GetRootDirectoryReference();
            foreach (var directoryName in directoryPath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                directory = directory.GetDirectoryReference(directoryName);
                directory.CreateIfNotExists();
            }

            return directory;
        }

        public CloudFileDirectory EnsureDirectoryExists(CloudFileShare share, string directoryName)
        {
            var directory = share.GetRootDirectoryReference().GetDirectoryReference(directoryName);
            directory.CreateIfNotExists();
            return directory;
        }

        public CloudFile CreateFile(CloudFileShare fileShare, string fileName, string source = null)
        {
            var file = fileShare.GetRootDirectoryReference().GetFileReference(fileName.Trim('/'));
            PrepareFileInternal(file, source);
            return file;
        }

        public CloudFile CreateFile(CloudFileDirectory directory, string fileName, string source = null)
        {
            var file = directory.GetFileReference(fileName);
            PrepareFileInternal(file, source);
            return file;
        }

        public void AssertFileShareNotExists(string fileShareName, string message)
        {
            var container = this.client.GetShareReference(fileShareName);
            Test.Assert(!container.Exists(), message);
        }

        public string FetchFileMD5(CloudFile file)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                file.DownloadRangeToStream(ms, null, null);
                MD5 md5Computer = MD5.Create();
                ms.Seek(0, SeekOrigin.Begin);
                return Convert.ToBase64String(md5Computer.ComputeHash(ms));
            }
        }

        public void CleanupDirectory(CloudFileDirectory directory)
        {
            foreach (var item in directory.ListFilesAndDirectories())
            {
                CloudFile file = item as CloudFile;
                if (file != null)
                {
                    file.Delete();
                }
                else
                {
                    CloudFileDirectory dir = item as CloudFileDirectory;
                    if (dir != null)
                    {
                        this.CleanupDirectory(dir);
                        dir.Delete();
                    }
                }
            }
        }


        /// <summary>
        /// Implementation of get full path.
        /// </summary>
        /// <param name="item">Indicating the directory item.</param>
        /// <param name="itemNames">
        /// Indicating a list of names (of the nested folders).
        /// </param>
        /// <returns>Returns the full path.</returns>
        private static string GetFullPathInternal(CloudFileDirectory item, List<string> itemNames)
        {
            itemNames.Add(item.Name);
            var parent = item.Parent;
            while (parent != null && !string.IsNullOrEmpty(parent.Name))
            {
                itemNames.Add(parent.Name);
                parent = parent.Parent;
            }

            return string.Join("/", itemNames.Reverse<string>());
        }

        private static StorageUri MockupStorageUri(StorageUri originalUri, string originalAccountName, string newAccountName)
        {
            return new StorageUri(
                MockupStorageUri(originalUri.PrimaryUri, originalAccountName, newAccountName),
                MockupStorageUri(originalUri.SecondaryUri, originalAccountName, newAccountName));
        }

        private static Uri MockupStorageUri(Uri originalUri, string originalAccountName, string newAccountName)
        {
            if (originalUri == null)
            {
                return null;
            }

            return new Uri(originalUri.ToString().Replace(originalAccountName, newAccountName));
        }

        private static void PrepareFileInternal(CloudFile file, string source)
        {
            if (source == null)
            {
                file.Create(1024);
            }
            else
            {
                Test.Info("Upload source file {0} to destination {1}.", source, file.Uri.OriginalString);
                
                if (AgentOSType != OSType.Windows)
                {
                    string argument;
                    string directory = string.Empty;
                    var parent = file.Parent;
                    while (parent != null && !string.IsNullOrEmpty(parent.Name))
                    {
                        directory = parent.Name + "/" + directory;
                        parent = parent.Parent;
                    }

                    string fileName = file.Name;
                    if (!string.IsNullOrEmpty(directory))
                    {
                        argument = string.Format("azure storage directory create '{0}' '{1}'", file.Share.Name, directory);
                        argument = AddAccountParameters(argument, AgentConfig);
                        RunNodeJSProcess(argument, true);

                        // when CloudFile is referenced from root directory
                        if (!fileName.Contains("/") || !fileName.StartsWith(directory))
                        {
                            fileName = directory.Trim('/') + '/' + fileName;
                        }
                    }

                    argument = string.Format("azure storage file upload '{0}' {1} {2} -q", source, file.Share.Name, fileName);
                    argument = AddAccountParameters(argument, AgentConfig);
                    RunNodeJSProcess(argument, true);
                }

                if (AgentOSType == OSType.Windows)
                {
                    file.Create(FileUtil.GetFileSize(source));
                    file.UploadFromFile(source, FileMode.Open);
                }
            }
        }
    }
}
