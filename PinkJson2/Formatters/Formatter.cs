using System;
using System.IO;

namespace PinkJson2.Formatters
{
    public static class Formatter
    {
        public const char Quote = '"';
        public const char LeftBrace = '{';
        public const char RightBrace = '}';
        public const char LeftBracket = '[';
        public const char RightBracket = ']';
        public const char Colon = ':';
        public const char Comma = ',';
        public const char Space = ' ';
        public const char Tab = '\t';
        public const string NullValue = "null";
        public const string TrueValue = "true";
        public const string FalseValue = "false";

        private static readonly string _buffer = FastNumberFormat.FastAllocateString(FastNumberFormat.CountDigits(ulong.MaxValue));

        public static void FormatValue(object value, TextWriter writer)
        {
            if (value is null)
            {
                writer.Write(NullValue);
            }
            else if (value is bool b)
            {
                writer.Write(b ? TrueValue : FalseValue);
            }
            else if (value is DateTime dt)
            {
                writer.Write(Quote);
                writer.Write(dt.ToISO8601String());
                writer.Write(Quote);
            }
            else if (value is int i)
            {
                FormatInt32Value(i, writer);
            }
            else if (value is uint u)
            {
                FormatUInt32Value(u, writer);
            }
            else if (
                value is sbyte || 
                value is byte ||
                value is short ||
                value is ushort ||
                value is long ||
                value is ulong
            )
            {
                writer.Write(value.ToString());
            }
            else if (
                value is float || 
                value is double ||
                value is decimal
            )
            {
                writer.Write(value.ToString().Replace(',', '.'));
            }
            else if (value is string str)
            {
                FormatString(str, writer);
            }
            else
            {
                FormatString(value.ToString(), writer);
            }
        }

        private static void FormatString(string str, TextWriter writer)
        {
            writer.Write(Quote);
            str.EscapeString(writer);
            writer.Write(Quote);
        }

        private static void FormatInt32Value(int value, TextWriter writer)
        {
            if (value >= 0)
                FormatUInt32Value((uint)value, writer);
            else
                writer.Write(value.ToString());
        }

        private static void FormatUInt32Value(uint value, TextWriter writer)
        {
            var length = FastNumberFormat.CountDigits(value);

            FastNumberFormat.FormatUInt32(value, length, _buffer);
            WriteFromBuffer(length, writer);
        }

        private static void WriteFromBuffer(int length, TextWriter writer)
        {
            for (var i = 0; i < length; i++)
                writer.Write(_buffer[i]);
        }
    }
}
