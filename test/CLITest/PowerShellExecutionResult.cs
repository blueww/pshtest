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

namespace Management.Storage.ScenarioTest
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Management.Automation;
    using System.Text;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;

    internal sealed class PowerShellExecutionResult : IExecutionResult, IEnumerable<PSObject>
    {
        private Collection<PSObject> result;

        public PowerShellExecutionResult(Collection<PSObject> result)
        {
            this.result = result;
        }

        public IEnumerator<PSObject> GetEnumerator()
        {
            return this.result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.result.GetEnumerator();
        }

        public void AssertNoResult()
        {
            Test.Assert(this.result.Count() == 0, "Number of output objects does not match the expectation. Expected: {0}, Actual: {1}", 0, this.result.Count());
        }

        public void AssertObjectCollection(Action<object> assertAction, int assertNumber = 1)
        {
            if (assertNumber > 0)
            {
                Test.Assert(this.result.Count() == assertNumber, "Number of output objects does not match the expectation. Expected: {0}, Actual: {1}", assertNumber, this.result.Count());
            }

            foreach (var psObject in this.result)
            {
                assertAction(psObject);
            }
        }

        public void AssertFileListItems(IEnumerable<CloudFile> files, IEnumerable<CloudFileDirectory> directories)
        {
            var fileList = new List<string>(files.Select(x => CloudFileUtil.GetFullPath(x).Trim(CloudFileUtil.PathSeparators)));
            var directoryList = new List<string>(directories.Select(x => CloudFileUtil.GetFullPath(x).Trim(CloudFileUtil.PathSeparators)));

            foreach (var psObject in this.result)
            {
                string fullPath;
                List<string> expectedList;
                if (psObject.ImmediateBaseObject is CloudFile)
                {
                    var fileObject = (CloudFile)psObject.ImmediateBaseObject;
                    fullPath = CloudFileUtil.GetFullPath(fileObject).Trim(CloudFileUtil.PathSeparators);
                    expectedList = fileList;
                }
                else if (psObject.ImmediateBaseObject is CloudFileDirectory)
                {
                    var directoryObject = (CloudFileDirectory)psObject.ImmediateBaseObject;
                    fullPath = CloudFileUtil.GetFullPath(directoryObject).Trim(CloudFileUtil.PathSeparators);
                    expectedList = directoryList;
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Unexpected output object: {0}.", psObject.ImmediateBaseObject));
                }

                Test.Assert(expectedList.Remove(fullPath), "Path {0} was found in the expected list.", fullPath);
            }

            Test.Assert(fileList.Count == 0, "{0} leftover items in file list.", fileList.Count);
            Test.Assert(directoryList.Count == 0, "{0} leftover items in directory list.", directoryList.Count);
        }

        public void AssertCloudFileContainer(object containerObj, string fileShareName, int expectedUsage = 0)
        {
            var containerObject = ((PSObject)containerObj).ImmediateBaseObject as CloudFileShare;
            Test.Assert(containerObject != null, "Output object should be an instance of CloudFileShare.");
            Test.Assert(containerObject.Name.Equals(fileShareName, StringComparison.OrdinalIgnoreCase), "Name of the container object should match the given parameter. Expected: {0}, Actual: {1}", fileShareName, containerObject.Name);
        }

        public void AssertCloudFileContainer(object containerObj, List<string> fileShareNames, bool failIfNotInGivenList = true)
        {
            var containerObject = ((PSObject)containerObj).ImmediateBaseObject as CloudFileShare;
            Test.Assert(containerObject != null, "Output object should be an instance of CloudFileShare.");
            bool withInGivenList = fileShareNames.Remove(containerObject.Name);
            if (failIfNotInGivenList)
            {
                Test.Assert(withInGivenList, "Name of the container '{0}' should be within the given collection.", containerObject.Name);
            }
        }

        public void AssertCloudFileDirectory(object directoryObj, string directoryPath)
        {
            var directoryObject = ((PSObject)directoryObj).ImmediateBaseObject as CloudFileDirectory;
            Test.Assert(directoryObject != null, "Output object should be an instance of CloudFileDirectory.");
            string fullPath = CloudFileUtil.GetFullPath(directoryObject).Trim(CloudFileUtil.PathSeparators);
            Test.Assert(fullPath.Equals(directoryPath.Trim(CloudFileUtil.PathSeparators), StringComparison.OrdinalIgnoreCase), "Prefix of the directory object should match the given parameter. Expected: {0}, Actual: {1}", directoryPath, fullPath);
        }

        public void AssertCloudFileDirectory(object directoryObj, List<string> directoryPathes)
        {
            var directoryObject = ((PSObject)directoryObj).ImmediateBaseObject as CloudFileDirectory;
            Test.Assert(directoryObject != null, "Output object should be an instance of CloudFileDirectory.");
            string fullPath = CloudFileUtil.GetFullPath(directoryObject).Trim(CloudFileUtil.PathSeparators);
            Test.Assert(directoryPathes.Remove(fullPath), "Prefix of the directory object '{0}' should be within the given collection.", fullPath);
        }

        public void AssertCloudFile(object fileObj, string fileName, string path = null)
        {
            var fileObject = ((PSObject)fileObj).ImmediateBaseObject as CloudFile;
            Test.Assert(fileObject != null, "Output object should be an instance of CloudFile.");

            // FIXME: Walk around a issue in XSCL where the CloudFile.Name property sometimes returns
            // full path of the file.
            string fileObjectName = fileObject.Name.Split(CloudFileUtil.PathSeparators, StringSplitOptions.RemoveEmptyEntries).Last();

            Test.Assert(fileObjectName.Equals(fileName, StringComparison.OrdinalIgnoreCase), "Name of the file object should match the given parameter. Expected: {0}, Actual: {1}", fileName, fileObjectName);
            if (path != null)
            {
                string fullPath = CloudFileUtil.GetFullPath(fileObject.Parent).Trim(CloudFileUtil.PathSeparators);
                Test.Assert(fileObject.Parent != null, "Since file is not on root folder, the parent directory should not be null.");
                Test.Assert(fullPath.Equals(path.Trim(CloudFileUtil.PathSeparators), StringComparison.OrdinalIgnoreCase), "Prefix of the directory object should match the path of the file. Expected: {0}, Actual: {1}", path, fullPath);
            }
        }

        public void AssertCloudFile(object fileObj, List<CloudFile> files)
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
