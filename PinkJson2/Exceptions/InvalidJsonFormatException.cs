using System;
using System.Collections.Generic;

namespace PinkJson2
{
    public class InvalidJsonFormatException : JsonParserException
    {
        public InvalidJsonFormatException(JsonPath path) : base("Invalid json format", path)
        {
        }
    }
}
