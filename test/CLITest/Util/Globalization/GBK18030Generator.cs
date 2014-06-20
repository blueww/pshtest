namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class GBK18030Generator : DoubleBytesCodePageUnicodeGenerator
    {
        public GBK18030Generator(params char[] excludedCharacters)
            : base(54936, excludedCharacters)
        {
        }

        protected override Tuple<Tuple<byte, byte>, Tuple<byte, byte>>[] GetLeadFollowByteRanges()
        {
            return new Tuple<Tuple<byte, byte>, Tuple<byte, byte>>[]{
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0x81,0xFE),
                    new Tuple<byte,byte>(0x40,0x7E)),
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0x81,0xFE),
                    new Tuple<byte,byte>(0x80,0xFE)),
            };
        }
    }
}
