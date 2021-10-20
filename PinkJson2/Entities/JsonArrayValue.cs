using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
