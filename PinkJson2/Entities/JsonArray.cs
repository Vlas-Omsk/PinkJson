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

        public LinkedListNode<JsonArrayValue> AddValueAfter(LinkedListNode<JsonArrayValue> node, object value)
        {
            return base.AddAfter(node, new JsonArrayValue(value));
        }

        public LinkedListNode<JsonArrayValue> AddValueBefore(LinkedListNode<JsonArrayValue> node, object value)
        {
            return base.AddBefore(node, new JsonArrayValue(value));
        }

        public LinkedListNode<JsonArrayValue> AddValueFirst(object value)
        {
            return base.AddFirst(new JsonArrayValue(value));
        }

        public LinkedListNode<JsonArrayValue> AddValueLast(object value)
        {
            return base.AddLast(new JsonArrayValue(value));
        }
    }
}
