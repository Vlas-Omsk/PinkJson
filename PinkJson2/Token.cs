using System;

namespace PinkJson2
{
    public class Token
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
