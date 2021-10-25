using System;
using System.Collections;
using System.Reflection;

namespace PinkJson2.Serializers
{
    public partial class ObjectSerializer : ISerializer
    {
        public BindingFlags PropertyBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance | (BindingFlags.GetField & BindingFlags.SetField);
        public BindingFlags FieldBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance | (BindingFlags.GetField & BindingFlags.SetField);
        public bool IgnoreMissingProperties { get; set; } = true;

        public ObjectSerializer()
        {
        }

        public IJson Serialize(object obj)
        {
            var type = obj.GetType();

            if (HelperSerializer.IsArray(type))
                return SerializeArray((IEnumerable)obj);
            else if (!IsValueType(type))
                return SerializeObject(obj);
            else
                throw new Exception($"Can't convert object of type {type} to json");
        }

        private object SerializeValue(object value, Type valueType)
        {
            if (value != null)
            {
                if (HelperSerializer.IsArray(valueType))
                    value = SerializeArray((IEnumerable)value);
                else if (!IsValueType(valueType))
                    value = SerializeObject(value);
            }

            return value;
        }

        private JsonObject SerializeObject(object obj)
        {
            var type = obj.GetType();
            var properties = type.GetProperties(PropertyBindingFlags);
            var fields = type.GetFields(FieldBindingFlags);
            var jsonObject = new JsonObject();

            foreach (var property in properties)
                if (TrySerializeMember(property, property.PropertyType, property.GetValue(obj), out JsonKeyValue jsonKeyValue))
                    jsonObject.AddLast(jsonKeyValue);
            foreach (var field in fields)
                if (TrySerializeMember(field, field.FieldType, field.GetValue(obj), out JsonKeyValue jsonKeyValue))
                    jsonObject.AddLast(jsonKeyValue);

            return jsonObject;
        }

        private bool TrySerializeMember(MemberInfo memberInfo, Type type, object value, out JsonKeyValue jsonKeyValue)
        {
            jsonKeyValue = null;
            var key = memberInfo.Name;

            if (TryGetJsonPropertyAttribute(memberInfo, out JsonPropertyAttribute jsonPropertyAttribute))
            {
                if (jsonPropertyAttribute.Ignore)
                    return false;

                if (jsonPropertyAttribute.Name != null)
                    key = jsonPropertyAttribute.Name;
            }

            key = TransformKey(key);
            jsonKeyValue = new JsonKeyValue(key, SerializeValue(value, type));

            return true;
        }

        private JsonArray SerializeArray(IEnumerable enumerable)
        {
            var jsonArray = new JsonArray();

            foreach (var item in enumerable)
                jsonArray.AddLast(new JsonArrayValue(SerializeValue(item, item.GetType())));

            return jsonArray;
        }

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
