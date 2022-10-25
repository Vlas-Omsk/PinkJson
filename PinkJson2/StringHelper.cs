using System;
using System.Reflection;

namespace PinkJson2
{
    internal static class StringHelper
    {
        private static MethodInfo _fastAllocateStringMethod = typeof(string)
            .GetMethod("FastAllocateString", BindingFlags.Static | BindingFlags.NonPublic);

        public static string FastAllocateString(int length)
        {
            return (string)_fastAllocateStringMethod.Invoke(null, new object[] { length });
        }
    }
}
