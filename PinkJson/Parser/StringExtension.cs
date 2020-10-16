using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson.Parser
{
    public static class StringExtension
    {
        public static string Trim(this string str, int count)
        {
            return str.Substring(count, str.Length - (count * 2));
        }
    }
}
