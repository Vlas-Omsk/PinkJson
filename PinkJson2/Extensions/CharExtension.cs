﻿using System;

namespace PinkJson2
{
    public static class CharExtension
    {
        public static string Repeat(this char c, int count)
        {
            return new string(c, count);
        }

        public static string ToUnicode(this char c)
        {
            var val = Convert.ToString(c, 16);
            return "\\u" + new string('0', 4 - val.Length) + val;
        }
    }
}
