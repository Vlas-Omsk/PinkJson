using System;

namespace PinkJson2
{
    public struct Token
    {
        public TokenType Type { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public object Value { get; set; }

        public Token(TokenType type, int position, int length, object value)
        {
            Type = type;
            Position = position;
            Length = length;
            Value = value;
        }
    }
}
