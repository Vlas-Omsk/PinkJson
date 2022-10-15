using System;

namespace PinkJson2.Formatters
{
    public static class Formatter
    {
        public static string FormatValue(object value)
        {
            string str;

            if (value is null)
                str = "null";
            else if (value is bool b)
                str = b ? "true" : "false";
            else if (value is DateTime dt)
                str = '\"' + dt.ToISO8601String() + '\"';
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
                str = value.ToString().Replace(',', '.');
            else
                str = $"\"{value.ToString().EscapeString()}\"";

            return str;
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
