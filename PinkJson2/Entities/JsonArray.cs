using System;
using System.Collections.Generic;

namespace PinkJson2
{
    public sealed class JsonArray : JsonRoot<JsonArrayValue>
    {
        public JsonArray()
        {
        }

        public JsonArray(IEnumerable<JsonArrayValue> collection) : base(collection)
        {
        }

        public JsonArray(params JsonArrayValue[] collection) : base(collection)
        {
        }
    }
}
