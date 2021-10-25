using System;
using System.Collections;

namespace PinkJson2.Serializers
{
    public static class HelperSerializer
    {
        public static bool IsArray(Type type)
        {
            return type.GetInterface(nameof(IEnumerable)) != null && type != typeof(string);
        }
    }
}
