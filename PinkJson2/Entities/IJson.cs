using System;
using System.Dynamic;

namespace PinkJson2
{
    public interface IJson : IDynamicMetaObjectProvider
    {
        object Value { get; }
        IJson this[string key] { get; set; }
        IJson this[int index] { get; set; }

        int IndexOfKey(string key);
    }
}
