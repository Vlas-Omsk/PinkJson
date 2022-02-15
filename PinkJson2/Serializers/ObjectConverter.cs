using System;
using System.Reflection;

namespace PinkJson2.Serializers
{
    public partial class ObjectConverter
    {
        public BindingFlags PropertyBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;
        public BindingFlags FieldBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;
        public bool IgnoreMissingProperties { get; set; } = true;
        public bool IgnoreRootCustomSerializer { get; set; } = false;
        public bool IgnoreRootCustomDeserializer { get; set; } = false;
        public bool IgnoreCustomSerializers { get; set; } = false;
        public bool IgnoreCustomDeserializers { get; set; } = false;
        public bool DeserializerIgnoreReadOnlyProperty { get; set; } = true;
        public bool SerializerIgnoreWriteOnlyProperties { get; set; } = true;

        private bool TryGetJsonPropertyAttribute(MemberInfo memberInfo, out JsonPropertyAttribute jsonPropertyAttribute)
        {
            jsonPropertyAttribute = memberInfo.GetCustomAttribute(typeof(JsonPropertyAttribute), true) as JsonPropertyAttribute;
            return jsonPropertyAttribute != null;
        }

        private static bool IsValueType(Type type)
        {
            return type.IsValueType || type == typeof(string);
        }

        protected virtual string TransformKey(string key)
        {
            return key;
        }
    }
}
