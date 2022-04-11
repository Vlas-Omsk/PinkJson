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

        public override IJson this[string key]
        {
            get => NodeAt(key).Value;
            set
            {
                var child = AsChild(value);
                if (child.Key != key)
                    throw new KeyNotMatchException(key, child.Key);
                NodeAt(key).Value = child;
            }
        }

        public override int SetIndex(object value, int index = -1)
        {
            throw new NotSupportedForTypeException(GetType());
        }

        public override void SetKey(string key, object value)
        {
            var node = NodeAtOrDefault(key);
            if (node == null)
                AddLast(new JsonKeyValue(key, value));
            else
                node.Value.Value = value;
        }

        protected override LinkedListNode<JsonKeyValue> NodeAtOrDefaultInternal(string key, out int index)
        {
            var current = First;
            for (index = 0; index < Count; index++)
            {
                if (current.Value.Key == key)
                    return current;
                current = current.Next;
            }
            index = -1;
            return null;
        }

        public LinkedListNode<JsonKeyValue> AddValueAfter(LinkedListNode<JsonKeyValue> node, string key, object value)
        {
            return AddAfter(node, new JsonKeyValue(key, value));
        }

        public LinkedListNode<JsonKeyValue> AddValueBefore(LinkedListNode<JsonKeyValue> node, string key, object value)
        {
            return AddBefore(node, new JsonKeyValue(key, value));
        }

        public LinkedListNode<JsonKeyValue> AddValueFirst(string key, object value)
        {
            return AddFirst(new JsonKeyValue(key, value));
        }

        public LinkedListNode<JsonKeyValue> AddValueLast(string key, object value)
        {
            return AddLast(new JsonKeyValue(key, value));
        }
    }
}
