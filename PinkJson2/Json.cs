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
            var lexer = new JsonLexer(stream);
            return JsonParser.Parse(lexer);
        }

        public static IJson Parse(Stream stream, Encoding encoding)
        {
            var lexer = new JsonLexer(stream, encoding);
            return JsonParser.Parse(lexer);
        }

        public static IJson Parse(string source)
        {
            var lexer = new JsonLexer(source);
            return JsonParser.Parse(lexer);
        }

        public static IJson Serialize(this object instance)
        {
            var serializer = new ObjectSerializer();
            return Serialize(instance, serializer);
        }

        public static IJson Serialize(this object instance, ObjectSerializerOptions options)
        {
            var serializer = new ObjectSerializer(options);
            return Serialize(instance, serializer);
        }

        public static IJson Serialize(this object instance, ISerializer serializer)
        {
            return serializer.Serialize(instance);
        }

        public static T Deserialize<T>(this IJson json)
        {
            var deserializer = new ObjectDeserializer();
            return Deserialize<T>(json, deserializer);
        }

        public static T Deserialize<T>(this IJson json, ObjectSerializerOptions options)
        {
            var deserializer = new ObjectDeserializer(options);
            return Deserialize<T>(json, deserializer);
        }

        public static T Deserialize<T>(this IJson json, IDeserializer deserializer)
        {
            if (!(json.Value is IJson))
                throw new InvalidObjectTypeException(typeof(IJson));
            return deserializer.Deserialize<T>((IJson)json.Value);
        }

        public static object Deserialize(this IJson json, Type type)
        {
            var deserializer = new ObjectDeserializer();
            return Deserialize((IJson)json.Value, type, deserializer);
        }

        public static object Deserialize(this IJson json, Type type, ObjectSerializerOptions options)
        {
            var deserializer = new ObjectDeserializer(options);
            return Deserialize((IJson)json.Value, type, deserializer);
        }

        public static object Deserialize(this IJson json, Type type, IDeserializer deserializer)
        {
            if (!(json.Value is IJson))
                throw new InvalidObjectTypeException(typeof(IJson));
            return deserializer.Deserialize((IJson)json.Value, type);
        }

        public static T Get<T>(this IJson json)
        {
            var value = json.Value;
            if (value is null)
                return default;

            return (T)TypeConverter.ChangeType(value, typeof(T));
        }

        public static JsonArray AsArray(this IJson json)
        {
            return json.Value as JsonArray;
        }

        public static JsonObject AsObject(this IJson json)
        {
            return json.Value as JsonObject;
        }

        public static IJson AsJson(this IJson json)
        {
            return json.Value as IJson;
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
