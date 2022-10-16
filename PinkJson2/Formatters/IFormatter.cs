using System;
using System.Collections.Generic;
using System.IO;

namespace PinkJson2.Formatters
{
    public interface IFormatter
    {
        void Format(IEnumerable<JsonEnumerableItem> json, TextWriter writer);
    }
}
