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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MS.Test.Common.MsTestLib;
using StorageTestLib;

namespace Management.Storage.ScenarioTest.Util
{
    /// <summary>
    /// This class could perform file operations across different platforms(Windows, Linux, Mac)
    /// </summary>
    public class FileUtil : UtilBase
    {
        private static string[] specialNames = { "pageabc", "blockabc ", "pagea b", "block abc", "page中文", 
            "block中 文", "page 中文", "block中文 ", "page.abc", "block.a bc", "page .abc", "block .abc ", string.Empty };
        private static Random random = new Random();

        public static string BinaryFileName { get; set; }

        internal static string GetLinuxPath(string filepath)
        {
            return Regex.Replace(filepath.Replace('\\', '/'), "^[a-zA-Z]:", ".");
        }

        public static void GenerateMediumFile(string filename, int sizeMB, bool AlwaysOperateOnWindows = false)
        {
            if (AgentOSType != OSType.Windows)
            {
                RunNodeJSProcess(string.Format("dd if=/dev/urandom of='{0}' bs=1048576 count={1}", GetLinuxPath(filename), sizeMB), true);
            }

            if (AgentOSType == OSType.Windows || AlwaysOperateOnWindows)
            {
                Helper.GenerateMediumFile(filename, sizeMB);
            }
        }

        public static void GenerateSmallFile(string filename, int sizeKB, bool AlwaysOperateOnWindows = false)
        {
            if (AgentOSType != OSType.Windows)
            {
                RunNodeJSProcess(string.Format("dd if=/dev/urandom of='{0}' bs=1024 count={1}", GetLinuxPath(filename), sizeKB), true);
            }

            if (AgentOSType == OSType.Windows || AlwaysOperateOnWindows)
            {
                Helper.GenerateSmallFile(filename, sizeKB);
            }
        }

        public static void GenerateRandomTestFile(string filename, int sizeKB, bool AlwaysOperateOnWindows = false)
        {
            if (AgentOSType != OSType.Windows)
            {
                RunNodeJSProcess(string.Format("dd if=/dev/urandom of='{0}' bs=1024 count={1}", GetLinuxPath(filename), sizeKB), true);
            }

            if (AgentOSType == OSType.Windows || AlwaysOperateOnWindows)
            {
                if (AgentOSType != OSType.Windows)
                {
                    filename = Path.GetFullPath(filename);
                }
                Helper.GenerateRandomTestFile(filename, sizeKB);
            }
        }

        public static void GenerateTinyFile(string filename, int sizeB, bool AlwaysOperateOnWindows = false)
        {
            if (AgentOSType != OSType.Windows)
            {
                RunNodeJSProcess(string.Format("dd if=/dev/urandom of='{0}' bs=1 count={1}", GetLinuxPath(filename), sizeB), true);
            }

            if (AgentOSType == OSType.Windows || AlwaysOperateOnWindows)
            {
                Helper.GenerateTinyFile(filename, sizeB);
            }
        }

        public static bool CompareTwoFiles(string filename, string filename2)
        {
            bool success = false;
            if (AgentOSType == OSType.Windows)
            {
                success = Helper.CompareTwoFiles(filename, filename2);
            }
            else
            {
                // use linux command "cmp" to compare two files
                RunNodeJSProcess(string.Format("cmp '{0}' '{1}'", GetLinuxPath(filename), GetLinuxPath(filename2)));
                success = string.IsNullOrEmpty(Output);
            }

            return success;
        }

        public static string GetFileContentMD5(string filePath)
        {
            string md5 = string.Empty;
            if (AgentOSType == OSType.Windows)
            {
                md5 = Helper.GetFileContentMD5(filePath);
            }
            else
            {
                if (AgentOSType == OSType.Linux)
                {
                    RunNodeJSProcess(string.Format("md5sum '{0}' | cut -f 1 -d ' '", GetLinuxPath(filePath)));
                }
                else if (AgentOSType == OSType.Mac)
                {
                    RunNodeJSProcess(string.Format("md5 '{0}'", GetLinuxPath(filePath)));
                }

                if (string.IsNullOrEmpty(Output) || Output.EndsWith(": No such file or directory"))
                {
                    throw new Exception(string.Format("file {0} not found, Output: {1}!", filePath, Output));
                }
                Output = Output.Trim('\n');

                string md5Hex = string.Empty;
                if (AgentOSType == OSType.Linux)
                {
                    md5Hex = Output;
                }
                else if (AgentOSType == OSType.Mac)
                {
                    // for mac, md5 is the last field in the output
                    int index = Output.LastIndexOf(' ');
                    if (index > -1)
                    {
                        md5Hex = Output.Substring(index + 1, Output.Length - index - 1);
                    }
                    else
                    {
                        throw new Exception(string.Format("file {0} not found, Output: {1}!", filePath, Output));
                    }
                }
                md5 = Convert.ToBase64String(Utility.StringToByteArray(md5Hex));
            }
            return md5;
        }

        public static string GetSpecialFileName()
        {
            int nameCount = specialNames.Count() - 1;
            int specialIndex = random.Next(0, nameCount);
            string prefix = specialNames[specialIndex];
            return Utility.GenNameString(prefix);
        }

        /// <summary>
        /// generate temp files using StorageTestLib helper
        /// </summary>
        /// <param name="rootPath">the root dir path</param>
        /// <param name="relativePath">the relative dir path</param>
        /// <param name="depth">sub dir depth</param>
        /// <param name="files">a list of created files</param>
        private static void GenerateTempFiles(string rootPath, string relativePath, int depth, List<string> files)
        {
            //minEntityCount should not be 0 after using parallel uploading and downloading. refer to bug#685185
            int minEntityCount = 1;
            int maxEntityCount = 5;
            int maxFileSize = 10; //KB

            int fileCount = random.Next(minEntityCount, maxEntityCount);
            bool AlwaysOperateOnWindows = AgentOSType != OSType.Windows;

            for (int i = 0; i < fileCount; i++)
            {
                int fileSize = random.Next(1, maxFileSize);
                string fileName = Path.Combine(relativePath, GetSpecialFileName());
                string filePath = Path.Combine(rootPath, fileName);
                files.Add(fileName);
                GenerateRandomTestFile(filePath, fileSize, AlwaysOperateOnWindows);
                Test.Info("Create a {0}kb test file '{1}'", fileSize, filePath);
            }

            int dirCount = random.Next(minEntityCount, maxEntityCount);
            for (int i = 0; i < dirCount; i++)
            {
                string prefix = GetSpecialFileName();
                string dirName = Path.Combine(relativePath, Utility.GenNameString(string.Format("dir{0}", prefix)));
                //TODO dir name should contain space
                dirName = dirName.Replace(" ", "");
                string absolutePath = Path.Combine(rootPath, dirName);
                CreateDirIfNotExits(absolutePath, AlwaysOperateOnWindows);
                Test.Info("Create directory '{0}'", absolutePath);

                if (depth >= 1)
                {
                    GenerateTempFiles(rootPath, dirName, depth - 1, files);
                }
            }
        }

        /// <summary>
        /// Create directory if not exists
        /// </summary>
        /// <param name="dirPath"></param>
        public static void CreateDirIfNotExits(string dirPath, bool AlwaysOperateOnWindows = false)
        {
            if (AgentOSType != OSType.Windows)
            {
                RunNodeJSProcess(string.Format("mkdir -p '{0}'", GetLinuxPath(dirPath)), true);
            }

            if (AgentOSType == OSType.Windows || AlwaysOperateOnWindows)
            {
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
            }
        }

        /// <summary>
        /// clean the specified dir
        /// </summary>
        /// <param name="directory">the destination dir</param>
        public static void CleanDirectory(string directory, bool AlwaysOperateOnWindows = false)
        {
            Test.Info("Start to clean directory {0} is done.", directory);
            try
            {
                if (AgentOSType != OSType.Windows)
                {
                    // remove all files & folders under this directory
                    string path = GetLinuxPath(directory);
                    if (path.Last() != '/')
                    {
                        path += '/';
                    }
                    path += "*";

                    RunNodeJSProcess(string.Format("rm -rf '{0}'", path));
                }

                if (AgentOSType == OSType.Windows || AlwaysOperateOnWindows)
                {
                    DirectoryInfo dir = new DirectoryInfo(directory);

                    foreach (FileInfo file in dir.GetFiles())
                    {
                        file.Delete();
                    }

                    foreach (DirectoryInfo subdir in dir.GetDirectories())
                    {
                        CleanDirectory(subdir.FullName);
                        subdir.Delete();
                    }
                }

                Test.Info("Cleaning directory {0} is done.", directory);
            }
            catch (Exception e)
            {
                Test.Warn("Exception when cleaning directory {0}. Message: {1}", directory, e);
            }
        }

        public static void CreateNewFolder(string foldername, bool AlwaysOperateOnWindows = false)
        {
            if (AgentOSType != OSType.Windows)
            {
                RunNodeJSProcess(string.Format("mkdir '{0}'", GetLinuxPath(foldername)), true);
            }

            if (AgentOSType == OSType.Windows || AlwaysOperateOnWindows)
            {
                Helper.CreateNewFolder(foldername);
            }
        }

        public static bool FileExists(string path)
        {
            if (AgentOSType == OSType.Windows)
            {
                return File.Exists(path);
            }
            else
            {
                RunNodeJSProcess(string.Format("ls '{0}'", GetLinuxPath(path)));
                return !Output.EndsWith(": No such file or directory");
            }
        }

        public static long GetFileSize(string filePath)
        {
            if (AgentOSType == OSType.Windows)
            {
                FileInfo fi = new FileInfo(filePath);
                return fi.Length;
            }
            else
            {
                if (AgentOSType == OSType.Mac)
                {
                    RunNodeJSProcess(string.Format("stat '{0}' | cut -f 8 -d ' '", GetLinuxPath(filePath)));
                }
                else
                {
                    RunNodeJSProcess(string.Format("stat -c %s '{0}'", GetLinuxPath(filePath)));
                }
                if (string.IsNullOrEmpty(Output) || Output.EndsWith(": No such file or directory"))
                {
                    throw new Exception(string.Format("file {0} not found, Output: {1}!", filePath, Output));
                }
                return Convert.ToInt64(Output);
            }
        }

        /// <summary>
        /// create temp dirs and files
        /// </summary>
        /// <param name="rootPath">the destination dir</param>
        /// <param name="depth">sub dir depth</param>
        /// <returns>a list of created files</returns>
        public static List<string> GenerateTempFiles(string rootPath, int depth)
        {
            List<string> files = new List<string>();
            files.Clear();
            GenerateTempFiles(rootPath, string.Empty, depth, files);
            files.Sort();
            return files;
        }

        /// <summary>
        /// Remove the specified file
        /// </summary>
        /// <param name="filePath">File Path</param>
        public static void RemoveFile(string filePath, bool AlwaysOperateOnWindows = false)
        {
            if (AgentOSType != OSType.Windows)
            {
                RunNodeJSProcess(string.Format("rm -rf '{0}'", GetLinuxPath(filePath)));
            }

            if (AgentOSType == OSType.Windows || AlwaysOperateOnWindows)
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Generate a temp local file for testing
        /// </summary>
        /// <returns>The temp local file path</returns>
        public static string GenerateOneTempTestFile(int minFileSize = 1, int maxFileSize = 10 * 1024)
        {
            string fileName = GetSpecialFileName();
            string uploadDirRoot = Test.Data.Get("UploadDir");
            string filePath = Path.Combine(uploadDirRoot, fileName);
            int fileSize = random.Next(minFileSize, maxFileSize);
            GenerateRandomTestFile(filePath, fileSize);
            return filePath;
        }

        /// <summary>
        /// Get OS config from testdata.xml
        /// </summary>
        public static void GetOSConfig(TestConfig data)
        {
            Utility.GetOSConfig(data, ref AgentOSType, AgentConfig);
        }

        public static void PrepareData(string folder, int fileNum, int sizeKB)
        {
            Helper.CreateNewFolder(folder);

            if (sizeKB < 1024)
            {
                for (int i = 0; i < fileNum; i++)
                {
                    string fileName = string.Format("{0}\\testfile_{1}K_{2}", folder, sizeKB, i);
                    if (!File.Exists(fileName))
                    {
                        Helper.GenerateSmallFile(fileName, sizeKB);
                    }
                }
            }
            else
            {
                for (int i = 0; i < fileNum; i++)
                {
                    string fileName = string.Format("{0}\\testfile_{1}M_{2}", folder, sizeKB / 1024, i);
                    if (!File.Exists(fileName))
                    {
                        Helper.GenerateMediumFile(fileName, sizeKB / 1024);
                    }
                }
            }
        }
    }
}
