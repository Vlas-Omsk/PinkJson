using PinkJson2.Formatters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace PinkJson2
{
    public abstract class JsonRoot<T> : LinkedList<T>, IDynamicJson, IList<T>, IList where T: IJson
    {
        internal JsonRoot()
        {
        }

        internal JsonRoot(IEnumerable<T> collection) : base(collection)
        {
        }

        internal JsonRoot(params T[] collection) : base(collection)
        {
        }

        #region IJson

        object IJson.Value
        {
            get => this;
            set => throw new NotSupportedForTypeException(GetType());
        }

        public abstract IJson this[string key] { get; set; }

        public IJson this[int index]
        {
            get => NodeAt(index).Value;
            set => NodeAt(index).Value = AsChild(value);
        }

        public int IndexOfKey(string key)
        {
            NodeAtOrDefaultInternal(key, out int index);
            return index;
        }

        public abstract void SetKey(string key, object value);

        public abstract int SetIndex(object value, int index = -1);
        #endregion

        #region IDynamicJson

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

        #endregion

        #region IList<T>

        T IList<T>.this[int index]
        {
            get => NodeAt(index).Value;
            set => NodeAt(index).Value = value;
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

        #endregion

        #region IList

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        object IList.this[int index] 
        { 
            get => this[index].Value;
            set => SetIndex(value, index);
        }

        int IList.Add(object value)
        {
            if (!(value is T))
                throw new InvalidObjectTypeException(typeof(T));
            AddLast((T)value);
            return IndexOf((T)value);
        }

        bool IList.Contains(object value)
        {
            return ((IList)this).IndexOf(value) != -1;
        }

        int IList.IndexOf(object value)
        {
            if (value is int)
                return (int)value;
            else if (value is T)
                return IndexOf((T)value);
            else
                throw new InvalidObjectTypeException(typeof(T));
        }

        void IList.Insert(int index, object value)
        {
            if (!(value is T))
                throw new InvalidObjectTypeException(typeof(T));
            var node = NodeAt(index);
            AddAfter(node, (T)value);
        }

        void IList.Remove(object value)
        {
            if (!(value is T))
                throw new InvalidObjectTypeException(typeof(T));
            Remove((T)value);
        }

        #endregion

        protected T AsChild(IJson value)
        {
            if (!(value is T child))
                throw new InvalidObjectTypeException(typeof(T));
            return child;
        }

        public LinkedListNode<T> NodeAt(string key)
        {
            var node = NodeAtOrDefault(key);
            if (node == null)
                throw new KeyNotFoundException(key);
            return node;
        }

        public LinkedListNode<T> NodeAtOrDefault(string key)
        {
            return NodeAtOrDefaultInternal(key, out _);
        }

        protected abstract LinkedListNode<T> NodeAtOrDefaultInternal(string key, out int index);

        public LinkedListNode<T> NodeAt(int index)
        {
            var node = NodeAtOrDefault(index);
            if (node == null)
                throw new IndexOutOfRangeException();
            return node;
        }

        public LinkedListNode<T> NodeAtOrDefault(int index)
        {
            if (index >= Count || index < 0)
                return null;

            var current = First;
            for (var i = 0; i < index; i++)
                current = current.Next;

            return current;
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
    }
}
