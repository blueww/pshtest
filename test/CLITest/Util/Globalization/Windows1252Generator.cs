namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public sealed class Windows1252Generator : SingleByteCodePageUnicodeGenerator
    {
        public Windows1252Generator(params char[] excludedCharacters)
            : base(1252, excludedCharacters)
        {
        }

        protected override Tuple<byte, byte>[] GetRanges()
        {
            return new Tuple<byte, byte>[]{
                new Tuple<byte,byte>(0x9F,0xFF),
            };
        }
    }
}
