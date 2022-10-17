using System;
using System.Collections.Generic;

namespace PinkJson2.Serializers
{
    public interface IJsonSerializable
    {
        IEnumerable<JsonEnumerableItem> Serialize(ISerializer serializer);
    }
}
