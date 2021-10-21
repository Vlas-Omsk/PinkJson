using System;

namespace PinkJson2.Serializers
{
    public interface IDeserializer
    {
        object Deserialize(IJson json);
    }
}
