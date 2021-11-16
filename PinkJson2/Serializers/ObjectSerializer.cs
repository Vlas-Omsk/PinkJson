using System;
using System.Collections;
using System.Reflection;

namespace PinkJson2.Serializers
{
    public partial class ObjectConverter : ISerializer
    {
        public IJson Serialize(object obj)
        {
            var type = obj.GetType();

            if (HelperSerializer.IsArray(type))
                return SerializeArray(obj, true);
            else if (!IsValueType(type))
                return SerializeObject(obj, true);
            else
                throw new Exception($"Can't convert object of type {type} to json");
        }

        private object SerializeValue(object value, Type valueType)
        {
            if (value != null)
            {
                if (HelperSerializer.IsArray(valueType))
                    value = SerializeArray(value, false);
                else if (!IsValueType(valueType))
                    value = SerializeObject(value, false);
            }

            return value;
        }

        private IJson SerializeObject(object obj, bool isRoot)
        {
            IJson jsonObject;

            if (TryCustomSerialize(obj, out jsonObject, isRoot))
                return jsonObject;

            var type = obj.GetType();
            var properties = type.GetProperties(PropertyBindingFlags);
            var fields = type.GetFields(FieldBindingFlags);
            jsonObject = new JsonObject();

            foreach (var property in properties)
                if (TrySerializeMember(property, property.PropertyType, property.GetValue(obj), out JsonKeyValue jsonKeyValue))
                    ((JsonObject)jsonObject).AddLast(jsonKeyValue);
            foreach (var field in fields)
                if (TrySerializeMember(field, field.FieldType, field.GetValue(obj), out JsonKeyValue jsonKeyValue))
                    ((JsonObject)jsonObject).AddLast(jsonKeyValue);

            return jsonObject;
        }

        private bool TrySerializeMember(MemberInfo memberInfo, Type type, object value, out JsonKeyValue jsonKeyValue)
        {
            jsonKeyValue = null;
            var key = memberInfo.Name;

            if (TryGetJsonPropertyAttribute(memberInfo, out JsonPropertyAttribute jsonPropertyAttribute))
            {
                if (jsonPropertyAttribute.SerializerIgnore)
                    return false;

                if (jsonPropertyAttribute.SerializerName != null)
                    key = jsonPropertyAttribute.SerializerName;

                if (!jsonPropertyAttribute.IsValueType)
                    value = SerializeValue(value, type);
            }
            else
            {
                value = SerializeValue(value, type);
            }

            key = TransformKey(key);
            jsonKeyValue = new JsonKeyValue(key, value);

            return true;
        }

        private IJson SerializeArray(object obj, bool isRoot)
        {
            IJson jsonArray;

            if (TryCustomSerialize(obj, out jsonArray, isRoot))
                return jsonArray;

            var enumerable = (IEnumerable)obj;
            jsonArray = new JsonArray();

            foreach (var item in enumerable)
                ((JsonArray)jsonArray).AddLast(new JsonArrayValue(SerializeValue(item, item.GetType())));

            return jsonArray;
        }

        private bool TryCustomSerialize(object obj, out IJson json, bool isRoot)
        {
            if (!(IgnoreCustomSerializers || (IgnoreRootCustomSerializer && isRoot)) && obj is ICustomSerializable serializable)
            {
                json = serializable.Serialize(this);
                return true;
            }
            json = null;
            return false;
        }
    }
}
