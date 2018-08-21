namespace MS.Test.Common.MsTestLib
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public static class DumpHelper
    {
        private const int CreateDumpTimeOut = 600 * 1000;

        public static string DumpFolder
        {
            get
            {
                try
                {
                    return Test.Data.Get("DumpFolder");
                }
                catch
                {
                    return "Dumps";
                }
            }
        }

        public static string DumpToolPath
        {
            get
            {
                try
                {
                    return Test.Data.Get("DumpToolPath");
                }
                catch
                {
                    return "procdump.exe";
                }
            }
        }
        

        public static void CreateDump(int processId)
        {
            Test.Info("Start to create dump for process {0}.", processId);
            string dumpFilePath = Path.Combine(DumpHelper.DumpFolder, GetDumpFileName(processId));
            string args = string.Format("-ma {0} \"{1}\" -accepteula", processId, dumpFilePath);

            if (!Directory.Exists(DumpHelper.DumpFolder))
            {
                Directory.CreateDirectory(DumpHelper.DumpFolder);
            }

            RunDumpTool(args);

            if (File.Exists(dumpFilePath))
            {
                Test.Info("Dump is created: {0}", dumpFilePath);
            }
            else
            {
                Test.Info("Fail to create dump for process {0}.", processId);
            }
        }

        private static string GetDumpFileName(int processId)
        {
            return string.Format("{0}-{1}.dmp", processId, Guid.NewGuid().ToString());
        }

        private static void RunDumpTool(string args)
        {
            if (!File.Exists(DumpToolPath))
            {
                Test.Info("Dump tool doesn't exist: {0}", DumpToolPath);
                return;
            }
            
            Test.Info("Running: {0} {1}", DumpToolPath, args);
            ProcessStartInfo prossStartInfo = new ProcessStartInfo(DumpToolPath, args);
            prossStartInfo.CreateNoWindow = true;
            prossStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            prossStartInfo.UseShellExecute = false;
            prossStartInfo.RedirectStandardError = false;
            prossStartInfo.RedirectStandardOutput = false;
            prossStartInfo.RedirectStandardInput = false;

            Process p = Process.Start(prossStartInfo);
            p.WaitForExit(CreateDumpTimeOut);

            if (!p.HasExited)
            {
                Test.Info("Kill dump tool process.");
                p.Kill();
            }
        }
    }
}
