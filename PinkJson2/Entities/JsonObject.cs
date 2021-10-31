using System;
using System.Collections.Generic;

namespace PinkJson2
{
    public sealed class JsonObject : JsonRoot<JsonKeyValue>
    {
        public JsonObject()
        {
        }

        public JsonObject(IEnumerable<JsonKeyValue> collection) : base(collection)
        {
        }

        public JsonObject(params JsonKeyValue[] collection) : base(collection)
        {
        }

        protected override LinkedListNode<JsonKeyValue> GetNodeByKey(string key)
        {
            var index = IndexOfKey(key);
            if (index == -1)
                return null;
            return NodeAt(index);
        }

        protected override JsonKeyValue CreateItemFromKeyValue(string key, object value)
        {
            return new JsonKeyValue(key, value);
        }

        public override int IndexOfKey(string key)
        {
            var current = First;
            for (var i = 0; i < Count; i++)
            {
                if (current.Value.Key == key)
                    return i;
                current = current.Next;
            }
            return -1;
        }
    }
}
