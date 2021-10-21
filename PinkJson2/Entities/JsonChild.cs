using System;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace PinkJson2
{
    public abstract class JsonChild : IJson
    {
        public object Value { get; set; }

        internal JsonChild()
        {
        }

        public IJson this[string key]
        {
            get
            {
                if (!(Value is IJson))
                    throw new InvalidObjectTypeException(typeof(IJson));
                return (Value as IJson)[key];
            }
            set
            {
                if (!(Value is IJson))
                    throw new InvalidObjectTypeException(typeof(IJson));
                (Value as IJson)[key] = value;
            }
        }

        public IJson this[int index]
        {
            get
            {
                if (!(Value is IJson))
                    throw new InvalidObjectTypeException(typeof(IJson));
                return (Value as IJson)[index];
            }
            set
            {
                if (!(Value is IJson))
                    throw new InvalidObjectTypeException(typeof(IJson));
                (Value as IJson)[index] = value;
            }
        }

        public int IndexOfKey(string key)
        {
            if (!(Value is IJson))
                throw new InvalidObjectTypeException(typeof(IJson));
            return (Value as IJson).IndexOfKey(key);
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

            if (!(value is IJson))
                throw new InvalidObjectTypeException(typeof(IJson));
            if (propertyName[0] == '_' && propertyName.Length > 1 && propertyName[1] != '_')
            {
                var index = int.Parse(propertyName.Substring(1));
                this[index] = (IJson)value;
                return value;
            }
            this[propertyName] = (IJson)value;
            return value;
        }

        private class MetaObject : DynamicMetaObject
        {
            private MethodInfo _getValueMethodInfo;
            private MethodInfo _setValueMethodInfo;

            internal MetaObject(Expression parameter, JsonChild value) : base(parameter, BindingRestrictions.Empty, value)
            {
                _getValueMethodInfo = typeof(JsonChild).GetMethod("GetValue", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                _setValueMethodInfo = typeof(JsonChild).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
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
