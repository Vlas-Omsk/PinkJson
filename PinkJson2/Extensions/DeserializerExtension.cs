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

        public static T Deserialize<T>(this IDeserializer self, IJson json, T instance)
        {
            return (T)self.Deserialize(json, instance);
        }

        public static T Deserialize<T>(this IDeserializer self, IJson json, T instance, bool useJsonDeserialize)
        {
            return (T)self.Deserialize(json, instance, useJsonDeserialize);
        }
    }
}
