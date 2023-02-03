using System;

namespace PinkJson2
{
    internal static class MemoizationHelper
    {
        public static int GetHashCode<T>(T value)
        {
            return value.GetHashCode();
        }

        public static int GetHashCode<T>(T value1, T value2)
        {
#if NETSTANDARD2_0
            // I hope this works
            return value1.GetHashCode() - value2.GetHashCode();
#else
            return HashCode.Combine(value1, value2);
#endif
        }
    }
}
