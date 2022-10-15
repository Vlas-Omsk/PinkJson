using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Formatters
{
    public sealed class MinifiedFormatter : IFormatter
    {
        private ITextWriter _writer;
        private IEnumerator<JsonEnumerableItem> _enumerator;
        private JsonEnumerableItem _current;

        public void Format(IEnumerable<JsonEnumerableItem> json, ITextWriter writer)
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
                do
                {
                    if (_current.Type != JsonEnumerableItemType.ObjectEnd)
                    {
                        FormatKeyValue();
                        MoveNext();
                        if (_current.Type != JsonEnumerableItemType.ObjectEnd)
                            _writer.Write(',');
                    }
                }
                while (_current.Type != JsonEnumerableItemType.ObjectEnd);
            }
            _writer.Write('}');
        }

        private void FormatKeyValue()
        {
            _writer.Write($"\"{((string)_current.Value).EscapeString()}\":");
            MoveNext();
            FormatValue();
        }

        private void FormatArray()
        {
            _writer.Write('[');
            if (_current.Type != JsonEnumerableItemType.ArrayEnd)
            {
                MoveNext();
                do
                {
                    if (_current.Type != JsonEnumerableItemType.ArrayEnd)
                    {
                        FormatValue();
                        MoveNext();
                        if (_current.Type != JsonEnumerableItemType.ArrayEnd)
                            _writer.Write(',');
                    }
                }
                while (_current.Type != JsonEnumerableItemType.ArrayEnd);
            }
            _writer.Write(']');
        }

        private void FormatValue()
        {
            if (new JsonEnumerableItemType[] { JsonEnumerableItemType.ObjectBegin, JsonEnumerableItemType.ArrayBegin }.Contains(_current.Type))
            {
                FormatJson();
                return;
            }
            _writer.Write(Formatter.FormatValue(_current.Value));
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
