using System;

namespace PinkJson2.Formatters
{
    internal static class NumberFormattingHelper
    {
        public static unsafe char* UInt32ToDecChars(char* bufferEnd, uint value)
        {
            do
            {
                uint remainder;
                (value, remainder) = MathHelper.DivRem(value, 10);
                *(--bufferEnd) = (char)(remainder + '0');
            }
            while (value != 0);
            return bufferEnd;
        }

        public static unsafe char* UInt32ToDecChars(char* bufferEnd, uint value, int digits)
        {
            while (--digits >= 0 || value != 0)
            {
                uint remainder;
                (value, remainder) = MathHelper.DivRem(value, 10);
                *(--bufferEnd) = (char)(remainder + '0');
            }
            return bufferEnd;
        }

        public static uint Low32(ulong value) => (uint)value;

        public static uint High32(ulong value) => (uint)((value & 0xFFFFFFFF00000000) >> 32);

        public static uint Int64DivMod1E9(ref ulong value)
        {
            uint rem = (uint)(value % 1000000000);
            value /= 1000000000;
            return rem;
        }
    }
}
