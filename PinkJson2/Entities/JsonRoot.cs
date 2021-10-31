using PinkJson2.Formatters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace PinkJson2
{
    public abstract class JsonRoot<T> : LinkedList<T>, IDynamicJson, IList<T> where T: IJson
    {
        object IJson.Value { get => this; set => throw new NotSupportedException($"An object of type {GetType()} does not support set Value"); }

        internal JsonRoot()
        {
        }

        internal JsonRoot(IEnumerable<T> collection) : base(collection)
        {
        }

        internal JsonRoot(params T[] collection) : base(collection)
        {
        }

        public object this[object key]
        {
            get => this[key.ToString()];
            set => this[key.ToString()] = value;
        }

        public object this[string key]
        {
            get => ((IJson)this)[key].Value;
            set
            {
                var node = GetNodeByKey(key);
                if (node != null)
                    node.Value.Value = value;
                else
                    AddLast(CreateItemFromKeyValue(key, value));
            }
        }

        public object this[int index]
        {
            get => ((IList<T>)this)[index].Value;
            set => ((IList<T>)this)[index].Value = value;
        }

        IJson IJson.this[object key]
        {
            get => ((IJson)this)[key.ToString()];
            set => ((IJson)this)[key.ToString()] = value;
        }

        IJson IJson.this[string key]
        {
            get
            {
                var node = GetNodeByKey(key);
                if (node == null)
                    throw new KeyNotFoundException(key);
                return node.Value;
            }
            set
            {
                if (!(value is T))
                    throw new InvalidObjectTypeException(typeof(T));
                var node = GetNodeByKey(key);
                if (node != null)
                    node.Value = (T)value;
                else
                    AddLast((T)value);
            }
        }

        IJson IJson.this[int index]
        {
            get => ((IList<T>)this)[index];
            set
            {
                if (!(value is T))
                    throw new InvalidObjectTypeException(typeof(T));
                ((IList<T>)this)[index] = (T)value;
            }
        }

        T IList<T>.this[int index]
        {
            get => NodeAt(index).Value;
            set => NodeAt(index).Value = value;
        }

        protected virtual LinkedListNode<T> GetNodeByKey(string key)
        {
            throw new NotSupportedException($"An object of type {GetType()} does not support a getters and setters by key '{key}'");
        }

        protected virtual T CreateItemFromKeyValue(string key, object value)
        {
            throw new NotSupportedException($"An object of type {GetType()} does not support a getters and setters by key '{key}'");
        }

        public int IndexOf(T item)
        {
            var current = First;
            for (var i = 0; i < Count; i++)
            {
                if (current.Value.Equals(item))
                    return i;
                current = current.Next;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            AddAfter(NodeAt(index), item);
        }

        public void RemoveAt(int index)
        {
            Remove(NodeAt(index));
        }

        public LinkedListNode<T> NodeAt(int index)
        {
            if (index >= Count || index < 0)
                throw new IndexOutOfRangeException();

            var current = First;
            for (var i = 0; i < index; i++)
                current = current.Next;

            return current;
        }

        public int IndexOfKey(object key)
        {
            return IndexOfKey(key.ToString());
        }

        public virtual int IndexOfKey(string key)
        {
            throw new NotSupportedException($"An object of type {GetType()} does not support a search by key '{key}'");
        }

        public void ForEach(Action<T> action)
        {
            var current = First;
            for (var i = 0; i < Count; i++)
            {
                action?.Invoke(current.Value);
                current = current.Next;
            }
        }

        public void ForEach(Action<T, int> action)
        {
            var current = First;
            for (var i = 0; i < Count; i++)
            {
                action?.Invoke(current.Value, i);
                current = current.Next;
            }
        }

        public override string ToString()
        {
            return new MinifiedFormatter().Format(this);
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new JsonMetaObject(parameter, this);
        }

        object IDynamicJson.DynamicGetValue(JsonMetaObject jsonMetaObject, string propertyName)
        {
            return jsonMetaObject.GetValue(propertyName);
        }

        object IDynamicJson.DynamicSetValue(JsonMetaObject jsonMetaObject, string propertyName, object value)
        {
            return jsonMetaObject.SetValue(propertyName, value);
        }
    }
}
