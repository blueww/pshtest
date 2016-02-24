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
    /// <summary>
    /// powershell test tags
    /// </summary>
    public struct PsTag
    {
        public const string Perf = "perf";
        public const string Scale = "scale";
        public const string GB18030 = "GB18030";
        public const string FilePerf = "fileperf";

        public const string Container = "container";
        public const string GetContainer = "getcontainer";
        public const string NewContainer = "newcontainer";
        public const string RemoveContainer = "removecontainer";
        public const string SetContainerAcl = "setcontaineracl";
        public const string NewContainerSas = "newcontainersas";
        public const string NewBlobSas = "newblobsas";
        public const string NewQueueSas = "newqueuesas";
        public const string NewTableSas = "newtablesas";
        public const string NewAccountSas = "newaccountsas";
        public const string SASInterop = "sasinterop";

        public const string StoredAccessPolicy = "storedaccesspolicy";

        public const string Blob = "blob";
        public const string GetBlob = "getblob";
        public const string RemoveBlob = "removeblob";

        public const string GetBlobContent = "getblobcontent";
        public const string SetBlobContent = "setblobcontent";
        
        public const string StartCopyBlob = "startcopyblob";
        public const string GetBlobCopyState = "getblobcopystate";
        public const string StopCopyBlob = "stopcopyblob";

        public const string NewShare = "newshare";
        public const string RemoveShare = "removeshare";
        public const string GetShare = "getshare";

        public const string NewDirectory = "newdirectory";
        public const string RemoveDirectory = "removedirectory";
        public const string GetDirectory = "getdirectory";

        public const string Queue = "queue";
        public const string GetQueue = "getqueue";
        public const string NewQueue = "newqueue";
        public const string RemoveQueue = "removequeue";

        public const string Table = "table";
        public const string GetTable = "gettable";
        public const string NewTable = "newtable";
        public const string RemoveTable = "removetable";

        public const string StorageContext = "storagecontext";
        public const string ServiceLogging = "servicelogging";
        public const string ServiceMetrics = "servicemetrics";
        public const string ServiceCORS = "servicecors";

        /// <summary>
        /// test tag for run the fast bvt cases for different environments
        /// </summary>
        public const string FastEnv = "fastenv";

        public const string File = "file";
        public const string FileBVT = "filebvt";
    }

    public struct CLITag
    {
        public const string NodeJSBVT = "nodejsbvt";
        public const string NodeJSFT= "nodejsft";
        public const string NodeJSPerf= "nodejsperf";
        public const string NodeJSScale= "nodejsscale";

        public const string NodeJSServiceAccount = "nodejsServiceAccount";
        public const string NodeJSResourceAccount = "nodejsResourceAccount";

        public const string Blob = "nodejsblob";
        public const string GetBlob = "nodejsgetblob";
        public const string RemoveBlob = "nodejsremoveblob";

        public const string StartCopyBlob = "nodestartcopyblob";
        public const string GetBlobCopyState = "nodegetblobcopystate";
        public const string StopCopyBlob = "nodestopcopyblob";

        public const string Table = "nodejstable";
        public const string GetTable = "nodejsgettable";
        public const string NewTable = "nodejsnewtable";
        public const string RemoveTable = "nodejsremovetable";

        public const string Queue = "nodejsqueue";
        public const string GetQueue = "nodejsgetqueue";
        public const string NewQueue = "nodejsnewqueue";
        public const string RemoveQueue = "nodejsremovequeue";

        public const string NewContainerSas = "nodejsnewcontainersas";
        public const string NewBlobSas = "nodejsnewblobsas";
        public const string NewQueueSas = "nodejsnewqueuesas";
        public const string NewTableSas = "nodejsnewtablesas";
        public const string NewShareSas = "nodejsnewsharesas";
        public const string NewFileSas = "nodejsnewfilesas";
        public const string SASInterop = "nodejssasinterop";

        public const string StoredAccessPolicy = "nodejsstoredaccesspolicy";

        public const string File = "nodejsfile";
        public const string StartCopyFile = "nodestartcopyfile";
        public const string GetFileCopyState = "nodegetfilecopystate";
        public const string StopCopyFile = "nodestopcopyfile";

        public const string ServiceLogging = "nodejsservicelogging";
        public const string ServiceMetrics = "nodejsservicemetrics";
        public const string ServiceCORS = "nodejsservicecors";
    }
}
