﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace PinkJson2.Serializers
{
    public sealed class ObjectSerializerOld : ISerializerOld
    {
        private const string _indexerPropertyName = "Item";
        private readonly List<object> _ids = new List<object>();
        private bool _running;

        public ObjectSerializerOld()
        {
            Options = ObjectSerializerOptions.Default;
        }

        public ObjectSerializerOld(ObjectSerializerOptions options)
        {
            Options = options;
        }

        public ObjectSerializerOptions Options { get; set; }

        public IJson Serialize(object instance)
        {
            return Serialize(instance, !_running);
        }

        public IJson Serialize(object instance, bool useJsonSerialize)
        {
            var isCaller = !_running;
            _running = true;

            try
            {
                var type = instance.GetType();

                return (IJson)SerializeValue(instance, type, useJsonSerialize);
            }
            finally
            {
                if (isCaller)
                {
                    _ids.Clear();
                    _running = false;
                }
            }
        }

        private object SerializeValue(object value, Type type)
        {
            return SerializeValue(value, type, true);
        }

        private object SerializeValue(object value, Type type, bool useJsonDeserialize)
        {
            if (value == null)
                return null;

            if (value.GetType().IsAssignableToCached(type))
                type = value.GetType();

            if (type.IsAssignableToCached(typeof(IJson)))
                return value;
            else if (type.IsArrayType())
                return SerializeArray(value, useJsonDeserialize);
            else if (!Options.TypeConverter.IsPrimitiveType(type))
                return SerializeObject(value, useJsonDeserialize);

            return Options.TypeConverter.ChangeType(value, typeof(object));
        }

        private IJson SerializeObject(object obj, bool useJsonSerialize)
        {
            IJson jsonObject;

            var id = _ids.IndexOf(obj);

            if (Options.PreserveObjectsReferences)
            {
                if (id != -1)
                    return new JsonObject(new JsonKeyValue("$ref", id));

                if (useJsonSerialize && TryJsonSerialize(obj, out jsonObject))
                {
                    id = _ids.IndexOf(obj);

                    if (id == -1)
                    {
                        id = _ids.Count;
                        _ids.Add(obj);
                    }

                    if (!jsonObject.ContainsKey("$id"))
                        ((JsonObject)jsonObject).AddLast(new JsonKeyValue("$id", id));

                    return jsonObject;
                }

                id = _ids.Count;
                _ids.Add(obj);
                jsonObject = new JsonObject(new JsonKeyValue("$id", id));
            }
            else
            {
                if (id != -1)
                    throw new JsonSerializationException($"Self referencing loop detected");

                if (useJsonSerialize && TryJsonSerialize(obj, out jsonObject))
                {
                    if (!_ids.Contains(obj))
                        _ids.Add(obj);

                    return jsonObject;
                }

                _ids.Add(obj);
                jsonObject = new JsonObject();
            }

            if (obj is ISerializable serializable)
            {
                var formatter = new FormatterConverter();
                var info = new SerializationInfo(obj.GetType(), formatter);
                serializable.GetObjectData(info, new StreamingContext());

                foreach (var prop in info)
                {
                    var key = Options.KeyTransformer.TransformKey(prop.Name);
                    var jsonKeyValue = new JsonKeyValue(key, SerializeValue(prop.Value, prop.ObjectType));
                    ((JsonObject)jsonObject).AddLast(jsonKeyValue);
                }

                return jsonObject;
            }
            var type = obj.GetType();
            var properties = type.GetProperties(Options.PropertyBindingFlags);
            var fields = type.GetFields(Options.FieldBindingFlags);

            foreach (var property in properties)
                if (property.GetMethod != null &&
                    property.Name != _indexerPropertyName &&
                    TrySerializeMember(property, property.PropertyType, property.GetValue(obj), out JsonKeyValue jsonKeyValue))
                    ((JsonObject)jsonObject).AddLast(jsonKeyValue);
            foreach (var field in fields)
                if (TrySerializeMember(field, field.FieldType, field.GetValue(obj), out JsonKeyValue jsonKeyValue))
                    ((JsonObject)jsonObject).AddLast(jsonKeyValue);

            return jsonObject;
        }

        private bool TrySerializeMember(MemberInfo memberInfo, Type type, object value, out JsonKeyValue jsonKeyValue)
        {
            jsonKeyValue = null;

            if (memberInfo.TryGetCustomAttribute<NonSerializedAttribute>(out _))
                return false;

            var key = memberInfo.Name;

            if (memberInfo.TryGetCustomAttribute(out JsonPropertyAttribute jsonPropertyAttribute))
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

            key = Options.KeyTransformer.TransformKey(key);
            jsonKeyValue = new JsonKeyValue(key, value);

            return true;
        }

        private IJson SerializeArray(object obj, bool useJsonSerialize)
        {
            if (useJsonSerialize && TryJsonSerialize(obj, out IJson json))
                return json;

            var type = obj.GetType();

            if (type.IsEqualsOrAssignableTo(typeof(IDictionary)))
            {
                var dictionary = (IDictionary)obj;
                var genericDictionaryType = type.GetInterface("IDictionary`2");
                var keyType = genericDictionaryType?.GetGenericArguments()[0];

                if (genericDictionaryType == null || keyType != typeof(string))
                {
                    json = new JsonArray();

                    foreach (var item in dictionary)
                        ((JsonArray)json).AddLast(new JsonArrayValue(SerializeValue(item, item?.GetType())));
                }
                else
                {
                    var valueType = genericDictionaryType.GetGenericArguments()[1];
                    var enumerator = dictionary.GetEnumerator();

                    json = new JsonObject();

                    while (enumerator.MoveNext())
                        ((JsonObject)json).AddLast(new JsonKeyValue((string)enumerator.Key, SerializeValue(enumerator.Value, valueType)));
                }
            }
            else
            {
                var enumerable = (IEnumerable)obj;

                json = new JsonArray();

                foreach (var item in enumerable)
                    ((JsonArray)json).AddLast(new JsonArrayValue(SerializeValue(item, item?.GetType())));
            }

            return json;
        }

        private bool TryJsonSerialize(object obj, out IJson json)
        {
            if (obj is IJsonSerializableOld serializable)
            {
                json = serializable.Serialize(this);
                return true;
            }

            json = null;
            return false;
        }
    }
}
