namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public sealed class Windows1256Generator : SingleByteCodePageUnicodeGenerator
    {
        public Windows1256Generator(params char[] excludedCharacters)
            : base(1256, excludedCharacters)
        {
        }

        protected override Tuple<byte, byte>[] GetRanges()
        {
            return new Tuple<byte, byte>[]{
                new Tuple<byte,byte>(0x81,0x89),
                new Tuple<byte,byte>(0x8B,0x8E),
                new Tuple<byte,byte>(0x90,0x97),
                new Tuple<byte,byte>(0x9B,0x9E),
                new Tuple<byte,byte>(0xA0,0xBF),
                new Tuple<byte,byte>(0xC1,0xFE),
            };
        }
    }
}
