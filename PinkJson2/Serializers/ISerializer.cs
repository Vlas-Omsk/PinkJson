using System;

namespace PinkJson2.Serializers
{
    public interface ISerializer
    {
        bool IgnoreRootCustomSerializer { get; set; }
        bool IgnoreCustomSerializers { get; set; }

        IJson Serialize(object obj);
    }
}
