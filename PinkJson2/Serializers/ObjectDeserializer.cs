using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PinkJson2.Serializers
{
    public partial class ObjectConverter : IDeserializer
    {
        public T Deserialize<T>(IJson json)
        {
            return (T)Deserialize(json, typeof(T));
        }

        public object Deserialize(IJson json, Type type)
        {
            if (HelperSerializer.IsArray(type))
                return DeserializeArray(json, type, true);
            else if (!IsValueType(type))
                return DeserializeObject(json, type, true);
            else
                throw new Exception($"Can't convert json of type {json.GetType()} to an object of type {type}");
        }

        public object DeserializeToObject(IJson json, object rootObj)
        {
            var type = rootObj.GetType();

            if (HelperSerializer.IsArray(type))
                return DeserializeArray(json, type, rootObj, GetElementType(type), true);
            else if (!IsValueType(type))
                return DeserializeObject(json, type, rootObj, true);
            else
                throw new Exception($"Can't convert json of type {json.GetType()} to an object of type {type}");
        }

        private object DeserializeValue(object value, Type type)
        {
            if (value != null)
            {
                if (HelperSerializer.IsArray(type))
                    return DeserializeArray((IJson)value, type, false);
                else if (!IsValueType(type))
                    return DeserializeObject((IJson)value, type, false);
            }

            return TransformValue(value, type);
        }

        private object DeserializeObject(IJson json, Type type, bool isRoot)
        {
            return DeserializeObject(json, type, CreateObject(json, type), isRoot);
        }

        private object DeserializeObject(IJson json, Type type, object obj, bool isRoot)
        {
            if (!(json is JsonObject))
                throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");

            ConstructorInfo anonymouseTypeConstructor = null;
            List<object> anonymouseTypeConstructorParameters = null;
            var isAnonymouseType = HelperSerializer.IsAnonymousType(type);

            if (isAnonymouseType)
            {
                anonymouseTypeConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
                anonymouseTypeConstructorParameters = new List<object>(anonymouseTypeConstructor.GetParameters().Length);
            }

            if (!isAnonymouseType && TryCustomDeserialize(obj, json, isRoot))
                return obj;

            var properties = type.GetProperties(PropertyBindingFlags);
            var fields = type.GetFields(FieldBindingFlags);

            foreach (var property in properties)
                if (TryDeserializeMember(property, property.PropertyType, json, out object value))
                {
                    if (!isAnonymouseType)
                        property.SetValue(obj, value);
                    else
                        anonymouseTypeConstructorParameters.Add(value);
                }   
            if (!isAnonymouseType)
                foreach (var field in fields)
                    if (TryDeserializeMember(field, field.FieldType, json, out object value))
                        field.SetValue(obj, value);

            if (isAnonymouseType)
                return anonymouseTypeConstructor.Invoke(anonymouseTypeConstructorParameters.ToArray());
            else
                return obj;
        }

        private bool TryDeserializeMember(MemberInfo memberInfo, Type type, IJson json, out object obj)
        {
            obj = null;
            var key = memberInfo.Name;

            if (TryGetJsonPropertyAttribute(memberInfo, out JsonPropertyAttribute jsonPropertyAttribute))
            {
                if (jsonPropertyAttribute.DeserializerIgnore)
                    return false;

                if (jsonPropertyAttribute.DeserializerName != null)
                    key = jsonPropertyAttribute.DeserializerName;
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

        private object DeserializeArray(IJson json, Type type, bool isRoot)
        {
            var elementType = GetElementType(type);
            var array = CreateArray((JsonArray)json, type, elementType);

            return DeserializeArray(json, type, array, elementType, isRoot);
        }

        private object DeserializeArray(IJson json, Type type, object obj, Type elementType, bool isRoot)
        {
            if (!(json is JsonArray))
                throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");

            if (TryCustomDeserialize(obj, json, isRoot))
                return obj;

            var array = (IList)obj;

            for (var i = 0; i < ((JsonArray)json).Count; i++)
            {
                var value = DeserializeValue(json[i].Value, elementType);
                if (array.IsFixedSize)
                    array[i] = value;
                else
                    array.Add(value);
            }

            return array;
        }

        private bool TryCustomDeserialize(object obj, IJson json, bool isRoot)
        {
            if (!(IgnoreCustomDeserializers || (IgnoreRootCustomDeserializer && isRoot)) && obj is ICustomDeserializable)
            {
                var customDeserializable = (ICustomDeserializable)obj;
                customDeserializable.Deserialize(this, json);
                return true;
            }
            return false;
        }

        private Type GetElementType(Type type)
        {
            var enumerableType = type.GetInterface("IEnumerable`1");
            if (enumerableType == null)
                return typeof(object);
            else
                return enumerableType.GenericTypeArguments[0];
        }

        private IList CreateArray(JsonArray json, Type type, Type elementType)
        {
            if (type.IsArray)
                return Array.CreateInstance(elementType, json.Count);
            else
                return (IList)CreateObject(json, type);
        }

        private object CreateObject(IJson json, Type type)
        {
            var cctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new Type[] { typeof(IJson) }, null);
            if (cctor != null)
                return cctor.Invoke(new object[] { json });
            cctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, Type.EmptyTypes, null);
            if (cctor != null)
                return cctor.Invoke(Array.Empty<object>());
            if (HelperSerializer.IsAnonymousType(type))
                return null;

            throw new Exception($"No matching constructors found for object of type {type}");
        }

        private object TransformValue(object value, Type type)
        {
            if (type == typeof(DateTime))
            {
                if (value is string)
                    return DateTime.Parse((string)value);
                else
                    throw new Exception($"Can't convert value of type {value.GetType()} to {type}");
            }
            else if (type != value.GetType())
                return Convert.ChangeType(value, type);
            else
                return value;
        }
    }
}
