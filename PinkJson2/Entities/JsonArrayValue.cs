using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PinkJson2
{
    [DebuggerDisplay("\\{JsonArrayValue: Value = {Value.ToString(),nq}}")]
    public sealed class JsonArrayValue : JsonChild
    {
        public JsonArrayValue(object value) : base(value)
        {
        }

        public override IEnumerable<JsonEnumerableItem> GetJsonEnumerable()
        {
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
