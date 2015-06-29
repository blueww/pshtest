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
    using Management.Storage.ScenarioTest.Common;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
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

        private static char[] InvalidPathChars = new char[] { '"', '\\', ':', '|', '<', '>', '*', '?' };

        private static int seed;

        private CloudStorageAccount account;

        private CloudFileClient client;

        private Random rd = new Random();

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

        public static string GetFullPath(CloudFile file)
        { 
            List<string> fileName = new List<string>();
            fileName.Add(file.Name);

            return GetFullPathInternal(file.Parent, fileName);
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
            return Utility.GenNameString("fileshare");
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
            var share = this.client.GetShareReference(fileShareName);
            Test.Assert(share.Exists(), message);
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

        public void DeleteFileShareIfExistsWithSleep(string fileShareName)
        {
            var fileShare = this.client.GetShareReference(fileShareName);
            if (fileShare.Exists())
            {
                fileShare.Delete();
                System.Threading.Thread.Sleep(60000);
            }
        }

        public void DeleteFileShareIfExists(string fileShareName)
        {
            var share = this.client.GetShareReference(fileShareName);
            share.DeleteIfExists();
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

        public CloudFileShare GetShareReference(string shareName)
        {
            return this.client.GetShareReference(shareName);
        }

        public CloudFileShare EnsureFileShareExists(string fileShareName)
        {
            const int retryInterval = 5000;
            const int retryLimit = 10;
            var share = this.client.GetShareReference(fileShareName);

            bool succeeded = false;
            for (int i = 0; i < retryLimit; i++)
            {
                try
                {
                    share.CreateIfNotExists();
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

            return share;
        }

        public bool FileShareExists(string fileShareName)
        {
            var share = this.client.GetShareReference(fileShareName);
            return share.Exists();
        }

        public void CreateFileFolders(CloudFileShare share, string filePath)
        {
            int lastDirSeparator = filePath.LastIndexOf('/');

            if (-1 != lastDirSeparator)
            {
                EnsureFolderStructure(share, filePath.Substring(0, lastDirSeparator));
            }
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
            return this.CreateFile(fileShare.GetRootDirectoryReference(), fileName, source);
        }

        public CloudFile CreateFile(CloudFileDirectory directory, string fileName, string source = null)
        {
            string[] path = fileName.Split('/');

            for (int i = 0; i < path.Length - 1; ++i)
            {
                if (!string.IsNullOrWhiteSpace(path[i]))
                {
                    directory = directory.GetDirectoryReference(path[i]);
                    directory.CreateIfNotExists();
                }
            }

            var file = directory.GetFileReference(path[path.Length - 1]);
            PrepareFileInternal(file, source);
            return file;
        }

        public void AssertFileShareNotExists(string fileShareName, string message)
        {
            var share = this.client.GetShareReference(fileShareName);
            Test.Assert(!share.Exists(), message);
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

        public CloudFile GetFileReference(CloudFileDirectory dir, string filePath)
        {
            string[] path = filePath.Split('/');

            var localDir = dir;

            for (int i = 0; i < path.Length - 1; ++i)
            {
                if (!string.IsNullOrWhiteSpace(path[i]))
                {
                    localDir = localDir.GetDirectoryReference(path[i]);
                }
            }

            return localDir.GetFileReference(path[path.Length - 1]);
        }

        public string ResolveFileName(CloudBlob blob)
        {
            // 1) Unescape original string, original string is UrlEncoded.
            // 2) Replace Azure directory separator with Windows File System directory separator.
            // 3) Trim spaces at the end of the file name.
            string destinationRelativePath = EscapeInvalidCharacters(blob.Name);

            // Split into path + filename parts.
            int lastSlash = destinationRelativePath.LastIndexOf("/", StringComparison.Ordinal);

            string destinationFileName;
            string destinationPath;

            if (-1 == lastSlash)
            {
                destinationPath = string.Empty;
                destinationFileName = destinationRelativePath;
            }
            else
            {
                destinationPath = destinationRelativePath.Substring(0, lastSlash); // Don't include slash in the path
                destinationFileName = destinationRelativePath.Substring(lastSlash + 1);
            }

            // Append snapshot time to filename.
            destinationFileName = AppendSnapShotToFileName(destinationFileName, blob.SnapshotTime);

            // Combine path and filename back together again.
            if (string.IsNullOrEmpty(destinationPath))
            {
                destinationRelativePath = destinationFileName;
            }
            else
            {
                destinationRelativePath = string.Format("{0}/{1}", destinationPath, destinationFileName);
            }

            destinationRelativePath = ResolveFileNameSuffix(destinationRelativePath);

            return destinationRelativePath;
        }

        private string ResolveFileNameSuffix(string baseFileName)
        {
            // TODO - MaxFileNameLength could be <= 0.
            int maxFileNameLength = 1024;

            if (baseFileName.Length > maxFileNameLength)
            {
                string postfixString = string.Format(" (1)");

                string pathAndFilename = Path.ChangeExtension(baseFileName, null);
                string extension = Path.GetExtension(baseFileName);

                string resolvedName = string.Empty;

                // TODO - trimLength could be be larger than pathAndFilename.Length, what do we do in this case?
                int trimLength = (pathAndFilename.Length + postfixString.Length + extension.Length) - maxFileNameLength;

                if (trimLength > 0)
                {
                    pathAndFilename = pathAndFilename.Remove(pathAndFilename.Length - trimLength);
                }

                return string.Format("{0}{1}{2}", pathAndFilename, postfixString, extension);
            }

            return baseFileName;
        }

        private string EscapeInvalidCharacters(string fileName)
        {            
            StringBuilder sb = new StringBuilder();
            char separator = '/';
            string escapedSeparator = string.Format("%{0:X2}", (int)separator);

            bool followSeparator = false;
            char[] fileNameChars = fileName.ToCharArray();
            int lastIndex = fileNameChars.Length - 1;

            for (int i = 0; i < fileNameChars.Length; ++i)
            {
                if (fileNameChars[i] == separator)
                {
                    if (followSeparator || (0 == i) || (lastIndex == i))
                    {
                        sb.Append(escapedSeparator);
                    }
                    else
                    {
                        sb.Append(fileNameChars[i]);
                    }

                    followSeparator = true;
                }
                else
                {
                    followSeparator = false;
                    sb.Append(fileNameChars[i]);
                }
            }

            fileName = sb.ToString();

            if (null != InvalidPathChars)
            {
                // Replace invalid characters with %HH, with HH being the hexadecimal
                // representation of the invalid character.
                foreach (char c in InvalidPathChars)
                {
                    fileName = fileName.Replace(c.ToString(), string.Format("%{0:X2}", (int)c));
                }
            }

            return fileName;
        }

        /// <summary>
        /// Append snapshot time to a file name.
        /// </summary>
        /// <param name="fileName">Original file name.</param>
        /// <param name="snapshotTime">Snapshot time to append.</param>
        /// <returns>A file name with appended snapshot time.</returns>
        private static string AppendSnapShotToFileName(string fileName, DateTimeOffset? snapshotTime)
        {
            string resultName = fileName;

            if (snapshotTime.HasValue)
            {
                string pathAndFileNameNoExt = Path.ChangeExtension(fileName, null);
                string extension = Path.GetExtension(fileName);
                string timeStamp = string.Format("{0:u}", snapshotTime.Value);

                resultName = string.Format(
                    "{0} ({1}){2}",
                    pathAndFileNameNoExt,
                    timeStamp.Replace(":", string.Empty).TrimEnd(new char[] { 'Z' }),
                    extension);
            }

            return resultName;
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

        private void PrepareFileInternal(CloudFile file, string source)
        {
            FileRequestOptions options = new FileRequestOptions() { StoreFileContentMD5 = true };
            if (source == null)
            {
                int buffSize = 1024;
                byte[] buffer = new byte[buffSize];
                rd.NextBytes(buffer);
                file.UploadFromByteArray(buffer, 0, buffSize, options: options);
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
                    file.UploadFromFile(source, FileMode.Open, options: options);
                }
            }

            GeneratePropertiesAndMetaData(file);
        }

        private void GeneratePropertiesAndMetaData(CloudFile file)
        {
            file.Properties.ContentEncoding = Utility.GenNameString("encoding");
            file.Properties.ContentLanguage = Utility.GenNameString("lang");

            int minMetaCount = 1;
            int maxMetaCount = 5;
            int minMetaValueLength = 10;
            int maxMetaValueLength = 20;
            int count = rd.Next(minMetaCount, maxMetaCount);

            for (int i = 0; i < count; i++)
            {
                string metaKey = Utility.GenNameString("metatest");
                int valueLength = rd.Next(minMetaValueLength, maxMetaValueLength);
                string metaValue = Utility.GenNameString("metavalue-", valueLength);
                file.Metadata.Add(metaKey, metaValue);
            }

            file.SetProperties();
            file.SetMetadata();
            file.FetchAttributes();
        }

        public void ValidateShareReadableWithSasToken(CloudFileShare share, string fileName, string sasToken)
        {
            Test.Info("Verify share read permission");
            CloudFile file = share.GetRootDirectoryReference().GetFileReference(fileName);

            if (!file.Exists())
            {
                PrepareFileInternal(file, null);
            }

            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(share.ServiceClient.Credentials.AccountName, sasToken);
            CloudFileClient sasClient = sasAccount.CreateCloudFileClient();
            CloudFileShare sasShare = sasClient.GetShareReference(share.Name);
            CloudFile sasFile = sasShare.GetRootDirectoryReference().GetFileReference(fileName);
            sasFile.FetchAttributes();
            long buffSize = sasFile.Properties.Length;
            byte[] buffer = new byte[buffSize];
            MemoryStream ms = new MemoryStream(buffer);
            sasFile.DownloadRangeToStream(ms, 0, buffSize);
            string md5 = Utility.GetBufferMD5(buffer);
            TestBase.ExpectEqual(sasFile.Properties.ContentMD5, md5, "content md5");
        }

        public void ValidateShareWriteableWithSasToken(CloudFileShare share, string sastoken)
        {
            Test.Info("Verify share write permission");
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(share.ServiceClient.Credentials.AccountName, sastoken);
            CloudFileShare sasShare = sasAccount.CreateCloudFileClient().GetShareReference(share.Name);
            string sasFileName = Utility.GenNameString("sasFile");
            CloudFile sasFile = sasShare.GetRootDirectoryReference().GetFileReference(sasFileName);
            long fileSize = 1024 * 1024;
            sasFile.Create(fileSize);
            CloudFile retrievedFile = share.GetRootDirectoryReference().GetFileReference(sasFileName);
            retrievedFile.FetchAttributes();
            TestBase.ExpectEqual(fileSize, retrievedFile.Properties.Length, "blob size");
        }

        public void ValidateShareDeleteableWithSasToken(CloudFileShare share, string sastoken)
        {
            Test.Info("Verify share delete permission");
            string fileName = Utility.GenNameString("file");
            CloudFile file = CreateFile(share, fileName);

            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(share.ServiceClient.Credentials.AccountName, sastoken);
            CloudFileShare sasShare = sasAccount.CreateCloudFileClient().GetShareReference(share.Name);

            CloudFile sasFile = sasShare.GetRootDirectoryReference().GetFileReference(fileName);
            sasFile.Delete();

            Test.Assert(!file.Exists(), "blob should not exist");
        }

        public void ValidateShareListableWithSasToken(CloudFileShare share, string sastoken)
        {
            Test.Info("Verify share list permission");
            string fileName = Utility.GenNameString("file");
            CloudFile file = CreateFile(share, fileName);

            int fileCount = share.GetRootDirectoryReference().ListFilesAndDirectories().Count();
            CloudStorageAccount sasAccount = TestBase.GetStorageAccountWithSasToken(share.ServiceClient.Credentials.AccountName, sastoken);
            CloudFileShare sasShare = sasAccount.CreateCloudFileClient().GetShareReference(share.Name);
            int retrievedFileCount = sasShare.GetRootDirectoryReference().ListFilesAndDirectories().Count();
            TestBase.ExpectEqual(fileCount, retrievedFileCount, "File count");
        }

        /// <summary>
        /// Validate the read permission in the sas token for the the specified file
        /// </summary>
        public void ValidateFileReadableWithSasToken(CloudFile file, string sasToken)
        {
            Test.Info("Verify file read permission");
            CloudFile sasFile = new CloudFile(file.Uri, new StorageCredentials(sasToken));
            sasFile.FetchAttributes();
            long buffSize = sasFile.Properties.Length;
            byte[] buffer = new byte[buffSize];
            MemoryStream ms = new MemoryStream(buffer);
            sasFile.DownloadRangeToStream(ms, 0, buffSize);
            string md5 = Utility.GetBufferMD5(buffer);
            TestBase.ExpectEqual(sasFile.Properties.ContentMD5, md5, "content md5");
        }

        /// <summary>
        /// Validate the write permission in the sas token for the the specified file
        /// </summary>
        internal void ValidateFileWriteableWithSasToken(CloudFile file, string sasToken)
        {
            Test.Info("Verify file write permission");
            CloudFile sasFile = new CloudFile(file.Uri, new StorageCredentials(sasToken));
            DateTimeOffset? lastModifiedTime = sasFile.Properties.LastModified;
            long buffSize = 1024 * 1024;
            byte[] buffer = new byte[buffSize];
            (new Random()).NextBytes(buffer);
            MemoryStream ms = new MemoryStream(buffer);
            sasFile.UploadFromStream(ms);
            file.FetchAttributes();
            DateTimeOffset? newModifiedTime = file.Properties.LastModified;
            TestBase.ExpectNotEqual(lastModifiedTime.ToString(), newModifiedTime.ToString(), "Last modified time");
        }

        /// <summary>
        /// Validate the delete permission in the sas token for the the specified file
        /// </summary>
        internal void ValidateFileDeleteableWithSasToken(CloudFile file, string sasToken)
        {
            Test.Info("Verify file delete permission");
            Test.Assert(file.Exists(), "The file should exist");
            CloudFile sasFile = new CloudFile(file.Uri, new StorageCredentials(sasToken));
            sasFile.Delete();
            Test.Assert(!file.Exists(), "The file should not exist after deleting with sas token");
        }
    }
}
