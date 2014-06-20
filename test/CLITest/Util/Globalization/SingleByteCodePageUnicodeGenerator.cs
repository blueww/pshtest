namespace Management.Storage.ScenarioTest.Util.Globalization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public abstract class SingleByteCodePageUnicodeGenerator : UnicodeGenerator
    {
        public SingleByteCodePageUnicodeGenerator(int codePage, params char[] excludedCharacters)
            : base(codePage, excludedCharacters)
        {
        }

        protected abstract Tuple<byte, byte>[] GetRanges();

        protected override byte[] GenerateRandomBytesWithinValidRange(int length)
        {
            var ranges = this.GetRanges();
            int totalCharacterCount = ranges.Sum(x => x.Item2 - x.Item1 + 1);
            byte[] buffer = new byte[length];
            for (int i = 0; i < length; )
            {
                int index = this.Random.Next(totalCharacterCount);
                foreach (var range in ranges)
                {
                    if (range.Item1 + index > range.Item2)
                    {
                        index -= range.Item2 - range.Item1 + 1;
                    }
                    else
                    {
                        buffer[i++] = (byte)(range.Item1 + index);
                        break;
                    }
                }
            }

            return buffer;
        }
    }
}
