using System;
using System.Dynamic;

namespace PinkJson2
{
    public interface IDynamicJson : IJson, IDynamicMetaObjectProvider
    {
        object DynamicGetValue(JsonMetaObject jsonMetaObject, string propertyName);
        object DynamicSetValue(JsonMetaObject jsonMetaObject, string propertyName, object value);
    }
}
