using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson
{
    public static class CharExtension
    {
        public static string Repeat(this char ch, uint count)
        {
            var result = "";
            for (var i = 0; i < count; i++)
                result += ch;
            return result;
        }
    }
}
