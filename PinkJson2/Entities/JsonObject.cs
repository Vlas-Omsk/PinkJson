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

        public override JsonKeyValue this[string key]
        {
            get
            {
                foreach (var item in this)
                    if (item.Key == key)
                        return item;
                throw new KeyNotFoundException(key);
            }
            set
            {
                var current = First;
                for (var i = 0; i < Count; i++)
                {
                    if (current.Value.Key == key)
                    {
                        current.Value = value;
                        return;
                    }
                    current = current.Next;
                }
                throw new KeyNotFoundException(key);
            }
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
