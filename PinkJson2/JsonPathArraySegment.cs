using System;

namespace PinkJson2
{
    public sealed class JsonPathArraySegment : IJsonPathSegment
    {
        public JsonPathArraySegment(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
