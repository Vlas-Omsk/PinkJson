using System;

namespace PinkJson2
{
    public readonly struct JsonEnumerableItem
    {
        public JsonEnumerableItemType Type { get; }
        public object Value { get; }

        public JsonEnumerableItem(JsonEnumerableItemType type, object value)
        {
            Type = type;
            Value = value;
        }
    }
}
