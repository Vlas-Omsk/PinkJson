using System;

namespace PinkJson2.Serializers
{
    public interface ISerializerOld
    {
        IJson Serialize(object instance);
        IJson Serialize(object instance, bool useJsonSerialize);
    }
}
