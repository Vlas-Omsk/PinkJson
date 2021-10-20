using System;
using System.Collections.Generic;

namespace PinkJson2
{
    public abstract class JsonRoot<T> : LinkedList<T>, IJson, IList<T> where T: IJson
    {
        object IJson.Value => this;

        internal JsonRoot()
        {
        }

        public virtual T this[string key]
        {
            get => throw new NotSupportedException($"An object of type {GetType()} does not support a search by key '{key}'");
            set => throw new NotSupportedException($"An object of type {GetType()} does not support a search by key '{key}'");
        }

        public T this[int index]
        {
            get => NodeAt(index).Value;
            set => NodeAt(index).Value = value;
        }

        IJson IJson.this[string key]
        {
            get => this[key];
            set
            {
                if (!(value is T))
                    throw new InvalidObjectTypeException(typeof(T));
                this[key] = (T)value;
            }
        }

        IJson IJson.this[int index]
        {
            get => this[index];
            set
            {
                if (!(value is T))
                    throw new InvalidObjectTypeException(typeof(T));
                this[index] = (T)value;
            }
        }

        T IList<T>.this[int index]
        {
            get => this[index];
            set
            {
                if (!(value is T))
                    throw new InvalidObjectTypeException(typeof(T));
                this[index] = value;
            }
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
            return JsonFormatter.Minified.Format(this);
        }
    }
}
