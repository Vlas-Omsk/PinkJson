using System;

namespace PinkJson2.Serializers
{
    public interface ICustomDeserializable
    {
        void Deserialize(IDeserializer deserializer, IJson json);
    }
}
