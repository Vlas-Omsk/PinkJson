using System;
using System.IO;

namespace PinkJson2.Formatters
{
    public sealed class PrettyFormatter : IFormatter
    {
        public IndentStyle IndentStyle { get; set; } = IndentStyle.Space;
        public int IndentSize { get; set; } = 2;

        private StreamWriter _stream;
        private int _depth = 0;
        private static readonly string _newLine = Environment.NewLine;

        public void Format(IJson json, StreamWriter stream)
        {
            _stream = stream;

            FormatJson(json);
        }

        private void FormatJson(IJson json)
        {
            if (json is JsonObject)
                FormatObject(json as JsonObject);
            else if (json is JsonArray)
                FormatArray(json as JsonArray);
            else if (json is JsonKeyValue)
                FormatKeyValue(json as JsonKeyValue);
            else if (json is JsonArrayValue)
                FormatValue(json.Value);
        }

        private void FormatObject(JsonObject json)
        {
            _stream.Write('{');
            if (json.Count > 0)
            {
                _depth++;
                _stream.Write(_newLine);
                json.ForEach((item, i) =>
                {
                    AddIndent();
                    FormatKeyValue(item);
                    if (i < json.Count - 1)
                        _stream.Write(',');
                    _stream.Write(_newLine);
                });
                _depth--;
                if (_depth > 0)
                    AddIndent();
            }
            _stream.Write('}');
        }

        private void FormatKeyValue(JsonKeyValue json)
        {
            _stream.Write($"\"{json.Key.EscapeString()}\"");
            _stream.Write(": ");
            FormatValue(json.Value);
        }

        private void FormatArray(JsonArray json)
        {
            _stream.Write('[');
            var isRoot = _depth == 0;
            if (json.Count > 0)
            {
                if (isRoot)
                {
                    _depth++;
                    _stream.Write(_newLine);
                    AddIndent();
                }
                json.ForEach((item, i) =>
                {
                    FormatValue(item);
                    if (i < json.Count - 1)
                        _stream.Write(", ");
                });
                if (isRoot)
                {
                    _depth--;
                    _stream.Write(_newLine);
                }
            }
            _stream.Write(']');
        }

        private void FormatValue(object value)
        {
            if (value is IJson json)
            {
                FormatJson(json);
                return;
            }
            _stream.Write(Formatter.FormatValue(value));
        }

        private void AddIndent()
        {
            var str = IndentStyle == IndentStyle.Space ? 
                ' '.Repeat(IndentSize) : 
                '\t'.Repeat(IndentSize);
            _stream.Write(str.Repeat(_depth));
        }
    }
}
