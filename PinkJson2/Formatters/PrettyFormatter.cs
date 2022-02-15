using System;
using System.Text;

namespace PinkJson2.Formatters
{
    public sealed class PrettyFormatter : IFormatter
    {
        public IndentStyle IndentStyle { get; set; } = IndentStyle.Space;
        public int IndentSize { get; set; } = 2;

        private StringBuilder _stringBuilder = new StringBuilder();
        private int _depth = 0;
        private static readonly string _newLine = Environment.NewLine;

        public string Format(IJson json)
        {
            FormatJson(json);

            var str = _stringBuilder.ToString();
            _stringBuilder.Clear();
            return str;
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
            _stringBuilder.Append('{');
            if (json.Count > 0)
            {
                _depth++;
                _stringBuilder.Append(_newLine);
                json.ForEach((item, i) =>
                {
                    AddIndent();
                    FormatKeyValue(item);
                    if (i < json.Count - 1)
                        _stringBuilder.Append(',');
                    _stringBuilder.Append(_newLine);
                });
                _depth--;
                if (_depth > 0)
                    AddIndent();
            }
            _stringBuilder.Append('}');
        }

        private void FormatKeyValue(JsonKeyValue json)
        {
            _stringBuilder.Append($"\"{json.Key.EscapeString()}\"");
            _stringBuilder.Append(": ");
            FormatValue(json.Value);
        }

        private void FormatArray(JsonArray json)
        {
            _stringBuilder.Append('[');
            var isRoot = _depth == 0;
            if (json.Count > 0)
            {
                if (isRoot)
                {
                    _depth++;
                    _stringBuilder.Append(_newLine);
                    AddIndent();
                }
                json.ForEach((item, i) =>
                {
                    FormatValue(item);
                    if (i < json.Count - 1)
                        _stringBuilder.Append(", ");
                });
                if (isRoot)
                {
                    _depth--;
                    _stringBuilder.Append(_newLine);
                }
            }
            _stringBuilder.Append(']');
        }

        private void FormatValue(object value)
        {
            var str = Formatter.FormatValue(value, FormatJson);
            if (str != null)
                _stringBuilder.Append(str);
        }

        private void AddIndent()
        {
            var str = IndentStyle == IndentStyle.Space ? ' '.Repeat(IndentSize) : '\t'.Repeat(IndentSize);
            _stringBuilder.Append(str.Repeat(_depth));
        }
    }
}
