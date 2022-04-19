using System;

namespace PinkJson2.Serializers
{
    public interface ISerializer
    {
        IJson Serialize(object instance);
        IJson Serialize(object instance, bool useJsonSerialize);
    }
}
