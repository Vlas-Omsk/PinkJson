using System;

namespace PinkJson2.Serializers
{
    public interface IDeserializer
    {
        bool IgnoreRootCustomDeserializer { get; set; }
        bool IgnoreCustomDeserializers { get; set; }

        T Deserialize<T>(IJson json);
        object Deserialize(IJson json, Type type);
        object DeserializeToObject(IJson json, object rootObj);
    }
}
