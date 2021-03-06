﻿namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Management.Automation;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;

    internal static class AssertUtil
    {
        public const string ShareBeingDeletedFullQualifiedErrorId = "ShareBeingDeleted";

        public const string ShareNotFoundFullQualifiedErrorId = "ShareNotFound";

        public const string ShareAlreadyExistsFullQualifiedErrorId = "ShareAlreadyExists";

        public const string AccountIsDisabledFullQualifiedErrorId = "AccountIsDisabled";

        public const string AuthenticationFailedFullQualifiedErrorId = "AuthenticationFailed";

        public const string ResourceAlreadyExistsFullQualifiedErrorId = "ResourceAlreadyExists";

        public const string InvalidArgumentFullQualifiedErrorId = "InvalidArgument";

        public const string ResourceNotFoundFullQualifiedErrorId = "ResourceNotFound";

        public const string InvalidOperationExceptionFullQualifiedErrorId = "InvalidOperationException";

        public const string TransferExceptionFullQualifiedErrorId = "TransferException";

        public const string DirectoryNotEmptyFullQualifiedErrorId = "DirectoryNotEmpty";

        public const string ParentNotFoundFullQualifiedErrorId = "ParentNotFound";

        public const string ProtocolErrorFullQualifiedErrorId = "ProtocolError+StorageException";

        public const string NameResolutionFailureFullQualifiedErrorId = "NameResolutionFailure";

        public const string InvalidResourceFullQualifiedErrorId = "InvalidResource+InvalidUri+OutOfRangeInput+UnsupportedHttpVerb+StorageException+Forbidden";

        public const string ParameterArgumentValidationErrorFullQualifiedErrorId = "ParameterArgumentValidationError";

        public const string MissingMandatoryParameterFullQualifiedErrorId = "MissingMandatoryParameter";

        public const string PathNotFoundFullQualifiedErrorId = "PathNotFound";

        public const string InvalidFileOrDirectoryPathNameFullQualifiedErrorId = "InvalidFileOrDirectoryPathName";

        public static readonly char[] PathSeparators = new char[] { '/', '\\' };

        public static void AssertCloudFileDirectory(this object directoryObj, string directoryPath)
        {
            var directoryObject = ((PSObject)directoryObj).ImmediateBaseObject as CloudFileDirectory;
            Test.Assert(directoryObject != null, "Output object should be an instance of CloudFileDirectory.");
            string fullPath = CloudFileUtil.GetFullPath(directoryObject).Trim(PathSeparators);
            Test.Assert(fullPath.Equals(directoryPath.Trim(PathSeparators), StringComparison.OrdinalIgnoreCase), "Prefix of the directory object should match the given parameter. Expected: {0}, Actual: {1}", directoryPath, fullPath);
        }

        public static void AssertCloudFileDirectory(this object directoryObj, List<string> directoryPathes)
        {
            var directoryObject = ((PSObject)directoryObj).ImmediateBaseObject as CloudFileDirectory;
            Test.Assert(directoryObject != null, "Output object should be an instance of CloudFileDirectory.");
            string fullPath = CloudFileUtil.GetFullPath(directoryObject).Trim(PathSeparators);
            Test.Assert(directoryPathes.Remove(fullPath), "Prefix of the directory object '{0}' should be within the given collection.", fullPath);
        }

        public static void AssertCloudFile(this object fileObj, string fileName, string path = null)
        {
            var fileObject = ((PSObject)fileObj).ImmediateBaseObject as CloudFile;
            Test.Assert(fileObject != null, "Output object should be an instance of CloudFile.");

            // FIXME: Walk around a issue in XSCL where the CloudFile.Name property sometimes returns
            // full path of the file.
            string fileObjectName = fileObject.Name.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries).Last();

            Test.Assert(fileObjectName.Equals(fileName, StringComparison.OrdinalIgnoreCase), "Name of the file object should match the given parameter. Expected: {0}, Actual: {1}", fileName, fileObjectName);
            if (path != null)
            {
                string fullPath = CloudFileUtil.GetFullPath(fileObject.Parent).Trim(PathSeparators);
                Test.Assert(fileObject.Parent != null, "Since file is not on root folder, the parent directory should not be null.");
                Test.Assert(fullPath.Equals(path.Trim(PathSeparators), StringComparison.OrdinalIgnoreCase), "Prefix of the directory object should match the path of the file. Expected: {0}, Actual: {1}", path, fullPath);
            }
        }

        public static void AssertCloudFile(this object fileObj, List<CloudFile> files)
        {
            var fileObject = ((PSObject)fileObj).ImmediateBaseObject as CloudFile;
            Test.Assert(fileObject != null, "Output object should be an instance of CloudFile.");

            string fileObjectFullName = CloudFileUtil.GetFullPath(fileObject);

            CloudFile matchingFile = files.FirstOrDefault(file => CloudFileUtil.GetFullPath(file).Equals(fileObjectFullName, StringComparison.OrdinalIgnoreCase));
            Test.Assert(matchingFile != null, "Output CloudFile object {0} was not found in the expecting list.", fileObjectFullName);
            files.Remove(matchingFile);
        }
    }
}
