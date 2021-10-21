using System;

namespace PinkJson2
{
    public sealed class JsonArrayValue : JsonChild
    {
        public JsonArrayValue(object value)
        {
            Value = value;
        }
    }
}
