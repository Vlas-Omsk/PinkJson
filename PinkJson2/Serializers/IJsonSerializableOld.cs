using System;

namespace PinkJson2.Serializers
{
    public interface IJsonSerializableOld
    {
        IJson Serialize(ISerializerOld serializer);
    }
}
