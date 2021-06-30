using PinkJson.Impl;
using PinkJson.Lexer.Tokens;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Numerics;

namespace PinkJson.Lexer
{
    public class JsonLexer
    {
        private string Content;
        private int ContentLen { get => Content.Length; }
        private LexerPosition LexerPosition;
        private SyntaxKind Kind;
        private object Value;
        public JsonLexer()
        {
            LexerPosition = new LexerPosition(0, 0);
        }
        private char Current { get => Peek(0); }
        private char Lookahead { get => Peek(1); }
        private char Peek(int offset = 0)
        {
            int index = LexerPosition.CurrentPosition + offset;
            if (index >= ContentLen)
                return '\0';
            return Content[index];
        }
        public TokenCollection Tokenize(string Content)
        {
            this.Content = Content;
            TokenCollection collection = new TokenCollection();
            SyntaxToken token;
            while (LexerPosition.CurrentPosition < ContentLen)
            {
                token = Get();
                if (token.Kind == SyntaxKind.InvalidToken)
                    throw new InvalidTokenException(LexerPosition.CurrentPosition, Content);
                if (token.Kind != SyntaxKind.Invisible)
                    collection.Add(token);
            }
            LexerPosition = new LexerPosition(0, 0);
            Value = null;
            return collection;
        }
        private SyntaxToken Get()
        {
            LexerPosition.StartPosition = LexerPosition.CurrentPosition;
            Kind = SyntaxKind.InvalidToken;
            Value = null;
            switch (Current)
            {
                //case '\0':
                //    Kind = SyntaxKind.EOE;
                //    break;
                //case '\n':
                //case ' ':
                //case '\t':
                //case '\r':
                //    ReadWhiteSpace();
                //    break;
                case ':':
                case ',':
                case '{':
                case '[':
                case ']':
                case '}':
                    ReadOperators();
                    break;
                case '-':
                case '+':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '0':
                    ReadNumber();
                    break;
                case '"':
                    ReadString();
                    break;
                default:
                    if (char.IsWhiteSpace(Current) || Current == '\0')
                        ReadWhiteSpace();
                    else
                        ReadOther();
                    break;
            }
            string Text = Content.Substring(LexerPosition.StartPosition, LexerPosition.CurrentPosition - LexerPosition.StartPosition);
            return new SyntaxToken(Kind, Text, new ElementPosition(LexerPosition.StartPosition, LexerPosition.CurrentPosition - LexerPosition.StartPosition), Value);
        }
        private void ReadWhiteSpace()
        {
            while (char.IsWhiteSpace(Current) || (Current == '\0' && LexerPosition.CurrentPosition < ContentLen))
                LexerPosition.CurrentPosition++;
            Kind = SyntaxKind.Invisible;
        }
        private readonly static char[] numberChars =
        {
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9',
            '.',
            '-',
            '+',
            'X',
            'x',
            'E',
            'e'
        }, hexadecimalChars = {
            'a',
            'b',
            'c',
            'd',
            'f',
            'A',
            'B',
            'C',
            'D',
            'F',
        };
        private void ReadNumber()
        {
            bool isDouble = false, isEnumber = false, isHexadecimal = false;
            char prev = Current;
            while (numberChars.Contains(Current) || (isHexadecimal && hexadecimalChars.Contains(Current)))
            {
                if ((Current == '.') && !isHexadecimal)
                {
                    if (isDouble)
                        throw new Exception("Invalid double number");
                    else
                        isDouble = true;
                }
                if ((Current == 'e' || Current == 'E') && !isHexadecimal)
                {
                    if (isEnumber)
                        throw new Exception("Invalid e number");
                    else
                        isEnumber = true;
                }
                if ((Current == 'x' || Current == 'X') && prev == '0')
                {
                    if (isHexadecimal)
                        throw new Exception("Invalid hexadecimal number");
                    else
                        isHexadecimal = true;
                }

                prev = Current;
                LexerPosition.CurrentPosition++;
            }

            int len = LexerPosition.CurrentPosition - LexerPosition.StartPosition;
            string str = Content.Substring(LexerPosition.StartPosition, len);
            
            if (isEnumber || isDouble)
            {
                if (!double.TryParse(str.Replace('.', ','), out double value))
                    throw new Exception($"Invalid double number {str}");
                Value = value;
            } else if (isHexadecimal)
            {
                try
                {
                    Value = Convert.ToInt32(str, 16);
                }
                catch (Exception ex)
                {
                    try
                    {
                        Value = Convert.ToInt64(str, 16);
                    }
                    catch
                    {
                        throw new Exception($"Invalid hexadecimal number {str}", ex);
                    }
                }
            } else
            {
                if (int.TryParse(str, out int intvalue))
                    Value = intvalue;
                else if (long.TryParse(str, out long longvalue))
                    Value = longvalue;
                else if (BigInteger.TryParse(str, out BigInteger bigintvalue))
                    Value = bigintvalue;
                else
                    throw new Exception($"Invalid or too big number {str}");
            }

            //bool isDouble = false;
            //bool isTempEnumber = false, isTempXnumber = false, isXnumber = false;

            //while (char.IsDigit(Current) || (Current == 'e' || Current == 'E' || Current == 'x' || Current == 'X') || isTempEnumber || isTempXnumber || Current == '-' || Current == '+' ||
            //    (isXnumber && (Current == 'A' || Current == 'a' || Current == 'B' || Current == 'b' || Current == 'C' || Current == 'c' || Current == 'D' || Current == 'd' || Current == 'e' || Current == 'E' || Current == 'F' || Current == 'f')))
            //{
            //    if (isTempEnumber)
            //        isDouble = true;
            //    if (isTempXnumber)
            //        isXnumber = true;
            //    //isTempEnumber = Current == 'e' || Current == 'E';
            //    isTempXnumber = Current == 'x' || Current == 'X';

            //    if (Lookahead is '.')
            //    {
            //        LexerPosition.CurrentPosition++;
            //        isDouble = true;
            //    }
            //    LexerPosition.CurrentPosition++;
            //}

            //int len = LexerPosition.CurrentPosition - LexerPosition.StartPosition;
            //string str = Content.Substring(LexerPosition.StartPosition, len);
            //if (!isDouble)
            //{
            //    var isParsed = false;
            //    if (isXnumber)
            //        try
            //        {
            //            Value = Convert.ToInt32(str, 16);
            //            isParsed = true;
            //        }
            //        catch
            //        {
            //            isParsed = false;
            //        }
            //    if (!isParsed)
            //        if (int.TryParse(str, out int intvalue))
            //            Value = intvalue;
            //        else if (long.TryParse(str, out long longvalue))
            //            Value = longvalue;
            //        else if (BigInteger.TryParse(str, out BigInteger bigintvalue))
            //            Value = bigintvalue;
            //        else
            //            throw new Exception($"Invalid or too big number {str}");
            //}
            //else {
            //    //))))))))))
            //    if (!double.TryParse(str.Replace('.', ','), out double value))
            //        throw new Exception($"Invalid double number {str}");
            //    Value = value;
            //}
            Kind = SyntaxKind.NUMBER;
        }
        private void ReadString()
        {
            bool escape = false;
            StringBuilder value = new StringBuilder("");
            LexerPosition.CurrentPosition++;
            while (Current != '"' || escape == true)
            {
                if (escape)
                {
                    escape = false;
                    switch (Current)
                    {
                        case 'b':
                            value.Append('\b');
                            goto end;
                        case 'a':
                            value.Append('\a');
                            goto end;
                        case 'f':
                            value.Append('\f');
                            goto end;
                        case 'n':
                            value.Append('\n');
                            goto end;
                        case 'r':
                            value.Append('\r');
                            goto end;
                        case 't':
                            value.Append('\t');
                            goto end;
                        case 'u':
                            string unicode_value = "";
                            for (var i = 0; i < 4; i++)
                            {
                                LexerPosition.CurrentPosition++;
                                unicode_value += Current;
                            }
                            value.Append((char)Convert.ToInt32(unicode_value, 16));
                            goto end;
                        case '"':
                            value.Append('\"');
                            goto end;
                        case '\\':
                            value.Append('\\');
                            goto end;
                        case '/':
                            value.Append('/');
                            goto end;
                        default:
                            throw new Exception($"Unidentified escape sequence \\{Current} at position {LexerPosition.CurrentPosition}.");
                    }
                }
                if (Current == '\\')
                {
                    escape = true;
                    goto end;
                }

                value.Append(Current);

                end:
                LexerPosition.CurrentPosition++;
            }
            LexerPosition.CurrentPosition++;

            //int len = LexerPosition.CurrentPosition - LexerPosition.StartPosition;
            //Value = Content.Substring(LexerPosition.StartPosition, len);
            Value = value.ToString();

            Kind = SyntaxKind.STRING;
        }
        private void ReadOperators()
        {
            if (Current is ':')
                Kind = SyntaxKind.EQSEPARATOR;
            else if (Current is ',')
                Kind = SyntaxKind.SEPARATOR;
            else if (Current is '{')
                Kind = SyntaxKind.OB;
            else if (Current is '}')
                Kind = SyntaxKind.CB;
            else if (Current is '[')
                Kind = SyntaxKind.OBA;
            else if (Current is ']')
                Kind = SyntaxKind.CBA;
            LexerPosition.CurrentPosition++;
        }
        private void ReadOther()
        {
            while (char.IsLetterOrDigit(Current))
                LexerPosition.CurrentPosition++;

            int len = LexerPosition.CurrentPosition - LexerPosition.StartPosition;
            var str = Content.Substring(LexerPosition.StartPosition, len);

            if (str is "null")
                Kind = SyntaxKind.NULL;
            else if (bool.TryParse(str, out bool value))
            {
                Value = value;
                Kind = SyntaxKind.BOOL;
            }
            else
                Kind = SyntaxKind.InvalidToken;
        }
    }
}
