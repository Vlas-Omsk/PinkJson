using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace PinkJson2
{
    public static class StringExtension
    {
        private static readonly char[] _hexadecimalChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        public static string Repeat(this string self, int count)
        {
            using (var writer = new StringWriter())
            {
                Repeat(self, count, writer);

                return writer.ToString();
            }
        }

        public static void Repeat(this string self, int count, TextWriter writer)
        {
            for (var i = 0; i < count; i++)
                writer.Write(self);
        }

        public static string EscapeString(this string self)
        {
            using (var writer = new StringWriter())
            {
                EscapeString(self, writer);

                return writer.ToString();
            }
        }

        public static void EscapeString(this string self, TextWriter writer)
        {
            for (var i = 0; i < self.Length; i++)
            {
                switch (self[i])
                {
                    case '\b':
                        writer.Write("\\b");
                        break;
                    case '\a':
                        writer.Write("\\a");
                        break;
                    case '\f':
                        writer.Write("\\f");
                        break;
                    case '\n':
                        writer.Write("\\n");
                        break;
                    case '\r':
                        writer.Write("\\r");
                        break;
                    case '\t':
                        writer.Write("\\t");
                        break;
                    case '\0':
                        writer.Write("\\0");
                        break;
                    case '\"':
                        writer.Write("\\\"");
                        break;
                    case '\\':
                        writer.Write("\\\\");
                        break;
                    default:
                        writer.Write(self[i]);
                        break;
                }
            }
        }

        public static string UnescapeString(this string self)
        {
            using (var writer = new StringWriter())
            {
                UnescapeString(self, writer);

                return writer.ToString();
            }
        }

        public static void UnescapeString(this string self, TextWriter writer)
        {
            var escape = false;

            for (var i = 0; i < self.Length; i++)
            {
                if (escape)
                {
                    escape = false;
                    switch (self[i])
                    {
                        case 'b':
                            writer.Write('\b');
                            break;
                        case 'a':
                            writer.Write('\a');
                            break;
                        case 'f':
                            writer.Write('\f');
                            break;
                        case 'n':
                            writer.Write('\n');
                            break;
                        case 'r':
                            writer.Write('\r');
                            break;
                        case 't':
                            writer.Write('\t');
                            break;
                        case '0':
                            writer.Write("\0");
                            break;
                        case 'u':
                            string unicode_value = "";
                            for (var j = 0; j < 4; j++)
                            {
                                i++;
                                if (i >= self.Length || !_hexadecimalChars.Contains(char.ToLowerInvariant(self[i])))
                                    throw new Exception($"The Unicode value must be hexadecimal and 4 characters long");
                                unicode_value += self[i];
                            }
                            writer.Write((char)Convert.ToInt32(unicode_value, 16));
                            break;
                        case '"':
                            writer.Write('\"');
                            break;
                        case '\\':
                            writer.Write('\\');
                            break;
                        case '/':
                            writer.Write('/');
                            break;
                        default:
                            throw new Exception($"Unidentified escape sequence \\{self[i]} at position {i}.");
                    }
                }
                else if (self[i] == '\\')
                {
                    escape = true;
                }
                else
                {
                    writer.Write(self[i]);
                }
            }
        }

        public static string ToUnicodeString(this string self)
        {
            using (var writer = new StringWriter())
            {
                ToUnicodeString(self, writer);

                return writer.ToString();
            }
        }

        public static void ToUnicodeString(this string value, TextWriter writer)
        {
            for (var i = 0; i < value.Length; i++)
                writer.Write(value[i].ToUnicode());
        }
    }
}
