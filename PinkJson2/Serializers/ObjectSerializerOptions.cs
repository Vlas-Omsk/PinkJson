using System;
using System.Reflection;

namespace PinkJson2.Serializers
{
    public class ObjectSerializerOptions
    {
        public BindingFlags PropertyBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;
        public BindingFlags FieldBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;
        public bool IgnoreMissingProperties { get; set; } = true;
        public bool PreserveObjectsReferences { get; set; }

        public static ObjectSerializerOptions Default { get; set; } = new ObjectSerializerOptions();
    }
}
