using System;
using System.IO;

namespace PinkJson2.Formatters
{
    public static class Formatter
    {
        public static void FormatValue(object value, TextWriter writer)
        {
            if (value is null)
                writer.Write("null");
            else if (value is bool b)
                writer.Write(b ? "true" : "false");
            else if (value is DateTime dt)
            {
                writer.Write('\"');
                writer.Write(dt.ToISO8601String());
                writer.Write('\"');
            }
            else if (value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal)
                writer.Write(value.ToString().Replace(',', '.'));
            else
            {
                writer.Write('\"');
                value.ToString().EscapeString(writer);
                writer.Write('\"');
            }
        }

        public static string GetIndent(IndentStyle style, int size)
        {
            switch (style)
            {
                case IndentStyle.Space:
                    return ' '.Repeat(size);
                case IndentStyle.Tab:
                    return '\t'.Repeat(size);
                default:
                    throw new Exception();
            }
        }
    }
}
