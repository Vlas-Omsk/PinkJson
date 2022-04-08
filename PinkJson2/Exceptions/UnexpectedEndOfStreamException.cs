using System;
using System.Collections.Generic;

namespace PinkJson2
{
    public class UnexpectedEndOfStreamException : JsonParserException
    {
        public UnexpectedEndOfStreamException(TokenType[] expectedTokenTypes, IEnumerable<string> path) : 
            base($"Unexpected end of stream expected {string.Join(", ", expectedTokenTypes)}", path)
        {
        }
    }
}
