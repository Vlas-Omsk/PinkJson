using System;

namespace PinkJson2
{
    public static class CharExtension
    {
        public static string Repeat(this char c, int count)
        {
            return new string(c, count);
        }
    }
}
