using System;

namespace PinkJson.Lexer.Tokens
{
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(int pos) : base($"Unknown element! (Position: {pos})")
        {

        }
    }
}
