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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.File;
    using MS.Test.Common.MsTestLib;
    using Newtonsoft.Json.Linq;

    internal class NodeJSExecutionResult : IExecutionResult
    {
        private Collection<Dictionary<string, object>> result;

        public NodeJSExecutionResult(Collection<Dictionary<string, object>> result)
        {
            this.result = result;
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

            foreach (var resultObject in this.result)
            {
                assertAction(resultObject);
            }
        }

        public void AssertFileListItems(IEnumerable<CloudFile> files, IEnumerable<CloudFileDirectory> directories)
        {
            Test.Assert(this.result.Count > 0, "No output objects.");
            var listResultObject = this.result[0];
            var fileList = new List<string>(files.Select(x => x.Name));
            var directoryList = new List<string>(directories.Select(x => x.Name));

            object filesResult, directoriesResult;
            if (!listResultObject.TryGetValue("files", out filesResult))
            {
                throw new AssertFailedException("List result should contain \"files\" element.");
            }

            if (!listResultObject.TryGetValue("directories", out directoriesResult))
            {
                throw new AssertFailedException("List result should contain \"directories\" element.");
            }

            var filesListResult = filesResult as JArray;
            var directoriesListResult = directoriesResult as JArray;
            if (filesListResult == null)
            {
                throw new AssertFailedException("files element should be an enumerable.");
            }

            if (directoriesListResult == null)
            {
                throw new AssertFailedException("directories element should be an enumerable.");
            }

            AssertItemInEnumerable(filesListResult.Select(x => x["name"].ToString()), fileList, "File {0} was not found in the expecting list.");
            AssertItemInEnumerable(directoriesListResult.Select(x => x["name"].ToString()), directoryList, "Directory {0} was not found in the expecting list.");

            Test.Assert(fileList.Count == 0, "{0} leftover items in file list.", fileList.Count);
            Test.Assert(directoryList.Count == 0, "{0} leftover items in directory list.", directoryList.Count);
        }

        public void AssertCloudFileContainer(object containerObj, string fileShareName)
        {
            var containerObject = containerObj as Dictionary<string, object>;
            Test.Assert(containerObject != null, "Output object should be an instance of Dictionary<string, object> class.");
            Test.Assert(containerObject["name"].ToString().Equals(fileShareName, StringComparison.OrdinalIgnoreCase), "Name of the container object should match the given parameter. Expected: {0}, Actual: {1}", fileShareName, containerObject["name"]);
        }

        public void AssertCloudFileContainer(object containerObj, List<string> fileShareNames, bool failIfNotInGivenList = true)
        {
            var containerObject = containerObj as Dictionary<string, object>;
            Test.Assert(containerObject != null, "Output object should be an instance of Dictionary<string, object> class.");
            bool withInGivenList = fileShareNames.Remove(containerObject["name"].ToString());
            if (failIfNotInGivenList)
            {
                Test.Assert(withInGivenList, "Name of the container '{0}' should be within the given collection.", containerObject["name"]);
            }
        }

        public void AssertCloudFileDirectory(object directoryObj, string directoryPath)
        {
            var directoryObject = directoryObj as Dictionary<string, object>;
            Test.Assert(directoryObject != null, "Output object should be an instance of Dictionary<string, object> class.");
            string baseName = (string)directoryObject["name"];
            Test.Assert(baseName.Equals(GetBaseName(directoryPath), StringComparison.OrdinalIgnoreCase), "Prefix of the directory object should match the given parameter. Expected: {0}, Actual: {1}", directoryPath, baseName);
        }

        public void AssertCloudFileDirectory(object directoryObj, List<string> directoryPathes)
        {
            var directoryObject = (Dictionary<string, object>)directoryObj as Dictionary<string, object>;
            Test.Assert(directoryObject != null, "Output object should be an instance of Dictionary<string, object> class.");
            string baseName = (string)directoryObject["name"];

            Test.Assert(directoryPathes.Remove(baseName), "Prefix of the directory object '{0}' should be within the given collection.", baseName);
        }

        public void AssertCloudFile(object fileObj, string fileName, string path = null)
        {  
            string fileObjectName = parseFileNameFromOutputObject(fileObj);

            Test.Assert(fileObjectName.Equals(fileName, StringComparison.OrdinalIgnoreCase), "Name of the file object should match the given parameter. Expected: {0}, Actual: {1}", fileName, fileObjectName);
        }

        public void AssertCloudFile(object fileObj, List<CloudFile> files)
        {
            string fileObjectName = parseFileNameFromOutputObject(fileObj);

            CloudFile matchingFile = files.FirstOrDefault(file => file.Name.Equals(fileObjectName, StringComparison.OrdinalIgnoreCase));
            Test.Assert(matchingFile != null, "Output CloudFile object {0} was not found in the expecting list.", fileObjectName);
            files.Remove(matchingFile);
        }

        private string parseFileNameFromOutputObject(object fileObj)
        {
            var fileObject = fileObj as Dictionary<string, object>;
            Test.Assert(fileObject != null, "Output object should be an instance of Dictionary<string, object> class.");

            string directory = fileObject.ContainsKey("directory") ? fileObject["directory"] as string : string.Empty;
            if (!string.IsNullOrWhiteSpace(directory))
            {
                directory += "/";
            }

            string name = fileObject.ContainsKey("name") ? fileObject["name"] as string : string.Empty;

            return directory.Replace('\\', '/') + name;
        }

        /// <summary>
        /// Gets the base name from a full path
        /// </summary>
        /// <param name="fullPath">Indicating the full path.</param>
        /// <returns>Returns the base name.</returns>
        private static string GetBaseName(string fullPath)
        {
            return fullPath.Split(CloudFileUtil.PathSeparators).LastOrDefault();
        }

        private static void AssertItemInEnumerable<T>(IEnumerable<T> assertItems, IList<T> expectingList, string assertMessage)
        {
            foreach (var item in assertItems)
            {
                Test.Assert(expectingList.Remove(item), assertMessage, item);
            }
        }
    }
}
