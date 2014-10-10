namespace Management.Storage.ScenarioTest.Util
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using MS.Test.Common.MsTestLib;

    public class UtilBase
    {
        public static int MaxWaitingTime = 600000;  // in miliseconds
        public static string WorkingDirectory = ".";
        public static string Output { get; set; }
        public static string Error { get; set; }

        public static OSType AgentOSType = AgentFactory.GetOSType();
        public static OSConfig AgentConfig = new OSConfig();

        internal static void SetProcessInfo(Process p, string command)
        {
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            p.StartInfo.WorkingDirectory = WorkingDirectory;

            p.StartInfo.FileName = AgentConfig.PLinkPath;
            p.StartInfo.Arguments = string.Format("-l {0} -i \"{1}\" {2} -P {3} ",
                AgentConfig.UserName, AgentConfig.PrivateKeyPath, AgentConfig.HostName, AgentConfig.Port);

            // replace all " with ' in command for linux
            command = command.Replace('"', '\'');
            p.StartInfo.Arguments += command;

            Test.Info("plink command: {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);
        }

        internal static void RunNodeJSProcess(string argument, bool ignoreError = false)
        {
            Process p = new Process();
            SetProcessInfo(p, argument);
            p.Start();

            Output = p.StandardOutput.ReadToEnd();
            Error = p.StandardError.ReadToEnd();

            p.WaitForExit(MaxWaitingTime);
            if (!p.HasExited)
            {
                p.Kill();
                throw new Exception(string.Format("NodeJS command timeout: cost time > {0}s !", MaxWaitingTime / 1000));
            }

            if (!string.IsNullOrEmpty(Error) && !ignoreError)
            {
                Test.Error("Error msg: " + Error);
            }
        }

        public static string AddAccountParameters(string argument, OSConfig AgentConfig)
        {
            string ret = argument;
            if (!string.IsNullOrEmpty(AgentConfig.SAS) && !string.IsNullOrEmpty(AgentConfig.AccountName))
            {
                ret += string.Format(" -a \"{0}\" --sas \"{1}\"", AgentConfig.AccountName, AgentConfig.SAS); 
            }
            else if (!string.IsNullOrEmpty(AgentConfig.ConnectionString))
            {
                ret += string.Format(" -c \"{0}\"", AgentConfig.ConnectionString);
            }
            else if (!string.IsNullOrEmpty(AgentConfig.AccountName))
            {
                ret += string.Format(" -a \"{0}\" -k \"{1}\"", AgentConfig.AccountName, AgentConfig.AccountKey);
            }
            else
            {
                ret += string.Format(" -c \"{0}\"", Test.Data.Get("StorageConnectionString"));
            }

            // if no account param set, then we would use the env var
            return ret;
        }
    }

    public class OSConfig
    {
        public string PLinkPath;
        public string UserName;
        public string HostName;
        public string Port = "22";
        public string PrivateKeyPath;

        public string ConnectionStr;
        public string Name;
        public string Key;
        public string Sas;

        public OSConfig()
        {
            UseEnvVar = false;
        }

        public string ConnectionString
        {
            get { return ConnectionStr; }
            set
            {
                ConnectionStr = value;
                Name = string.Empty;
                Key = string.Empty;
            }
        }

        public string AccountName
        {
            get { return Name; }
            set
            {
                Name = value;
                ConnectionStr = string.Empty;
            }
        }

        public string AccountKey
        {
            get { return Key; }
            set
            {
                Key = value;
                ConnectionStr = string.Empty;
            }
        }

        public string SAS
        {
            get { return Sas; }
            set
            {
                Sas = value;
                ConnectionStr = string.Empty;
            }
        }

        public bool UseEnvVar { get; set; }
    }
}
