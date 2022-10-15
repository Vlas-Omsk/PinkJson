using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PinkJson2
{
    [DebuggerDisplay("\\{JsonArray: Count = {Count}, Value = {ToString(),nq}}")]
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

        public override IJson this[string key]
        {
            get => throw new NotSupportedForTypeException(GetType());
            set => throw new NotSupportedForTypeException(GetType());
        }

        public override int SetIndex(object value, int index = -1)
        {
            var node = NodeAtOrDefault(index);
            if (node == null)
            {
                AddLast(new JsonArrayValue(value));
                return Count - 1;
            }
            else
            {
                node.Value.Value = value;
                return index;
            }
        }

        public override void SetKey(string key, object value)
        {
            throw new NotSupportedForTypeException(GetType());
        }

        protected override LinkedListNode<JsonArrayValue> NodeAtOrDefaultInternal(string key, out int index)
        {
            throw new NotSupportedForTypeException(GetType());
        }

        public LinkedListNode<JsonArrayValue> AddValueAfter(LinkedListNode<JsonArrayValue> node, object value)
        {
            return AddAfter(node, new JsonArrayValue(value));
        }

        public LinkedListNode<JsonArrayValue> AddValueBefore(LinkedListNode<JsonArrayValue> node, object value)
        {
            return AddBefore(node, new JsonArrayValue(value));
        }

        public LinkedListNode<JsonArrayValue> AddValueFirst(object value)
        {
            return AddFirst(new JsonArrayValue(value));
        }

        public LinkedListNode<JsonArrayValue> AddValueLast(object value)
        {
            return AddLast(new JsonArrayValue(value));
        }
    }
}
