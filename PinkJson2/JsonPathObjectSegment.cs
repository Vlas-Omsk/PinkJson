using System;

namespace PinkJson2
{
    public sealed class JsonPathObjectSegment : IJsonPathSegment
    {
        public JsonPathObjectSegment(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
