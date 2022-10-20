using System;
using System.Collections;
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
        private IndentStyle _indentStyle = IndentStyle.Space;
        private int _indentSize = 2;
        private string _indent;

        public IndentStyle IndentStyle
        {
            get => _indentStyle;
            set
            {
                _indentStyle = value;
                _indent = null;
            }
        }
        public int IndentSize
        {
            get => _indentSize;
            set
            {
                _indentSize = value;
                _indent = null;
            }
        }

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
            _writer.Write(Formatter.LeftBrace);
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
                        _writer.Write(Formatter.Comma);
                    _writer.WriteLine();
                }
                _depth--;
                if (_depth > 0)
                    AddIndent();
            }
            _writer.Write(Formatter.RightBrace);
        }

        private void FormatKeyValue()
        {
            _writer.Write(Formatter.Quote);
            ((string)_current.Value).EscapeString(_writer);
            _writer.Write(Formatter.Quote);
            _writer.Write(Formatter.Colon);
            _writer.Write(Formatter.Space);
            MoveNext();
            FormatValue();
        }

        private void FormatArray()
        {
            var isRoot = _depth == 0;

            _writer.Write(Formatter.LeftBracket);
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
                    {
                        _writer.Write(Formatter.Comma);
                        _writer.Write(Formatter.Space);
                    }
                }
                if (isRoot)
                {
                    _depth--;
                    _writer.WriteLine();
                }
            }
            _writer.Write(Formatter.RightBracket);
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
            GetIndent().Repeat(_depth, _writer);
        }

        public string GetIndent()
        {
            if (_indent != null)
                return _indent;

            switch (IndentStyle)
            {
                case IndentStyle.Space:
                    return _indent = Formatter.Space.Repeat(IndentSize);
                case IndentStyle.Tab:
                    return _indent = Formatter.Tab.Repeat(IndentSize);
                default:
                    throw new Exception();
            }
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
