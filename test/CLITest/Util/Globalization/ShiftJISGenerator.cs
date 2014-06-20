namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed class ShiftJISGenerator : DoubleBytesCodePageUnicodeGenerator
    {
        public ShiftJISGenerator(params char[] excludedCharacters)
            : base(932, excludedCharacters)
        {
        }

        protected override Tuple<Tuple<byte, byte>, Tuple<byte, byte>>[] GetLeadFollowByteRanges()
        {
            return new Tuple<Tuple<byte, byte>, Tuple<byte, byte>>[]{
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0x81,0x9F),
                    new Tuple<byte,byte>(0x40,0x7E)
                    ),
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0x81,0x9F),
                    new Tuple<byte,byte>(0x80,0xFC)
                    ),
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0xE0,0xEF),
                    new Tuple<byte,byte>(0x40,0x7E)
                    ),
                new Tuple<Tuple<byte,byte>,Tuple<byte,byte>>(
                    new Tuple<byte,byte>(0xE0,0xEF),
                    new Tuple<byte,byte>(0x80,0xFC)
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
