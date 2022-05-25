using System;

namespace PinkJson2
{
    public sealed class Token
    {
        public TokenType Type { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public object Value { get; set; }

        public Token()
        {
        }
    }
}
