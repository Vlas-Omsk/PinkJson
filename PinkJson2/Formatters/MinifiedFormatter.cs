using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PinkJson2.Formatters
{
    public sealed class MinifiedFormatter : IFormatter
    {
        private StreamWriter _stream;
        private IEnumerator<JsonEnumerableItem> _enumerator;
        private JsonEnumerableItem _current;

        private void MoveNext()
        {
            if (_enumerator.MoveNext())
            {
                _current = _enumerator.Current;
                return;
            }

            throw new Exception();
        }

        public void Format(IEnumerable<JsonEnumerableItem> json, StreamWriter stream)
        {
            _stream = stream;
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
            _stream.Write('{');
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
                            _stream.Write(',');
                    }
                }
                while (_current.Type != JsonEnumerableItemType.ObjectEnd);
            }
            _stream.Write('}');
        }

        private void FormatKeyValue()
        {
            _stream.Write($"\"{((string)_current.Value).EscapeString()}\"");
            _stream.Write(':');
            MoveNext();
            FormatValue();
        }

        private void FormatArray()
        {
            _stream.Write('[');
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
                            _stream.Write(',');
                    }
                }
                while (_current.Type != JsonEnumerableItemType.ArrayEnd);
            }
            _stream.Write(']');
        }

        private void FormatValue()
        {
            if (new JsonEnumerableItemType[] { JsonEnumerableItemType.ObjectBegin, JsonEnumerableItemType.ArrayBegin }.Contains(_current.Type))
            {
                FormatJson();
                return;
            }
            _stream.Write(Formatter.FormatValue(_current.Value));
        }
    }
}
