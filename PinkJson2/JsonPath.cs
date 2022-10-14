using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PinkJson2
{
    public sealed class JsonPath : Collection<IJsonPathSegment>
    {
        private const string _rootObjectName = "root";

        public JsonPath(IList<IJsonPathSegment> segments) : base(segments)
        {
        }

        public override string ToString()
        {
            var pathString = new StringBuilder();
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
