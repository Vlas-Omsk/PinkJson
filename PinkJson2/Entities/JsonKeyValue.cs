using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PinkJson2
{
    [DebuggerDisplay("\\{JsonKeyValue: Key = {Key}, Value = {Value.ToString(),nq}}")]
    public sealed class JsonKeyValue : JsonChild
    {
        public string Key { get; set; }

        public JsonKeyValue(string key, object value) : base(value)
        {
            Key = key;
        }

        public override IEnumerable<JsonEnumerableItem> GetJsonEnumerable()
        {
            yield return new JsonEnumerableItem(JsonEnumerableItemType.Key, Key);

            if (Value is IJson json)
            {
                foreach (var item in json.GetJsonEnumerable())
                    yield return item;
            }
            else
            {
                yield return new JsonEnumerableItem(JsonEnumerableItemType.Value, Value);
            }
        }
    }
}
