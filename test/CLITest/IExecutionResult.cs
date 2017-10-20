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
    using Microsoft.WindowsAzure.Storage.File;

    /// <summary>
    /// Provides an interface for execution result of CLI.
    /// </summary>
    public interface IExecutionResult
    {
        void AssertNoResult();

        void AssertObjectCollection(Action<object> assertAction, int assertNumber = 1);

        void AssertFileListItems(IEnumerable<CloudFile> files, IEnumerable<CloudFileDirectory> directories);

        void AssertCloudFileContainer(object containerObj, string fileShareName, int expectedUsage = 0, DateTimeOffset? snapshotTime = null);

        void AssertCloudFileContainersExist(string fileShareName, List<DateTimeOffset> snapshotTimes = null);

        void AssertCloudFileContainer(object containerObj, List<string> fileShareNames, bool failIfNotInGivenList = true);

        void AssertCloudFileDirectory(object directoryObj, string directoryPath);

        void AssertCloudFileDirectory(object directoryObj, List<string> directoryPathes);

        void AssertCloudFile(object fileObj, string fileName, string path = null);

        void AssertCloudFile(object fileObj, List<CloudFile> files);
    }
}
