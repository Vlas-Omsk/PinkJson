using System;

namespace PinkJson2.Formatters
{
    public static class Formatter
    {
        public static string FormatValue(object value, Action<IJson> formatJson)
        {
            string str;

            if (value is null)
                str = "null";
            else if (value is bool)
                str = ((bool)value) ? "true" : "false";
            else if (value is DateTime)
                str = '\"' + ((DateTime)value).ToISO8601String() + '\"';
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
            else if (value is IJson)
            {
                formatJson(value as IJson);
                return null;
            }
            else
                str = $"\"{value.ToString().EscapeString()}\"";

            return str;
        }
    }
}
