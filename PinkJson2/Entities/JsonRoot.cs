using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace PinkJson2
{
    public abstract class JsonRoot<T> : LinkedList<T>, IJson, IList<T> where T: IJson
    {
        object IJson.Value => this;

        internal JsonRoot()
        {
        }

        internal JsonRoot(IEnumerable<T> collection) : base(collection)
        {
        }

        internal JsonRoot(params T[] collection) : base(collection)
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
            return new MinifiedFormatter().Format(this);
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaObject(parameter, this);
        }

        private object GetValue(string propertyName)
        {
            object member;
            if ((member = GetType().GetProperty(propertyName)) != null)
                return ((PropertyInfo)member).GetValue(this);
            if ((member = GetType().GetField(propertyName)) != null)
                return ((FieldInfo)member).GetValue(this);

            if (propertyName[0] == '_' && propertyName.Length > 1 && propertyName[1] != '_')
            {
                var index = int.Parse(propertyName.Substring(1));
                return this[index];
            }
            return this[propertyName];
        }

        private object SetValue(string propertyName, object value)
        {
            object member;
            if ((member = GetType().GetProperty(propertyName)) != null)
            {
                ((PropertyInfo)member).SetValue(this, value);
                return value;
            }
            if ((member = GetType().GetField(propertyName)) != null)
            {
                ((FieldInfo)member).SetValue(this, value);
                return value;
            }

            if (!(value is T))
                throw new InvalidObjectTypeException(typeof(T));
            if (propertyName[0] == '_' && propertyName.Length > 1 && propertyName[1] != '_')
            {
                var index = int.Parse(propertyName.Substring(1));
                this[index] = (T)value;
                return value;
            }
            this[propertyName] = (T)value;
            return value;
        }

        private class MetaObject : DynamicMetaObject
        {
            private MethodInfo _getValueMethodInfo;
            private MethodInfo _setValueMethodInfo;

            internal MetaObject(Expression parameter, JsonRoot<T> value) : base(parameter, BindingRestrictions.Empty, value)
            {
                _getValueMethodInfo = typeof(JsonRoot<T>).GetMethod("GetValue", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                _setValueMethodInfo = typeof(JsonRoot<T>).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var arguments = new Expression[]
                {
                    Expression.Constant(binder.Name)
                };

                Expression objectExpression = Expression.Call(Expression.Convert(Expression, LimitType), _getValueMethodInfo, arguments);

                return new DynamicMetaObject(objectExpression, BindingRestrictions.GetTypeRestriction(Expression, RuntimeType));
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                var arguments = new Expression[]
                {
                    Expression.Constant(binder.Name),
                    Expression.Constant(value.Value)
                };

                Expression objectExpression = Expression.Call(Expression.Convert(Expression, LimitType), _setValueMethodInfo, arguments);

                return new DynamicMetaObject(objectExpression, BindingRestrictions.GetTypeRestriction(Expression, RuntimeType));
            }
        }
    }
}
