using System;
using System.Collections.Generic;

namespace PinkJson2
{
    public class InvalidJsonFormatException : JsonParserException
    {
        public InvalidJsonFormatException(IEnumerable<string> path) : base("Invalid json format", path)
        {
        }
    }
}
