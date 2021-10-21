using System;
using System.Dynamic;

namespace PinkJson2
{
    public interface IJson : IDynamicMetaObjectProvider
    {
        object Value { get; set; }
        IJson this[string key] { get; set; }
        IJson this[int index] { get; set; }

        int IndexOfKey(string key);
        object DynamicGetValue(JsonMetaObject jsonMetaObject, string propertyName);
        object DynamicSetValue(JsonMetaObject jsonMetaObject, string propertyName, object value);
    }
}
