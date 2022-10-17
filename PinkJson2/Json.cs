﻿using PinkJson2.Formatters;
using PinkJson2.Serializers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace PinkJson2
{
    public static class Json
    {
        public static IEnumerable<JsonEnumerableItem> Parse(StreamReader stream)
        {
            return new JsonParser(new JsonLexer(stream));
        }

        public static IEnumerable<JsonEnumerableItem> Parse(Stream stream)
        {
            return new JsonParser(new JsonLexer(stream));
        }

        public static IEnumerable<JsonEnumerableItem> Parse(Stream stream, Encoding encoding)
        {
            return new JsonParser(new JsonLexer(stream, encoding));
        }

        public static IEnumerable<JsonEnumerableItem> Parse(string source)
        {
            return new JsonParser(new JsonLexer(source));
        }

        public static IEnumerable<JsonEnumerableItem> Serialize(this object instance)
        {
            var serializer = new ObjectSerializer();
            return Serialize(instance, serializer);
        }

        public static IEnumerable<JsonEnumerableItem> Serialize(this object instance, ObjectSerializerOptions options)
        {
            var serializer = new ObjectSerializer(options);
            return Serialize(instance, serializer);
        }

        public static IEnumerable<JsonEnumerableItem> Serialize(this object instance, ISerializer serializer)
        {
            return serializer.Serialize(instance);
        }

        public static T Deserialize<T>(this IJson json)
        {
            var deserializer = new ObjectDeserializerOld();
            return Deserialize<T>(json, deserializer);
        }

        public static T Deserialize<T>(this IJson json, ObjectSerializerOptions options)
        {
            var deserializer = new ObjectDeserializerOld(options);
            return Deserialize<T>(json, deserializer);
        }

        public static T Deserialize<T>(this IJson json, IDeserializer deserializer)
        {
            return deserializer.Deserialize<T>(json);
        }

        public static object Deserialize(this IJson json, Type type)
        {
            var deserializer = new ObjectDeserializerOld();
            return Deserialize(json, type, deserializer);
        }

        public static object Deserialize(this IJson json, Type type, ObjectSerializerOptions options)
        {
            var deserializer = new ObjectDeserializerOld(options);
            return Deserialize(json, type, deserializer);
        }

        public static object Deserialize(this IJson json, Type type, IDeserializer deserializer)
        {
            return deserializer.Deserialize(json, type);
        }

        public static T Get<T>(this IJson json)
        {
            return Get<T>(json, TypeConverter.Default);
        }

        public static T Get<T>(this IJson json, TypeConverter typeConverter)
        {
            return (T)typeConverter.ChangeType(json.Value, typeof(T));
        }

        public static T Get<T>(this JsonEnumerableItem item)
        {
            return Get<T>(item, TypeConverter.Default);
        }

        public static T Get<T>(this JsonEnumerableItem item, TypeConverter typeConverter)
        {
            return (T)typeConverter.ChangeType(item.Value, typeof(T));
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

        public static string ToString(this IJson self, IFormatter formatter)
        {
            return ToJsonString(self.ToJsonEnumerable(), formatter);
        }

        public static string ToJsonString(this IEnumerable<JsonEnumerableItem> self)
        {
            return ToJsonString(self, new MinifiedFormatter());
        }

        public static string ToJsonString(this IEnumerable<JsonEnumerableItem> self, IFormatter formatter)
        {
            using (var writer = new StringWriter())
            {
                formatter.Format(self, writer);

                return writer.ToString();
            }
        }

        public static void ToStream(this IJson self, TextWriter writer)
        {
            ToStream(self.ToJsonEnumerable(), writer);
        }

        public static void ToStream(this IEnumerable<JsonEnumerableItem> self, TextWriter writer)
        {
            ToStream(self, new MinifiedFormatter(), writer);
        }

        public static void ToStream(this IJson self, IFormatter formatter, TextWriter writer)
        {
            ToStream(self.ToJsonEnumerable(), formatter, writer);
        }

        public static void ToStream(this IEnumerable<JsonEnumerableItem> self, IFormatter formatter, TextWriter writer)
        {
            formatter.Format(self, writer);
        }

        public static IJson ToJson(this IEnumerable<JsonEnumerableItem> self)
        {
            var converter = new JsonEnumerableToJsonConverter();

            return converter.Convert(self);
        }

        public static IEnumerable<JsonEnumerableItem> ToJsonEnumerable(this IJson self)
        {
            var converter = new JsonToJsonEnumerableConverter(self);

            return converter;
        }
    }
}
