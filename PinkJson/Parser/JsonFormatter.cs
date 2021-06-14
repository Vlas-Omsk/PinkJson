using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace PinkJson
{
    public static class JsonFormatter
    {
        public static object ValueToFormatJsonString(object value, ushort spacing, uint gen)
        {
            if (value is ObjectBase)
                value = (value as ObjectBase).ToFormatString(spacing, gen);
            //else if (value is JsonObject)
            //    value = (value as JsonObject).ToFormatString(spacing, gen);
            //else if (value is JsonArray)
            //    value = (value as JsonArray).ToFormatString(spacing, gen);
            //else if (value is JsonArrayObject)
            //    value = (value as JsonArrayObject).ToFormatString(spacing, gen);
            else
                value = ValueToJsonString(value);
            return value;
        }

        public static object ValueToJsonString(object value)
        {
            if (value is null)
                return "null";
            else if (value is bool)
                return ((bool)value) ? "true" : "false";
            else if (value is DateTime)
                return '\"' + ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + '\"';
            else if (value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal
                    || value is BigInteger)
                return value.ToString().Replace(',', '.');
            else if (value is ObjectBase)
                return (value as ObjectBase).ToString();
            //else if (value is JsonObject)
            //    return (value as JsonObject).ToString();
            //else if (value is JsonArray)
            //    return (value as JsonArray).ToString();
            //else if (value is JsonArrayObject)
            //    return (value as JsonArrayObject).ToString();
            else
                return $"\"{value.ToString().EscapeString()}\"";
        }
    }
}
