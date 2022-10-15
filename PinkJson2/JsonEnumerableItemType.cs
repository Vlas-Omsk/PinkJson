using System;

namespace PinkJson2
{
    public enum JsonEnumerableItemType
    {
        Invalid,
        ObjectBegin,
        ObjectEnd,
        ArrayBegin,
        ArrayEnd,
        Key,
        Value
    }
}
