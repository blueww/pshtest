namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class DoubleBytesCodePageUnicodeGenerator : UnicodeGenerator
    {
        public DoubleBytesCodePageUnicodeGenerator(int codePage, params char[] excludedCharacters)
            : base(codePage, excludedCharacters)
        {
        }

        protected abstract Tuple<Tuple<byte, byte>, Tuple<byte, byte>>[] GetLeadFollowByteRanges();

        protected virtual Tuple<ushort, ushort>[] GetOtherRanges()
        {
            return new Tuple<ushort, ushort>[0];
        }

        protected override byte[] GenerateRandomBytesWithinValidRange(int length)
        {
            var otherRanges = this.GetOtherRanges();
            int otherCharacterCount = otherRanges.Sum(x => (int)(x.Item2 - x.Item1 + 1));

            var leadFollowByteRanges = this.GetLeadFollowByteRanges();

            int leadFollowCharacterCount = leadFollowByteRanges.Sum(
                x => ((int)(x.Item1.Item2 - x.Item1.Item1 + 1)) * (x.Item2.Item2 - x.Item2.Item1 + 1));

            int totalCharacterCount = otherCharacterCount + leadFollowCharacterCount;

            byte[] buffer = new byte[length * 2];
            for (int i = 0; i < length * 2; i += 2)
            {
                int index = this.Random.Next(totalCharacterCount);
                Tuple<byte, byte> ch = null;
                if (index < otherCharacterCount)
                {
                    ch = LocateCharacterByIndex(otherRanges, ref index);
                }
                else
                {
                    index -= otherCharacterCount;
                    foreach (var leadFollowRange in leadFollowByteRanges)
                    {
                        int numberOfCharacterInAPage = leadFollowRange.Item2.Item2 - leadFollowRange.Item2.Item1 + 1;
                        int numberOfPages = leadFollowRange.Item1.Item2 - leadFollowRange.Item1.Item1 + 1;
                        int pageOffset = index / numberOfCharacterInAPage;
                        if (pageOffset >= numberOfPages)
                        {
                            index -= numberOfPages * numberOfCharacterInAPage;
                        }
                        else
                        {
                            int pageId = pageOffset + leadFollowRange.Item1.Item1;
                            Debug.Assert(pageId <= leadFollowRange.Item1.Item2);
                            int offsetInAPage = index % numberOfCharacterInAPage;
                            int indexInAPage = leadFollowRange.Item2.Item1 + offsetInAPage;
                            Debug.Assert(indexInAPage <= leadFollowRange.Item2.Item2);
                            ch = new Tuple<byte, byte>((byte)pageId, (byte)indexInAPage);
                            break;
                        }
                    }
                }

                Debug.Assert(ch != null);
                buffer[i] = ch.Item1;
                buffer[i + 1] = ch.Item2;
            }

            return buffer;
        }

        private static Tuple<byte, byte> LocateCharacterByIndex(Tuple<ushort, ushort>[] ranges, ref int index)
        {
            foreach (var range in ranges)
            {
                if (index <= range.Item2 - range.Item1)
                {
                    int ch = (int)range.Item1 + index;
                    return new Tuple<byte, byte>(
                        (byte)(ch >> 8),
                        (byte)(ch));
                }

                index -= (range.Item2 - range.Item1 + 1);
            }

            return null;
        }
    }
}
