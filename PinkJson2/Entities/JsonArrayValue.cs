using System;
using System.Diagnostics;

namespace PinkJson2
{
    [DebuggerDisplay("\\{JsonArrayValue: Value = {Value?.ToString(),nq}}")]
    public sealed class JsonArrayValue : JsonChild
    {
        public JsonArrayValue(object value) : base(value)
        {
        }
    }
}
