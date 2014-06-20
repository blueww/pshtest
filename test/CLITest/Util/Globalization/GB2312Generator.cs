namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed class GB2312Generator : DoubleBytesCodePageUnicodeGenerator
    {
        public GB2312Generator(params char[] excludedCharacters)
            : base(936, excludedCharacters)
        {
        }

        protected override Tuple<Tuple<byte, byte>, Tuple<byte, byte>>[] GetLeadFollowByteRanges()
        {
            return new Tuple<Tuple<byte, byte>, Tuple<byte, byte>>[]{
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0xA1,0xF7),
                    new Tuple<byte,byte>(0xA1,0xFE)),
            };
        }
    }
}
