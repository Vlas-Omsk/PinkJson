using System;
using System.IO;

namespace PinkJson2
{
    public class InvalidTokenException : JsonLexerException
    {
        public InvalidTokenException(int position, StreamReader stream) : base("Invalid token", position, stream)
        {
        }
    }
}
