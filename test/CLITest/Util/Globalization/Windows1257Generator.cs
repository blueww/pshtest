namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public sealed class Windows1257Generator : SingleByteCodePageUnicodeGenerator
    {
        public Windows1257Generator(params char[] excludedCharacters)
            : base(1257, excludedCharacters)
        {
        }

        protected override Tuple<byte, byte>[] GetRanges()
        {
            return new Tuple<byte, byte>[]{
                new Tuple<byte,byte>(0xA0,0xFF),
            };
        }
    }
}
