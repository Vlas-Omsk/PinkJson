using PinkJson2.Formatters;
using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace PinkJson2
{
    public abstract class JsonChild : IDynamicJson
    {
        public object Value { get; set; }

        internal JsonChild(object value)
        {
            Value = value;
        }

        #region IJson

        public int Count => ValueAsJson().Count;

        public IJson this[string key]
        {
            get => ValueAsJson()[key];
            set => ValueAsJson()[key] = value;
        }

        public IJson this[int index]
        {
            get => ValueAsJson()[index];
            set => ValueAsJson()[index] = value;
        }

        public int IndexOfKey(string key)
        {
            return ValueAsJson().IndexOfKey(key);
        }

        public void SetKey(string key, object value)
        {
            ValueAsJson().SetKey(key, value);
        }

        public int SetIndex(object value, int index = -1)
        {
            return ValueAsJson().SetIndex(value, index);
        }

        public bool TryGetValue(string key, out IJson value)
        {
            return ValueAsJson().TryGetValue(key, out value);
        }

        public bool TryGetValue(int index, out IJson value)
        {
            return ValueAsJson().TryGetValue(index, out value);
        }

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

        private IJson ValueAsJson()
        {
            if (!(Value is IJson json))
                throw new InvalidObjectTypeException(typeof(IJson));
            return json;
        }

        public override string ToString()
        {
            return new MinifiedFormatter().FormatToString(this);
        }
    }
}
