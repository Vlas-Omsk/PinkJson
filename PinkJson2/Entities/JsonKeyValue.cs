using System;
using System.Diagnostics;

namespace PinkJson2
{
    [DebuggerDisplay("\\{JsonKeyValue: Key = {Key}, Value = {Value?.ToString(),nq}}")]
    public sealed class JsonKeyValue : JsonChild
    {
        public JsonKeyValue(string key, object value) : base(value)
        {
            Key = key;
        }

        public string Key { get; set; }
    }
}
