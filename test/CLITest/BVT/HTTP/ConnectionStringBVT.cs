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

using StorageTestLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using MS.Test.Common.MsTestLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Management.Storage.ScenarioTest.Common;

namespace Management.Storage.ScenarioTest.BVT.HTTP
{
    /// <summary>
    /// bvt tests using connection string
    /// </summary>
    [TestClass]
    public class ConnectionStringBVT : Management.Storage.ScenarioTest.BVT.HTTPS.ConnectionStringBVT
    {
        [ClassInitialize()]
        public static void ConnectionStringHTTPBVTClassInitialize(TestContext testContext)
        {
            //first set the storage account
            //second init common bvt
            //third set storage context in powershell
            useHttps = false;
            SetUpStorageAccount = CloudStorageAccount.Parse(Test.Data.Get("StorageConnectionString"));
            CLICommonBVT.CLICommonBVTInitialize(testContext);
            PowerShellAgent.SetStorageContext(Test.Data.Get("StorageConnectionString"));
        }

        [ClassCleanup()]
        public static void ConnectionStringHTTPBVTCleanup()
        {
            CLICommonBVT.CLICommonBVTCleanup();
        }
    }
}
