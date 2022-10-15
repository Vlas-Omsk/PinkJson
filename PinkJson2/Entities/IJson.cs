using System;

namespace PinkJson2
{
    public interface IJson
    {
        object Value { get; set; }
        int Count { get; }
        IJson this[string key] { get; set; }
        IJson this[int index] { get; set; }

        int IndexOfKey(string key);
        void SetKey(string key, object value);
        int SetIndex(object value, int index = -1);
        bool TryGetValue(string key, out IJson value);
        bool TryGetValue(int index, out IJson value);
    }
}
