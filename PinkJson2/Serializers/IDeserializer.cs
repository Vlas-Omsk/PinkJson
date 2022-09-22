using System;

namespace PinkJson2.Serializers
{
    public interface IDeserializer
    {
        object Deserialize(IJson json, Type type);
        object Deserialize(IJson json, object instance);
        object Deserialize(IJson json, object instance, bool useJsonDeserialize);
    }
}
