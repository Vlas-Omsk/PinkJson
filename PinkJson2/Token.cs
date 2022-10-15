using System;

namespace PinkJson2
{
    public readonly struct Token
    {
        public Token(TokenType type, int position, int length, object value)
        {
            Type = type;
            Position = position;
            Length = length;
            Value = value;
        }

        public TokenType Type { get; }
        public int Position { get; }
        public int Length { get; }
        public object Value { get; }
    }
}
