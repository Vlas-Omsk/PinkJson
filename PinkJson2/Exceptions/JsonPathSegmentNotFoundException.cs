using System;

namespace PinkJson2.Exceptions
{
    public class JsonPathSegmentNotFoundException : PinkJsonException
    {
        public JsonPathSegmentNotFoundException(IJsonPathSegment segment, JsonPath path) : base($"Segment {segment} in {path} not found in current enumerable")
        {
        }
    }
}
