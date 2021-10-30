using System;

namespace PinkJson2.Serializers
{
    public interface ICustomSerializable
    {
        IJson Serialize(ISerializer serializer);
    }
}
