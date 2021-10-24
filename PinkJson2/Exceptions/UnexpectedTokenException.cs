using System;
using System.Collections.Generic;

namespace PinkJson2
{
    public class UnexpectedTokenException : JsonParserException
    {
        public Token Token { get; }

        public UnexpectedTokenException(Token token, TokenType[] expectedTokenTypes, Stack<string> path) :
            base($"Unexpectedt token {token.Type} expected {string.Join(", ", expectedTokenTypes)} (Position: {token.Position})", path)
        {
            Token = token;
        }
    }
}
