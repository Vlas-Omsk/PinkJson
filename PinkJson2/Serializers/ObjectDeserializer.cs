using System;
using System.Collections;
using System.Reflection;

namespace PinkJson2.Serializers
{
    public partial class ObjectSerializer : IDeserializer
    {
        public T Deserialize<T>(IJson json)
        {
            var type = typeof(T);

            return (T)Deserialize(json, type);
        }

        public object Deserialize(IJson json, Type type)
        {
            if (HelperSerializer.IsArray(type))
            {
                if (json is JsonArray)
                    return DeserializeArray((JsonArray)json, type);
                else
                    throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");
            }
            else if (!IsValueType(type))
            {
                if (json is JsonObject)
                    return DeserializeObject((JsonObject)json, type);
                else
                    throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");
            }
            else
                throw new Exception($"Can't convert json of type {json.GetType()} to an object of type {type}");
        }

        private object DeserializeValue(object value, Type type)
        {
            if (value != null)
            {
                if (HelperSerializer.IsArray(type))
                {
                    if (value is JsonArray)
                        return DeserializeArray((JsonArray)value, type);
                    else
                        throw new Exception($"Json of type {value.GetType()} cannot be converted to an object of type {type}");
                }
                else if (!IsValueType(type))
                {
                    if (value is JsonObject)
                        return DeserializeObject((JsonObject)value, type);
                    else
                        throw new Exception($"Json of type {value.GetType()} cannot be converted to an object of type {type}");
                }
            }

            return value;
        }

        private object DeserializeObject(JsonObject json, Type type)
        {
            var properties = type.GetProperties(PropertyBindingFlags);
            var fields = type.GetFields(FieldBindingFlags);
            var obj = CreateObject(json, type);

            foreach (var property in properties)
                if (TryDeserializeMember(property, property.PropertyType, json, out object value))
                    property.SetValue(obj, value);
            foreach (var field in fields)
                if (TryDeserializeMember(field, field.FieldType, json, out object value))
                    field.SetValue(obj, value);

            return obj;
        }

        private bool TryDeserializeMember(MemberInfo memberInfo, Type type, IJson json, out object obj)
        {
            obj = null;
            var key = memberInfo.Name;

            if (TryGetJsonPropertyAttribute(memberInfo, out JsonPropertyAttribute jsonPropertyAttribute))
            {
                if (jsonPropertyAttribute.Ignore)
                    return false;

                if (jsonPropertyAttribute.Name != null)
                    key = jsonPropertyAttribute.Name;
            }

            key = TransformKey(key);

            if (!json.ContainsKey(key))
            {
                if (IgnoreMissingProperties)
                    return false;
                else
                    throw new Exception($"Json does not include a field named {key}");
            }

            obj = DeserializeValue(json[key].Value, type);

            return true;
        }

        private IEnumerable DeserializeArray(JsonArray json, Type type)
        {
            var enumerableType = type.GetInterface("IEnumerable`1");
            Type elementType;
            if (enumerableType == null)
                elementType = typeof(object);
            else
                elementType = enumerableType.GenericTypeArguments[0];
            IList array;
            if (type.IsArray)
            {
                array = Array.CreateInstance(elementType, json.Count);
            }
            else
            {
                var cctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, Type.EmptyTypes, null);
                if (cctor == null)
                    throw new Exception($"Array object of type {type} does not contain an empty constructor to initialize");
                else
                    array = (IList)cctor.Invoke(Array.Empty<object>());
            }

            for (var i = 0; i < json.Count; i++)
            {
                var value = DeserializeValue(json[i].Value, elementType);
                if (array.IsFixedSize)
                    array[i] = value;
                else
                    array.Add(value);
            }

            return array;
        }

        private object CreateObject(IJson json, Type type)
        {
            var cctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new Type[] { typeof(IJson) }, null);
            if (cctor != null)
                return cctor.Invoke(new object[] { json });
            cctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, Type.EmptyTypes, null);
            if (cctor != null)
                return cctor.Invoke(Array.Empty<object>());

            throw new Exception($"No matching constructors found for object of type {type}");
        }
    }
}
