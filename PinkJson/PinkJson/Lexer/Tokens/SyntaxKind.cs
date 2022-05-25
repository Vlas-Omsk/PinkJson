namespace PinkJson.Lexer.Tokens
{
    public enum SyntaxKind
    {
        OB,
        CB,
        OBA,
        CBA,

        NUMBER,
        STRING,
        BOOL,
        NULL,

        EQSEPARATOR,
        SEPARATOR,

        //Invisible is \0, \n, any whitespaces
        Invisible,
        Comment,
        InvalidToken
    }

    public static class SyntaxKindExpression
    {
        public static string KindToString(this SyntaxKind kind)
        {
            return nameof(kind);
        }
    }
}
