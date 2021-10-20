using System;

namespace PinkJson2
{
    public class InvalidJsonFormatException : Exception
    {
        public InvalidJsonFormatException() : base("Invalid json format")
        {
        }
    }
}
