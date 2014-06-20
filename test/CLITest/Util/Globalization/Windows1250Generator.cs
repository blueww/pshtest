namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public sealed class Windows1250Generator : SingleByteCodePageUnicodeGenerator
    {
        public Windows1250Generator(params char[] excludedCharacters)
            : base(1250, excludedCharacters)
        {
        }

        protected override Tuple<byte, byte>[] GetRanges()
        {
            return new Tuple<byte, byte>[]{
                new Tuple<byte,byte>(0x89,0x8F),
                new Tuple<byte,byte>(0x91,0x97),
                new Tuple<byte,byte>(0x99,0xFF),
            };
        }
    }
}
