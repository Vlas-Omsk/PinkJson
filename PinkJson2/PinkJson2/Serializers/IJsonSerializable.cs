using System;

namespace PinkJson2.Serializers
{
    public interface IJsonSerializable
    {
        IJson Serialize(ISerializer serializer);
    }
}
