using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public InvalidTokenException(int pos) : base($"Unknown element! (Position: {pos})")
        {
        }
    }
}
