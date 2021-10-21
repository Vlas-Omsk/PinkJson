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
            return new JsonMetaObject(parameter, this);
        }

        object IJson.DynamicGetValue(JsonMetaObject jsonMetaObject, string propertyName)
        {
            return jsonMetaObject.GetValue(propertyName);
        }

        object IJson.DynamicSetValue(JsonMetaObject jsonMetaObject, string propertyName, object value)
        {
            return jsonMetaObject.SetValue(propertyName, value);
        }
    }
}
