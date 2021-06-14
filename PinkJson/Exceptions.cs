using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace PinkJson
{
    public class InvalidTypeException : Exception
    {
        public InvalidTypeException(string message) : base(message)
        {
        }
    }

    public class InvalidTokenException : Exception
    {
        const short borders = 200;

        public InvalidTokenException(int pos, string content) : base()
        {
            var result = $"Unknown element! (Position: {pos})\r\n\r\nDetails:\r\n" + 
                content.Substring(pos - borders - 1, borders * 2)/*.Insert(borders + 1, " ")*/.Insert(borders, " ---> ");
            typeof(Exception).GetRuntimeFields().First(fi => fi.Name == "_message").SetValue(this, result);
        }
    }
}
