using System;

namespace PinkJson2.Serializers
{
    public interface IJsonDeserializable
    {
        void Deserialize(IDeserializer deserializer, IJson json);
    }
}
