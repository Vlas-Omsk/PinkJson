using System;
using System.Collections.Generic;

namespace PinkJson2.Formatters
{
    public interface IFormatter
    {
        void Format(IEnumerable<JsonEnumerableItem> json, ITextWriter writer);
    }
}
