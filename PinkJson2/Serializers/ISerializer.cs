using System;
using System.Collections.Generic;

namespace PinkJson2.Serializers
{
    public interface ISerializer
    {
        IEnumerable<JsonEnumerableItem> Serialize(object instance);
    }
}
