using PinkJson2.Formatters;
using System;
using System.Collections.Generic;
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
            var flushIndex = 0;

            for (var i = 0; i < self.Length; i++)
            {
                var ch = self[i];

                switch (ch)
                {
                    case '\b':
                        FlushString(self, ref flushIndex, i, writer);
                        writer.Write("\\b");
                        break;
                    case '\a':
                        FlushString(self, ref flushIndex, i, writer);
                        writer.Write("\\a");
                        break;
                    case '\f':
                        FlushString(self, ref flushIndex, i, writer);
                        writer.Write("\\f");
                        break;
                    case '\n':
                        FlushString(self, ref flushIndex, i, writer);
                        writer.Write("\\n");
                        break;
                    case '\r':
                        FlushString(self, ref flushIndex, i, writer);
                        writer.Write("\\r");
                        break;
                    case '\t':
                        FlushString(self, ref flushIndex, i, writer);
                        writer.Write("\\t");
                        break;
                    case '\0':
                        FlushString(self, ref flushIndex, i, writer);
                        writer.Write("\\0");
                        break;
                    case '\"':
                        FlushString(self, ref flushIndex, i, writer);
                        writer.Write("\\\"");
                        break;
                    case '\\':
                        FlushString(self, ref flushIndex, i, writer);
                        writer.Write("\\\\");
                        break;
                }
            }

            FlushString(self, ref flushIndex, self.Length, writer);
        }

        private static void FlushString(string str, ref int flushIndex, int i, TextWriter writer)
        {
            FlushStringInternal(str, ref flushIndex, i, writer);

            flushIndex = i + 1;
        }

        private static void FlushStringInternal(string str, ref int flushIndex, int i, TextWriter writer)
        {
            if (flushIndex == 0 && i == str.Length)
            {
                writer.Write(str);
                return;
            }
            
            var length = i - flushIndex;

            if (length == 0)
                return;

            if (length == 1)
            {
                writer.Write(str[flushIndex]);
                return;
            }

#if NET5_0_OR_GREATER
            writer.Write(str.AsSpan(flushIndex, length));
#else
            writer.Write(str.Substring(flushIndex, length));
#endif
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
