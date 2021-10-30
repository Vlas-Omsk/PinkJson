using PinkJson2.Formatters;
using PinkJson2.Serializers;
using System;
using System.IO;
using System.Text;

namespace PinkJson2
{
    public static class Json
    {
        public static IJson Parse(StreamReader stream)
        {
            using (var lexer = new JsonLexer(stream))
                return JsonParser.Parse(lexer);
        }

        public static IJson Parse(Stream stream, Encoding encoding)
        {
            using (var lexer = new JsonLexer(stream, encoding))
                return JsonParser.Parse(lexer);
        }

        public static IJson Parse(string source)
        {
            using (var lexer = new JsonLexer(source))
                return JsonParser.Parse(lexer);
        }

        public static T Get<T>(this IJson json)
        {
            if (json.Value is null)
                return default;
            try
            {
                return (T)Convert.ChangeType(json.Value, typeof(T));
            }
            catch
            {
                return (T)json.Value;
            }
        }

        public static JsonArray AsArray(this IJson json)
        {
            return json.Value as JsonArray;
        }

        public static JsonObject AsJson(this IJson json)
        {
            return json.Value as JsonObject;
        }

        public static bool ContainsKey(this IJson json, string key)
        {
            return json.IndexOfKey(key) != -1;
        }

        public static string ToString(this IJson json, IFormatter formatter)
        {
            return formatter.Format(json);
        }
    }
}
