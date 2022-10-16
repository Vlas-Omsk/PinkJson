using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2
{
    public sealed class JsonPath : LinkedList<IJsonPathSegment>
    {
        private const string _rootObjectName = "root";

        public JsonPath() : base()
        {
        }

        public JsonPath(IList<IJsonPathSegment> segments) : base(segments)
        {
        }

        public override string ToString()
        {
            if (Count == 0)
                return "<empty>";

            return _rootObjectName + string.Concat(this.Select(x =>
            {
                if (x is JsonPathObjectSegment objectSegment)
                    return $".{objectSegment.Value}";
                else if (x is JsonPathArraySegment arraySegment)
                    return $"[{arraySegment.Value}]";
                else
                    throw new NotSupportedException();
            }));
        }
    }
}
