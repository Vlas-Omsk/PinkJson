using System;
using System.Text;

namespace PinkJson2
{
    public class JsonFormatter
    {
        public IndentStyle IndentStyle { get; set; } = IndentStyle.Space;
        public int IndentSize { get; set; } = 2;
        public bool Minify { get; set; }

        private StringBuilder _stringBuilder = new StringBuilder();
        private int _depth = 0;
        private static readonly string _newLine = Environment.NewLine;

        public static JsonFormatter Default => new JsonFormatter();
        public static JsonFormatter Minified => new JsonFormatter() { Minify = true };

        public string Format(IJson json)
        {
            FormatJson(json);

            var str = _stringBuilder.ToString();
            _stringBuilder.Clear();
            return str;
        }

        protected void FormatJson(IJson json)
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

        protected virtual void FormatObject(JsonObject json)
        {
            _stringBuilder.Append('{');
            if (json.Count > 0)
            {
                _depth++;
                if (!Minify)
                    _stringBuilder.Append(_newLine);
                json.ForEach((item, i) =>
                {
                    if (!Minify)
                        AddIndent();
                    FormatKeyValue(item);
                    if (i < json.Count - 1)
                        _stringBuilder.Append(',');
                    if (!Minify)
                        _stringBuilder.Append(_newLine);
                });
                _depth--;
                if (!Minify && _depth > 0)
                    AddIndent();
            }
            _stringBuilder.Append('}');
        }

        protected virtual void FormatKeyValue(JsonKeyValue json)
        {
            _stringBuilder.Append($"\"{json.Key.EscapeString()}\"");
            _stringBuilder.Append(':');
            if (!Minify)
                _stringBuilder.Append(' ');
            FormatValue(json.Value);
        }

        protected virtual void FormatArray(JsonArray json)
        {
            _stringBuilder.Append('[');
            var isRoot = _depth == 0;
            if (json.Count > 0)
            {
                if (isRoot)
                {
                    _depth++;
                    if (!Minify)
                    {
                        _stringBuilder.Append(_newLine);
                        AddIndent();
                    }
                }
                json.ForEach((item, i) =>
                {
                    FormatValue(item);
                    if (i < json.Count - 1) {
                        _stringBuilder.Append(',');
                        if (!Minify)
                            _stringBuilder.Append(' ');
                    }
                });
                if (isRoot)
                {
                    _depth--;
                    if (!Minify)
                        _stringBuilder.Append(_newLine);
                }
            }
            _stringBuilder.Append(']');
        }

        protected virtual void FormatValue(object value)
        {
            string str;

            if (value is null)
                str = "null";
            else if (value is bool)
                str = ((bool)value) ? "true" : "false";
            else if (value is DateTime)
                str = '\"' + ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + '\"';
            else if (value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal)
                str = value.ToString().Replace(',', '.');
            else if (value is IJson)
            {
                FormatJson(value as IJson);
                return;
            }
            else
                str = $"\"{value.ToString().EscapeString()}\"";

            _stringBuilder.Append(str);
        }

        protected void AddIndent()
        {
            var str = IndentStyle == IndentStyle.Space ? ' '.Repeat(IndentSize) : '\t'.Repeat(IndentSize);
            _stringBuilder.Append(str.Repeat(_depth));
        }
    }
}
