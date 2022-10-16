using System;
using System.Collections.Generic;
using System.IO;

namespace PinkJson2.Formatters
{
    public sealed class PrettyFormatter : IFormatter
    {
        private TextWriter _writer;
        private IEnumerator<JsonEnumerableItem> _enumerator;
        private int _depth = 0;
        private JsonEnumerableItem _current;

        public IndentStyle IndentStyle { get; set; } = IndentStyle.Space;
        public int IndentSize { get; set; } = 2;

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
                _depth++;
                _writer.WriteLine();
                MoveNext();
                while (_current.Type != JsonEnumerableItemType.ObjectEnd)
                {
                    AddIndent();
                    FormatKeyValue();
                    MoveNext();
                    if (_current.Type != JsonEnumerableItemType.ObjectEnd)
                        _writer.Write(',');
                    _writer.WriteLine();
                }
                _depth--;
                if (_depth > 0)
                    AddIndent();
            }
            _writer.Write('}');
        }

        private void FormatKeyValue()
        {
            _writer.Write('\"');
            ((string)_current.Value).EscapeString(_writer);
            _writer.Write("\": ");
            MoveNext();
            FormatValue();
        }

        private void FormatArray()
        {
            _writer.Write('[');
            var isRoot = _depth == 0;
            if (_current.Type != JsonEnumerableItemType.ArrayEnd)
            {
                if (isRoot)
                {
                    _depth++;
                    _writer.WriteLine();
                    AddIndent();
                }
                MoveNext();
                while (_current.Type != JsonEnumerableItemType.ArrayEnd)
                {
                    FormatValue();
                    MoveNext();
                    if (_current.Type != JsonEnumerableItemType.ArrayEnd)
                        _writer.Write(", ");
                }
                if (isRoot)
                {
                    _depth--;
                    _writer.WriteLine();
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

        private void AddIndent()
        {
            Formatter.GetIndent(IndentStyle, IndentSize).Repeat(_depth, _writer);
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
