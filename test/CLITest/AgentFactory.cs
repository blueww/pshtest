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

        public static Agent CreateAgent(IDictionary properties)
        {
            Language lang = GetLanguage(properties);

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

    public enum Language { PowerShell, NodeJS };
}
