using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest
{
    public class AgentFactory
    {
        public static Language GetLanguage(IDictionary properties)
        {
            Dictionary<object, object> props = properties as Dictionary<object, object>;
            if (props.ContainsKey("lang") && !String.IsNullOrEmpty(props["lang"] as string))
            {
                string v = props["lang"] as string;
                if (String.Compare(Language.PowerShell.ToString(), v, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return Language.PowerShell;
                }
                else if (String.Compare(Language.NodeJS.ToString(), v, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return Language.NodeJS;
                }
                else
                {
                    throw new Exception(String.Format("Unsupported lang value: {0}", props["lang"]));
                }
            }
            else
            {
                throw new Exception(String.Format("Please specify language parameter value!"));
            }
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
