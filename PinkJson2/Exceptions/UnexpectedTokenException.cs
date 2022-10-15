using System;

namespace PinkJson2
{
    public class UnexpectedTokenException : JsonParserException
    {
        public UnexpectedTokenException(Token token, TokenType[] expectedTokenTypes, JsonPath path) :
            base($"Unexpectedt token {token.Type} expected {string.Join(", ", expectedTokenTypes)} (Position: {token.Position})", path)
        {
            Token = token;
        }

        public Token Token { get; }
    }
}
