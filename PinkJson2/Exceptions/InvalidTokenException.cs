using System;
using System.IO;

namespace PinkJson2
{
    public class InvalidTokenException : JsonException
    {
        public InvalidTokenException(int position, StreamReader stream) : base("Invalid token", position, stream)
        {
        }
    }
}
