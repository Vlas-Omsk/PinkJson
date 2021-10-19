using System;

namespace PinkJson2
{
    public enum TokenType
    {
        Invalid,
        Invisible,    // \0, \n, any whitespaces

        String,       // "string"
        Number,       // 123, 0x0f, 10e-10
        Boolean,      // true or false
        Null,         // null
        Comment,      // // comment

        Colon,        // :
        Comma,        // ,
        LeftBrace,    // {
        RightBrace,   // }
        LeftBracket,  // [
        RightBracket, // ]
    }
}
