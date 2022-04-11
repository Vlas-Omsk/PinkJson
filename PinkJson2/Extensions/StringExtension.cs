using System;
using System.Linq;
using System.Text;

namespace PinkJson2
{
    public static class StringExtension
    {
        private static readonly char[] _hexadecimalChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        public static string Repeat(this string str, int count)
        {
            var result = new StringBuilder();
            for (var i = 0; i < count; i++)
                result.Append(str);
            return result.ToString();
        }

        public static string EscapeString(this string value)
        {
            var result = new StringBuilder();

            for (var i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case '\b':
                        result.Append("\\b");
                        break;
                    case '\a':
                        result.Append("\\a");
                        break;
                    case '\f':
                        result.Append("\\f");
                        break;
                    case '\n':
                        result.Append("\\n");
                        break;
                    case '\r':
                        result.Append("\\r");
                        break;
                    case '\t':
                        result.Append("\\t");
                        break;
                    case '\0':
                        result.Append("\\0");
                        break;
                    case '\"':
                        result.Append("\\\"");
                        break;
                    case '\\':
                        result.Append("\\\\");
                        break;
                    default:
                        result.Append(value[i]);
                        break;
                }
            }

            return result.ToString();
        }

        public static string UnescapeString(this string value)
        {
            var result = new StringBuilder();
            var escape = false;

            for (var i = 0; i < value.Length; i++)
            {
                if (escape)
                {
                    escape = false;
                    switch (value[i])
                    {
                        case 'b':
                            result.Append('\b');
                            break;
                        case 'a':
                            result.Append('\a');
                            break;
                        case 'f':
                            result.Append('\f');
                            break;
                        case 'n':
                            result.Append('\n');
                            break;
                        case 'r':
                            result.Append('\r');
                            break;
                        case 't':
                            result.Append('\t');
                            break;
                        case '0':
                            result.Append("\0");
                            break;
                        case 'u':
                            string unicode_value = "";
                            for (var j = 0; j < 4; j++)
                            {
                                i++;
                                if (i >= value.Length || !_hexadecimalChars.Contains(char.ToLowerInvariant(value[i])))
                                    throw new Exception($"The Unicode value must be hexadecimal and 4 characters long");
                                unicode_value += value[i];
                            }
                            result.Append((char)Convert.ToInt32(unicode_value, 16));
                            break;
                        case '"':
                            result.Append('\"');
                            break;
                        case '\\':
                            result.Append('\\');
                            break;
                        case '/':
                            result.Append('/');
                            break;
                        default:
                            throw new Exception($"Unidentified escape sequence \\{value[i]} at position {i}.");
                    }
                }
                else if (value[i] == '\\')
                {
                    escape = true;
                }
                else
                {
                    result.Append(value[i]);
                }
            }

            return result.ToString();
        }

        public static string ToUnicodeString(this string value)
        {
            var result = new StringBuilder();

            for (var i = 0; i < value.Length; i++)
                result.Append(value[i].ToUnicode());

            return result.ToString();
        }
    }
}
