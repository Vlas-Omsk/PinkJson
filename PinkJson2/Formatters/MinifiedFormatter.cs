using System;
using System.Collections.Generic;
using System.IO;

namespace PinkJson2.Formatters
{
    public sealed class MinifiedFormatter : IFormatter
    {
        private readonly TypeConverter _typeConverter;

        private sealed class Formatter : IDisposable
        {
            private readonly TextWriter _writer;
            private readonly ValueFormatter _valueFormatter;
            private readonly IEnumerator<JsonEnumerableItem> _enumerator;
            private JsonEnumerableItem _current;

            public Formatter(TextWriter writer, TypeConverter typeConverter, IEnumerator<JsonEnumerableItem> enumerator)
            {
                _writer = writer;
                _valueFormatter = new ValueFormatter(writer, typeConverter);
                _enumerator = enumerator;
            }

            public void Format()
            {
                MoveNext();

                FormatJson();

                _enumerator.Dispose();
            }

            private void FormatJson()
            {
                switch (_current.Type)
                {
                    case JsonEnumerableItemType.ObjectBegin:
                        FormatObject();
                        break;
                    case JsonEnumerableItemType.ArrayBegin:
                        FormatArray();
                        break;
                    case JsonEnumerableItemType.Key:
                        FormatKeyValue();
                        break;
                    case JsonEnumerableItemType.Value:
                        FormatValue();
                        break;
                    default:
                        throw new UnexpectedJsonEnumerableItemException(
                            _current,
                            new JsonEnumerableItemType[]
                            {
                                JsonEnumerableItemType.ObjectBegin,
                                JsonEnumerableItemType.ArrayBegin,
                                JsonEnumerableItemType.Key,
                                JsonEnumerableItemType.Value
                            }
                        );
                }
            }

            private void FormatObject()
            {
                _writer.Write(ValueFormatter.LeftBrace);
                if (_current.Type != JsonEnumerableItemType.ObjectEnd)
                {
                    MoveNext();
                    while (_current.Type != JsonEnumerableItemType.ObjectEnd)
                    {
                        FormatKeyValue();
                        MoveNext();
                        if (_current.Type != JsonEnumerableItemType.ObjectEnd)
                            _writer.Write(ValueFormatter.Comma);
                    }
                }
                _writer.Write(ValueFormatter.RightBrace);
            }

            private void FormatKeyValue()
            {
                _writer.Write(ValueFormatter.Quote);
                ((string)_current.Value).EscapeString(_writer);
                _writer.Write(ValueFormatter.Quote);
                _writer.Write(ValueFormatter.Colon);
                MoveNext();
                FormatValue();
            }

            private void FormatArray()
            {
                _writer.Write(ValueFormatter.LeftBracket);
                if (_current.Type != JsonEnumerableItemType.ArrayEnd)
                {
                    MoveNext();
                    while (_current.Type != JsonEnumerableItemType.ArrayEnd)
                    {
                        FormatValue();
                        MoveNext();
                        if (_current.Type != JsonEnumerableItemType.ArrayEnd)
                            _writer.Write(ValueFormatter.Comma);
                    }
                }
                _writer.Write(ValueFormatter.RightBracket);
            }

            private void FormatValue()
            {
                if (JsonEnumerableItemType.ObjectBegin == _current.Type || JsonEnumerableItemType.ArrayBegin == _current.Type)
                    FormatJson();
                else
                    _valueFormatter.FormatValue(_current.Value);
            }

            private void MoveNext()
            {
                if (!_enumerator.MoveNext())
                    throw new UnexpectedEndOfJsonEnumerableException();

                _current = _enumerator.Current;
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }

        public MinifiedFormatter() : this(TypeConverter.Default)
        {
        }

        public MinifiedFormatter(TypeConverter typeConverter)
        {
            _typeConverter = typeConverter;
        }

        public void Format(IEnumerable<JsonEnumerableItem> json, TextWriter writer)
        {
            using (var formatter = new Formatter(writer, _typeConverter, json.GetEnumerator()))
                formatter.Format();
        }
    }
}
