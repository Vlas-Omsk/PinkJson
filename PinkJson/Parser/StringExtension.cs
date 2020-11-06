using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson.Parser
{
    public static class StringExtension
    {
        public static string Trim(this string str, int count)
        {
            return str.Substring(count, str.Length - (count * 2));
        }

        public static string Trim(this string str, int trimstart, int trimend)
        {
            var tmp = str.Substring(trimstart);
            return tmp.Substring(0, tmp.Length - trimend);
        }

        public static string EscapeString(this string value)
        {
            string result = "";
            for (var i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case '\b':
                        result += "\\b";
                        break;
                    case '\a':
                        result += "\\a";
                        break;
                    case '\f':
                        result += "\\f";
                        break;
                    case '\n':
                        result += "\\n";
                        break;
                    case '\r':
                        result += "\\r";
                        break;
                    case '\t':
                        result += "\\t";
                        break;
                    case '\"':
                        result += "\\\"";
                        break;
                    case '\\':
                        result += "\\\\";
                        break;
                    default:
                        result += value[i];
                        break;
                }
            }

            return result;
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
                        case '"':
                            result += '\"';
                            goto end;
                        case '\\':
                            result += '\\';
                            goto end;
                        default:
                            throw new Exception($"Unidentified escape sequence \\{value[i]} at position {i}.");
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
