namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public sealed class Windows1253Generator : SingleByteCodePageUnicodeGenerator
    {
        public Windows1253Generator(params char[] excludedCharacters)
            : base(1253, excludedCharacters)
        {
        }

        protected override Tuple<byte, byte>[] GetRanges()
        {
            return new Tuple<byte, byte>[]{
                new Tuple<byte,byte>(0xA0,0xFE),
            };
        }
    }
}
