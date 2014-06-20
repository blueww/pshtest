namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class UnicodeGenerator
    {
        private Encoding encoding;

        private Random random = new Random();

        public UnicodeGenerator(int codePage, params char[] excludedCharacters)
        {
            this.encoding = Encoding.GetEncoding(codePage, EncoderFallback.ReplacementFallback, new DecoderReplacementFallback(string.Empty));
            this.ExcludedCharacters = excludedCharacters;
        }

        public char[] ExcludedCharacters
        {
            get;
            set;
        }

        protected Random Random
        {
            get { return this.random; }
        }

        public string GenerateRandomString(int length)
        {
            StringBuilder result = new StringBuilder(length);
            while (result.Length < length)
            {
                byte[] bytes = GenerateRandomBytesWithinValidRange(length - result.Length);
                string str = this.encoding.GetString(bytes);
                foreach (var excludedChar in this.ExcludedCharacters)
                {
                    str.Replace(excludedChar.ToString(), string.Empty);
                }

                for (int i = 0; i < str.Length; i++)
                {
                    int ch = (int)str[i];
                    if (ch < 0xE000 && ch >= 0x20)
                    {
                        result.Append(str[i]);
                    }
                }
            }

            return result.ToString();
        }

        protected abstract byte[] GenerateRandomBytesWithinValidRange(int length);
    }
}
