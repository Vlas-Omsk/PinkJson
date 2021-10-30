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

        public static IJson Serialize(this object obj)
        {
            return Serialize(obj, new ObjectConverter());
        }

        public static IJson Serialize(this object obj, ISerializer serializer)
        {
            return serializer.Serialize(obj);
        }

        public static T Deserialize<T>(this IJson json)
        {
            return Deserialize<T>(json, new ObjectConverter());
        }

        public static T Deserialize<T>(this IJson json, IDeserializer deserializer)
        {
            if (!(json.Value is IJson))
                throw new InvalidObjectTypeException(typeof(IJson));
            return deserializer.Deserialize<T>((IJson)json.Value);
        }

        public static object Deserialize(this IJson json, Type type)
        {
            return Deserialize(json, type, new ObjectConverter());
        }

        public static object Deserialize(this IJson json, Type type, IDeserializer deserializer)
        {
            if (!(json.Value is IJson))
                throw new InvalidObjectTypeException(typeof(IJson));
            return deserializer.Deserialize((IJson)json.Value, type);
        }

        public static object DeserializeToObject(this IJson json, object rootObj)
        {
            return DeserializeToObject(json, rootObj, new ObjectConverter());
        }

        public static object DeserializeToObject(this IJson json, object rootObj, IDeserializer deserializer)
        {
            if (!(json.Value is IJson))
                throw new InvalidObjectTypeException(typeof(IJson));
            return deserializer.DeserializeToObject((IJson)json.Value, rootObj);
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
