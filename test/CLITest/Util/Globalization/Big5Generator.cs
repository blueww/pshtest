namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed class Big5Generator : DoubleBytesCodePageUnicodeGenerator
    {
        public Big5Generator(params char[] excludedCharacters)
            : base(950, excludedCharacters)
        {
        }

        protected override Tuple<Tuple<byte, byte>, Tuple<byte, byte>>[] GetLeadFollowByteRanges()
        {
            return new Tuple<Tuple<byte, byte>, Tuple<byte, byte>>[]{
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0xA4,0xC6),
                    new Tuple<byte,byte>(0x40,0x7E)
                    ),
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0xA4,0xC6),
                    new Tuple<byte,byte>(0xA1,0xFE)
                    ),
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0xC9,0xFE),
                    new Tuple<byte,byte>(0x40,0x7E)
                    ),
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0xC9,0xFE),
                    new Tuple<byte,byte>(0xA1,0xFE)
                    ),
            };
        }

        protected override Tuple<ushort, ushort>[] GetOtherRanges()
        {
            return new Tuple<ushort, ushort>[]{
                new Tuple<ushort,ushort>(0xA259,0xA261),
            };
        }
    }
}
