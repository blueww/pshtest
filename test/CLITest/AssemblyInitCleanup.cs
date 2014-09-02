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

namespace DataMovementTest
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MS.Test.Common.MsTestLib;

    [TestClass]
    public class AssemblyInitCleanup
    {
        [AssemblyInitialize]
        public static void TestInit(TestContext testContext)
        {
            // init loggers and load test config data
            Test.Init();

            // set the assertfail delegate to report failure in VS
            Test.AssertFail = new AssertFailDelegate(Assert.Fail);

            CleanupTempFolder();
        }

        [AssemblyCleanup]
        public static void TestCleanup()
        {
            CleanupTempFolder();

            //close loggers
            Test.Close();
        }

        private static void CleanupTempFolder()
        {
            string tempDir = Test.Data.Get("TempDir");
            if (!string.IsNullOrEmpty(tempDir))
            {
                try
                {
                    Test.Verbose("Cleanup temp folder...");
                    var tempDirInfo = new DirectoryInfo(tempDir);
                    foreach (var file in tempDirInfo.EnumerateFiles())
                    {
                        try
                        {
                            Test.Verbose("Deleting file {0}...", file);
                            file.Delete();
                        }
                        catch (Exception e)
                        {
                            Test.Warn("Failed to delete file {0}: {1}", file, e);
                        }
                    }

                    foreach (var dir in tempDirInfo.EnumerateDirectories())
                    {
                        try
                        {
                            Test.Verbose("Deleting file {0}...", dir);
                            dir.Delete(true);
                        }
                        catch (Exception e)
                        {
                            Test.Warn("Failed to delete file {0}: {1}", dir, e);
                        }
                    }
                }
                catch (Exception e)
                {
                    Test.Error("Failed to cleanup temp folder {0}: {1}", tempDir, e);
                }
            }
        }
    }
}
