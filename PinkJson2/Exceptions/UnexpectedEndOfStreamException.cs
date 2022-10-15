using System;

namespace PinkJson2
{
    public class UnexpectedEndOfStreamException : JsonParserException
    {
        public UnexpectedEndOfStreamException(TokenType[] expectedTokenTypes, JsonPath path) : 
            base($"Unexpected end of stream expected {string.Join(", ", expectedTokenTypes)}", path)
        {
        }
    }
}
