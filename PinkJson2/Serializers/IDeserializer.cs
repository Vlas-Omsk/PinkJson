using System;

namespace PinkJson2.Serializers
{
    public interface IDeserializer
    {
        T Deserialize<T>(IJson json);
        object Deserialize(IJson json, Type type);
        object DeserializeToObject(IJson json, object rootObj);
    }
}
