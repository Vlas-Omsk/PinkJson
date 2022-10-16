using System;
using System.Collections.Generic;
using System.IO;

namespace PinkJson2.Formatters
{
    public sealed class MinifiedFormatter : IFormatter
    {
        private TextWriter _writer;
        private IEnumerator<JsonEnumerableItem> _enumerator;
        private JsonEnumerableItem _current;

        public void Format(IEnumerable<JsonEnumerableItem> json, TextWriter writer)
        {
            _writer = writer;
            _enumerator = json.GetEnumerator();

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
                    throw new Exception();
            }
        }

        private void FormatObject()
        {
            _writer.Write('{');
            if (_current.Type != JsonEnumerableItemType.ObjectEnd)
            {
                MoveNext();
                while (_current.Type != JsonEnumerableItemType.ObjectEnd)
                {
                    FormatKeyValue();
                    MoveNext();
                    if (_current.Type != JsonEnumerableItemType.ObjectEnd)
                        _writer.Write(',');
                }
            }
            _writer.Write('}');
        }

        private void FormatKeyValue()
        {
            _writer.Write('\"');
            ((string)_current.Value).EscapeString(_writer);
            _writer.Write("\":");
            MoveNext();
            FormatValue();
        }

        private void FormatArray()
        {
            _writer.Write('[');
            if (_current.Type != JsonEnumerableItemType.ArrayEnd)
            {
                MoveNext();
                while (_current.Type != JsonEnumerableItemType.ArrayEnd)
                {
                    FormatValue();
                    MoveNext();
                    if (_current.Type != JsonEnumerableItemType.ArrayEnd)
                        _writer.Write(',');
                }
            }
            _writer.Write(']');
        }

        private void FormatValue()
        {
            if (JsonEnumerableItemType.ObjectBegin == _current.Type || JsonEnumerableItemType.ArrayBegin == _current.Type)
            {
                FormatJson();
                return;
            }
            Formatter.FormatValue(_current.Value, _writer);
        }

        private void MoveNext()
        {
            if (_enumerator.MoveNext())
            {
                _current = _enumerator.Current;
                return;
            }

            throw new Exception();
        }
    }
}
