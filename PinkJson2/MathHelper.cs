using System;
using System.Runtime.CompilerServices;

namespace PinkJson2
{
    internal static class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (uint Quotient, uint Remainder) DivRem(uint left, uint right)
        {
            uint quotient = left / right;
            return (quotient, left - quotient * right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(float f)
        {
#if NET5_0_OR_GREATER
            return float.IsFinite(f);
#else
            int bits = SingleToInt32Bits(f);
            return (bits & 0x7FFFFFFF) < 0x7F800000;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(double d)
        {
#if NET5_0_OR_GREATER
            return double.IsFinite(d);
#else
            long bits = BitConverter.DoubleToInt64Bits(d);
            return (bits & 0x7FFFFFFFFFFFFFFF) < 0x7FF0000000000000;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int SingleToInt32Bits(float value)
        {
            return *(int*)&value;
        }
    }
}
