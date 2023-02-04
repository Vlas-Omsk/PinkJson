using GrisuDotNet;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace PinkJson2.Formatters
{
    public sealed class ValueFormatter
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

        private static readonly NumberFormatInfo _numberFormatInfo = new NumberFormatInfo()
        {
            NaNSymbol = NullValue,
            NegativeInfinitySymbol = NullValue,
            PositiveInfinitySymbol = NullValue,
            NumberDecimalSeparator = "."
        };
        [ThreadStatic]
        private static string _buffer;
        private readonly TextWriter _writer;
        private readonly TypeConverter _typeConverter;
        private readonly GrisuWriter _grisuWriter;

        public ValueFormatter(TextWriter writer, TypeConverter typeConverter)
        {
            _writer = writer;
            _typeConverter = typeConverter;
            _grisuWriter = new GrisuWriter(writer, _numberFormatInfo);

            if (_buffer == null)
                _buffer = StringHelper.FastAllocateString(FormattingHelpers.CountDigits(ulong.MaxValue));
        }

        public void FormatValue(object value)
        {
            if (TryFormatValue(value))
                return;

            if (_typeConverter.TryChangeType(value, typeof(FormattedValue), out var result))
            {
                var formattedValue = (FormattedValue)result;

                if (TryFormatValue(formattedValue.Value))
                    return;

                value = formattedValue.Value;
            }

            FormatString(value.ToString());
        }

        public bool TryFormatValue(object value)
        {
            if (value is null)
            {
                _writer.Write(NullValue);
                return true;
            }
            else if (value is bool bl)
            {
                _writer.Write(bl ? TrueValue : FalseValue);
            }
            else if (value is DateTime dt)
            {
                _writer.Write(Quote);
                _writer.Write(dt.ToISO8601String());
                _writer.Write(Quote);
                return true;
            }
            else if (value is int i)
            {
                FormatInt32Value(i);
                return true;
            }
            else if (value is uint ui)
            {
                FormatUInt32Value(ui);
                return true;
            }
            else if (value is long l)
            {
                FormatInt64Value(l);
                return true;
            }
            else if (value is ulong ul)
            {
                FormatUInt64Value(ul);
                return true;
            }
            else if (value is short s)
            {
                FormatInt32Value(s);
                return true;
            }
            else if (value is ushort us)
            {
                FormatUInt32Value(us);
                return true;
            }
            else if (value is sbyte sb)
            {
                FormatInt32Value(sb);
                return true;
            }
            else if (value is byte b)
            {
                FormatUInt32Value(b);
                return true;
            }
            else if (value is float f)
            {
                FormatFloatValue(f);
                return true;
            }
            else if (value is double d)
            {
                FormatDoubleValue(d);
                return true;
            }
            else if (value is decimal m)
            {
                FormatDoubleValue((double)m);
                return true;
            }
            else if (value is string str)
            {
                FormatString(str);
                return true;
            }

            return false;
        }

        private void FormatString(string str)
        {
            _writer.Write(Quote);
            str.EscapeString(_writer);
            _writer.Write(Quote);
        }

        private void FormatInt32Value(int value)
        {
            if (value < 0)
            {
                _writer.Write(_numberFormatInfo.NegativeSign);
                value = -value;
            }

            FormatUInt32Value((uint)value);
        }

        private unsafe void FormatUInt32Value(uint value)
        {
            var length = FormattingHelpers.CountDigits(value);

            fixed (char* buffer = _buffer)
            {
                char* p = buffer + length;
                p = NumberFormattingHelper.UInt32ToDecChars(p, value);
                Debug.Assert(p == buffer);
            }

            WriteFromBuffer(length);
        }

        private void FormatInt64Value(long value)
        {
            if (value < 0)
            {
                _writer.Write(_numberFormatInfo.NegativeSign);
                value = -value;
            }

            FormatUInt64Value((ulong)value);
        }

        private unsafe void FormatUInt64Value(ulong value)
        {
            var length = FormattingHelpers.CountDigits(value);
            var digits = 1;

            fixed (char* buffer = _buffer)
            {
                char* p = buffer + length;
                while (NumberFormattingHelper.High32(value) != 0)
                {
                    p = NumberFormattingHelper.UInt32ToDecChars(p, NumberFormattingHelper.Int64DivMod1E9(ref value), 9);
                    digits -= 9;
                }
                p = NumberFormattingHelper.UInt32ToDecChars(p, NumberFormattingHelper.Low32(value), digits);
                Debug.Assert(p == buffer);
            }

            WriteFromBuffer(length);
        }

        private void FormatFloatValue(float value)
        {
            if (!MathHelper.IsFinite(value))
            {
                _writer.Write(NullValue);
                return;
            }
            
            _grisuWriter.WriteDouble(new GrisuDouble(value));
        }

        private void FormatDoubleValue(double value)
        {
            if (!MathHelper.IsFinite(value))
            {
                _writer.Write(NullValue);
                return;
            }

            _grisuWriter.WriteDouble(new GrisuDouble(value));
        }

        private void WriteFromBuffer(int length)
        {
            for (var i = 0; i < length; i++)
                _writer.Write(_buffer[i]);
        }
    }
}
