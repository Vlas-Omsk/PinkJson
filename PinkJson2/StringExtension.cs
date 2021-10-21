using System;
using System.Text;

namespace PinkJson2
{
    public static class StringExtension
    {
        public static string Repeat(this string str, int count)
        {
            var result = new StringBuilder();
            for (var i = 0; i < count; i++)
                result.Append(str);
            return result.ToString();
        }

        public static string EscapeString(this string value)
        {
            StringBuilder result = new StringBuilder();
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
            string result = "";
            bool escape = false;
            for (var i = 0; i < value.Length; i++)
            {
                if (escape)
                {
                    escape = false;
                    switch (value[i])
                    {
                        case 'b':
                            result += '\b';
                            goto end;
                        case 'a':
                            result += '\a';
                            goto end;
                        case 'f':
                            result += '\f';
                            goto end;
                        case 'n':
                            result += '\n';
                            goto end;
                        case 'r':
                            result += '\r';
                            goto end;
                        case 't':
                            result += '\t';
                            goto end;
                        case 'u':
                            string unicode_value = "";
                            for (var ii = 0; ii < 4; ii++)
                            {
                                i++;
                                unicode_value += value[i];
                            }
                            result += (char)Convert.ToInt32(unicode_value, 16);
                            goto end;
                        case '0':
                            result += ("\0");
                            goto end;
                        case '"':
                            result += '\"';
                            goto end;
                        case '\\':
                            result += '\\';
                            goto end;
                        default:
                            result += value[i];
                            goto end;
                            //throw new Exception($"Unidentified escape sequence \\{value[i]} at position {i}.");
                    }
                }
                if (value[i] == '\\')
                {
                    escape = true;
                    goto end;
                }

                result += value[i];

                end:;
            }
            return result;
        }

        public static string ToUnicodeString(this string value)
        {
            string result = "";
            for (var i = 0; i < value.Length; i++)
            {
                var val = Convert.ToString(value[i], 16);
                result += "\\u" + new string('0', 4 - val.Length) + val;
            }

            return result;
        }
    }
}
