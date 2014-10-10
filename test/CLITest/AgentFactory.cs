namespace Management.Storage.ScenarioTest
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using MS.Test.Common.MsTestLib;

    public class AgentFactory
    {
        private static Language? agentLanguage;
        private static OSType? agentOS;

        public static Language GetLanguage(IDictionary properties = null)
        {
            if (agentLanguage.HasValue)
            {
                return agentLanguage.Value;
            }

            if (properties == null)
            {
                throw new InvalidOperationException("Language has not been cached.");
            }

            if (!properties.Contains("lang"))
            {
                properties["lang"] = Test.Data.Get("language");
            }

            if (properties.Contains("lang") && !String.IsNullOrEmpty(properties["lang"] as string))
            {
                string v = properties["lang"] as string;
                if (String.Compare(Language.PowerShell.ToString(), v, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    agentLanguage = Language.PowerShell;
                }
                else if (String.Compare(Language.NodeJS.ToString(), v, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    agentLanguage = Language.NodeJS;
                }
                else
                {
                    throw new Exception(String.Format("Unsupported lang value: {0}", properties["lang"]));
                }
            }
            else
            {
                throw new Exception(String.Format("Please specify language parameter value!"));
            }

            return agentLanguage.Value;
        }

        public static OSType GetOSType()
        {
            if (agentOS.HasValue)
            {
                return agentOS.Value;
            }

            string os =  Test.Data.Get("AgentOS");

            if (!String.IsNullOrEmpty(os))
            {
                if (String.Compare(OSType.Windows.ToString(), os, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    agentOS = OSType.Windows;
                }
                else if (String.Compare(OSType.Linux.ToString(), os, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    agentOS = OSType.Linux;
                }
                else if (String.Compare(OSType.Mac.ToString(), os, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    agentOS = OSType.Mac;
                }
                else
                {
                    throw new Exception(String.Format("Unsupported AgentOS value: {0}", os));
                }
            }
            else
            {
                throw new Exception(String.Format("Please specify AgentOS parameter value!"));
            }

            return agentOS.Value;
        }

        public static Agent CreateAgent(IDictionary properties)
        {
            Language lang = GetLanguage(properties);
            GetOSType();

            switch (lang)
            {
                case Language.PowerShell:
                    return new PowerShellAgent();
                case Language.NodeJS:
                    return new NodeJSAgent();
                default:
                    throw new Exception(String.Format("Please specify language parameter value!"));
            }
        }
    }

    public enum Language 
    { 
        PowerShell, NodeJS 
    };

    public enum OSType
    {
        Windows, Linux, Mac
    }

}
