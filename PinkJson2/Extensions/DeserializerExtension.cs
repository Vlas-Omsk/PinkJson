using PinkJson2.Serializers;
using System;

namespace PinkJson2
{
    public static class DeserializerExtension
    {
        public static T Deserialize<T>(this IDeserializer self, IJson json)
        {
            return (T)self.Deserialize(json, typeof(T));
        }

        public static object Deserialize(this IDeserializer self, IJson json, Type type)
        {
            return self.Deserialize(json, type, true);
        }

        public static T Deserialize<T>(this IDeserializer self, IJson json, T instance)
        {
            return (T)self.Deserialize(json, instance, true);
        }

        public static T Deserialize<T>(this IDeserializer self, IJson json, T instance, bool useJsonDeserialize)
        {
            return (T)self.Deserialize(json, instance, useJsonDeserialize);
        }

        public static object Deserialize(this IDeserializer self, IJson json, object instance)
        {
            return self.Deserialize(json, instance, true);
        }

        public static object Deserialize(this IDeserializer self, IJson json, object instance, bool useJsonDeserialize)
        {
            return self.Deserialize(json, instance, useJsonDeserialize);
        }
    }
}
