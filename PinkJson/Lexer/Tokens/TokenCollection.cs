using System.Collections.Generic;
using System.Linq;
using PinkJson.Lexer.Tokens;

namespace PinkJson.Lexer.Tokens
{
    public class TokenCollection : List<SyntaxToken>
    {
        public TokenCollection Copy()
        {
            SyntaxToken[] copyarr = new SyntaxToken[] { };
            System.Array.Resize(ref copyarr, Count);
            CopyTo(copyarr);
            TokenCollection copy = new TokenCollection();
            copy.AddRange(copyarr);
            return copy;
        }

        public override string ToString()
        {
            return string.Join(", ", this);
        }

        public string ToSimplyString()
        {
            return string.Join("", this.Select(e => e.ToSimplyString()).ToArray());
        }
    }
}
