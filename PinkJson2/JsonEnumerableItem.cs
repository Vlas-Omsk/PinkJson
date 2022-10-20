using System;

namespace PinkJson2
{
    public readonly struct JsonEnumerableItem
    {
        public JsonEnumerableItem(JsonEnumerableItemType type, object value)
        {
            Type = type;
            Value = value;
        }

        public JsonEnumerableItemType Type { get; }
        public object Value { get; }

        public static JsonEnumerableItem Null => new JsonEnumerableItem(JsonEnumerableItemType.Value, null);
    }
}
