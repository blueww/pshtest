namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public sealed class Windows1254Generator : SingleByteCodePageUnicodeGenerator
    {
        public Windows1254Generator(params char[] excludedCharacters)
            : base(1254, excludedCharacters)
        {
        }

        protected override Tuple<byte, byte>[] GetRanges()
        {
            return new Tuple<byte, byte>[]{
                new Tuple<byte,byte>(0x82,0x8C),
                new Tuple<byte,byte>(0x91,0x9C),
                new Tuple<byte,byte>(0x9F,0xFF),
            };
        }
    }
}
