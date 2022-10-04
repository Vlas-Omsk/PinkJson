using System;
using System.IO;

namespace PinkJson2.Formatters
{
    public sealed class MinifiedFormatter : IFormatter
    {
        private StreamWriter _stream;

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
                json.ForEach((item, i) =>
                {
                    FormatKeyValue(item);
                    if (i < json.Count - 1)
                        _stream.Write(',');
                });
            }
            _stream.Write('}');
        }

        private void FormatKeyValue(JsonKeyValue json)
        {
            _stream.Write($"\"{json.Key.EscapeString()}\"");
            _stream.Write(':');
            FormatValue(json.Value);
        }

        private void FormatArray(JsonArray json)
        {
            _stream.Write('[');
            if (json.Count > 0)
            {
                json.ForEach((item, i) =>
                {
                    FormatValue(item);
                    if (i < json.Count - 1)
                        _stream.Write(',');
                });
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
    }
}
