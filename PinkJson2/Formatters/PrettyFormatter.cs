using System;
using System.Collections.Generic;
using System.IO;

namespace PinkJson2.Formatters
{
    public sealed class PrettyFormatter : IFormatter
    {
        private IndentStyle _indentStyle = IndentStyle.Space;
        private int _indentSize = 2;
        private string _indent;

        private sealed class Formatter : IDisposable
        {
            private readonly PrettyFormatter _formatter;
            private readonly TextWriter _writer;
            private readonly ValueFormatter _valueFormatter;
            private readonly IEnumerator<JsonEnumerableItem> _enumerator;
            private int _depth = 0;
            private JsonEnumerableItem _current;

            public Formatter(PrettyFormatter formatter, TextWriter writer, IEnumerator<JsonEnumerableItem> enumerator)
            {
                _formatter = formatter;
                _writer = writer;
                _valueFormatter = new ValueFormatter(writer);
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
                        throw new Exception();
                }
            }

            private void FormatObject()
            {
                _writer.Write(ValueFormatter.LeftBrace);
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
                            _writer.Write(ValueFormatter.Comma);
                        _writer.WriteLine();
                    }
                    _depth--;
                    if (_depth > 0)
                        AddIndent();
                }
                _writer.Write(ValueFormatter.RightBrace);
            }

            private void FormatKeyValue()
            {
                _writer.Write(ValueFormatter.Quote);
                ((string)_current.Value).EscapeString(_writer);
                _writer.Write(ValueFormatter.Quote);
                _writer.Write(ValueFormatter.Colon);
                _writer.Write(ValueFormatter.Space);
                MoveNext();
                FormatValue();
            }

            private void FormatArray()
            {
                var isRoot = _depth == 0;

                _writer.Write(ValueFormatter.LeftBracket);
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
                            _writer.Write(ValueFormatter.Comma);
                            _writer.Write(ValueFormatter.Space);
                        }
                    }
                    if (isRoot)
                    {
                        _depth--;
                        _writer.WriteLine();
                    }
                }
                _writer.Write(ValueFormatter.RightBracket);
            }

            private void FormatValue()
            {
                if (JsonEnumerableItemType.ObjectBegin == _current.Type || JsonEnumerableItemType.ArrayBegin == _current.Type)
                {
                    FormatJson();
                    return;
                }
                _valueFormatter.FormatValue(_current.Value);
            }

            private void AddIndent()
            {
                _formatter.GetIndent().Repeat(_depth, _writer);
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

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }

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
            using (var formatter = new Formatter(this, writer, json.GetEnumerator()))
                formatter.Format();
        }

        private string GetIndent()
        {
            if (_indent != null)
                return _indent;

            switch (IndentStyle)
            {
                case IndentStyle.Space:
                    return _indent = ValueFormatter.Space.Repeat(IndentSize);
                case IndentStyle.Tab:
                    return _indent = ValueFormatter.Tab.Repeat(IndentSize);
                default:
                    throw new Exception();
            }
        }
    }
}
