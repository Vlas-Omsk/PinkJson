using System;
using System.IO;

namespace PinkJson2
{
    public class UnexpectedTokenException : JsonException
    {
        public UnexpectedTokenException(Token token, TokenType[] expectedTokenTypes, StreamReader stream) :
            base($"Unexpectedt token {token.Type} expected {string.Join(", ", expectedTokenTypes)}", token.Position, stream)
        {
        }
    }
}
