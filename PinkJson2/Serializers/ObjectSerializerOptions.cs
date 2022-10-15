using PinkJson2.KeyTransformers;
using System;
using System.Reflection;

namespace PinkJson2.Serializers
{
    public class ObjectSerializerOptions
    {
        public static ObjectSerializerOptions Default { get; set; } = new ObjectSerializerOptions()
        {
            TypeConverter = TypeConverter.Default
        };
        public BindingFlags PropertyBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;
        public BindingFlags FieldBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;
        public bool IgnoreMissingProperties { get; set; } = true;
        public bool PreserveObjectsReferences { get; set; } = false;
        public TypeConverter TypeConverter { get; set; } = TypeConverter.Default.Clone();
        public IKeyTransformer KeyTransformer { get; set; } = new DefaultKeyTransformer();
    }
}
