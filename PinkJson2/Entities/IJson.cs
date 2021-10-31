using System;

namespace PinkJson2
{
    public interface IJson
    {
        object Value { get; set; }
        IJson this[object key] { get; set; }
        IJson this[string key] { get; set; }
        IJson this[int index] { get; set; }

        int IndexOfKey(object key);
        int IndexOfKey(string key);
    }
}
