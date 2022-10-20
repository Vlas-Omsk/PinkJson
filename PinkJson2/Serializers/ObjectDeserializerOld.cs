using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace PinkJson2.Serializers
{
    public sealed class ObjectDeserializerOld : IDeserializer
    {
        private const string _idField = "$id";
        private const string _refField = "$ref";
        private const string _keyPropertyName = "Key";
        private const string _valuePropertyName = "Value";
        private readonly Dictionary<int, JsonId> _ids = new Dictionary<int, JsonId>();
        private bool _running;

        private class JsonId
        {
            public IJson Json { get; set; }
            public object Obj { get; set; }
            public bool HasObj { get; set; }
        }

        private class DeserializerFormatterConverter : IFormatterConverter
        {
            private readonly ObjectDeserializerOld _deserializer;

            public DeserializerFormatterConverter(ObjectDeserializerOld deserializer)
            {
                _deserializer = deserializer;
            }

            public T Convert<T>(object value)
            {
                return (T)Convert(value, typeof(T));
            }

            public object Convert(object value, Type type)
            {
                if (value is IJson json)
                    return _deserializer.Deserialize(json, type);
                else
                    return _deserializer.Options.TypeConverter.ChangeType(value, type);
            }

            public object Convert(object value, TypeCode typeCode)
            {
                throw new NotSupportedException();
            }

            public bool ToBoolean(object value)
            {
                return Convert<bool>(value);
            }

            public byte ToByte(object value)
            {
                return Convert<byte>(value);
            }

            public char ToChar(object value)
            {
                return Convert<char>(value);
            }

            public DateTime ToDateTime(object value)
            {
                return Convert<DateTime>(value);
            }

            public decimal ToDecimal(object value)
            {
                return Convert<decimal>(value);
            }

            public double ToDouble(object value)
            {
                return Convert<double>(value);
            }

            public short ToInt16(object value)
            {
                return Convert<short>(value);
            }

            public int ToInt32(object value)
            {
                return Convert<int>(value);
            }

            public long ToInt64(object value)
            {
                return Convert<long>(value);
            }

            public sbyte ToSByte(object value)
            {
                return Convert<sbyte>(value);
            }

            public float ToSingle(object value)
            {
                return Convert<float>(value);
            }

            public string ToString(object value)
            {
                return Convert<string>(value);
            }

            public ushort ToUInt16(object value)
            {
                return Convert<ushort>(value);
            }

            public uint ToUInt32(object value)
            {
                return Convert<uint>(value);
            }

            public ulong ToUInt64(object value)
            {
                return Convert<ulong>(value);
            }
        }

        public ObjectDeserializerOld()
        {
            Options = ObjectSerializerOptions.Default;
        }

        public ObjectDeserializerOld(ObjectSerializerOptions options)
        {
            Options = options;
        }

        public ObjectSerializerOptions Options { get; set; }

        public object Deserialize(IJson json, Type type)
        {
            return DeserializeInternal(json, null, type, true, !_running);
        }

        public object Deserialize(IJson json, object instance)
        {
            return DeserializeInternal(json, instance, instance.GetType(), false, !_running);
        }

        public object Deserialize(IJson json, object instance, bool useJsonDeserialize)
        {
            return DeserializeInternal(json, instance, instance.GetType(), false, useJsonDeserialize);
        }

        private object DeserializeInternal(IJson json, object instance, Type type, bool createObject, bool useJsonDeserialize)
        {
            var isCaller = !_running;
            _running = true;

            try
            {
                if (isCaller)
                    AggregateIds(json);

                return DeserializeValue(json, type, instance, createObject, useJsonDeserialize);
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

        private void AggregateIds(object obj)
        {
            if (obj is JsonObject jsonObject)
            {
                if (jsonObject.ContainsKey(_idField))
                    _ids.Add(jsonObject[_idField].Get<int>(Options.TypeConverter), new JsonId() { Json = jsonObject });

                foreach (var item in jsonObject)
                    AggregateIds(item.Value);
            }
            if (obj is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                    AggregateIds(item.Value);
            }
        }

        private object ResolveRef(int id, Type type)
        {
            if (!_ids.TryGetValue(id, out JsonId jsonId))
                throw new Exception("Reference " + id + " not found");

            if (jsonId.HasObj)
                return jsonId.Obj;

            jsonId.Obj = DeserializeValue(jsonId.Json, type);
            jsonId.HasObj = true;
            return jsonId.Obj;
        }

        private void TryAddRef(IJson json, object obj)
        {
            if (!json.ContainsKey(_idField))
                return;

            var id = json[_idField].Get<int>();
            var jsonId = _ids[id];
            jsonId.Obj = obj;
            jsonId.HasObj = true;
        }

        private object DeserializeValue(IJson json, Type type)
        {
            return DeserializeValue(json, type, null, true, true);
        }

        private object DeserializeValue(IJson json, Type type, object instance, bool createObject, bool useJsonDeserialize)
        {
            type = TryGetUnderlayingType(type);

            if (type != typeof(object) && type.IsEqualsOrAssignableTo(typeof(IJson)))
                return json.Value;

            var value = json.Value;

            if (value == null)
                return null;

            var valueType = value.GetType();

            if (valueType != type && valueType.IsAssignableToCached(type))
                type = value.GetType();

            if (type.IsArrayType() && (!type.IsEqualsOrAssignableTo(typeof(IDictionary)) || value is JsonArray))
                return DeserializeArray((IJson)value, type, instance, createObject, useJsonDeserialize);
            else if (!Options.TypeConverter.IsPrimitiveType(type))
                return DeserializeObject((IJson)value, type, instance, createObject, useJsonDeserialize);
            else
                return Options.TypeConverter.ChangeType(value, type);
        }

        private object DeserializeObject(IJson json, Type type, object obj, bool createObject, bool useJsonDeserialize)
        {
            if (json == null)
                return null;

            if (!(json is JsonObject))
                throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");

            if (json.ContainsKey(_refField))
                return ResolveRef(json[_refField].Get<int>(), type);

            if (json.ContainsKey(_idField) && 
                _ids.TryGetValue(json[_idField].Get<int>(), out JsonId jsonId) && 
                jsonId.HasObj)
                return jsonId.Obj;

            if (type.IsAnonymousType())
                return DeserializeAnonymouseObject(json, type);

            if (createObject)
            {
                if (type.Name != "Dictionary`2")
                {
                    var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(x =>
                        {
                            var parameters = x.GetParameters();

                            return parameters.Length == 2 &&
                                parameters[0].ParameterType == typeof(SerializationInfo) &&
                                parameters[1].ParameterType == typeof(StreamingContext);
                        });

                    if (ctor != null)
                    {
                        var formatter = new DeserializerFormatterConverter(this);
                        var info = new SerializationInfo(type, formatter);

                        obj = FormatterServices.GetUninitializedObject(type);

                        TryAddRef(json, obj);

                        foreach (var keyValue in json.AsObject())
                            info.AddValue(Options.KeyTransformer.TransformKey(keyValue.Key), keyValue.Value);

                        ctor.Invoke(obj, new object[] { info, new StreamingContext() });

                        NotifyDeserialized(obj);
                        return obj;
                    }
                }

                obj = CreateObject(json, type);
            }

            if (useJsonDeserialize && TryJsonDeserialize(obj, json))
                return obj;

            TryAddRef(json, obj);

            if (type.IsEqualsOrAssignableTo(typeof(IDictionary)))
            {
                var genericDictionaryType = type.GetInterface("IDictionary`2");

                if (genericDictionaryType == null)
                    throw new Exception();

                var keyType = genericDictionaryType.GetGenericArguments()[0];
                var valueType = genericDictionaryType.GetGenericArguments()[1];
                var dictionary = (IDictionary)obj;

                foreach (var keyValue in json.AsObject())
                    dictionary.Add(
                        Options.TypeConverter.ChangeType(keyValue.Key, keyType),
                        DeserializeValue(keyValue, valueType)
                    );
            }
            else
            {
                var properties = type.GetProperties(Options.PropertyBindingFlags);
                var fields = type.GetFields(Options.FieldBindingFlags);

                foreach (var property in properties)
                    if (property.SetMethod != null)
                        if (TryDeserializeMember(property, property.PropertyType, json, out object value))
                            property.SetValue(obj, value);

                foreach (var field in fields)
                    if (TryDeserializeMember(field, field.FieldType, json, out object value))
                        field.SetValue(obj, value);
            }

            NotifyDeserialized(obj);
            return obj;
        }

        private object DeserializeAnonymouseObject(IJson json, Type type)
        {
            if (!(json is JsonObject))
                throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");

            var obj = FormatterServices.GetUninitializedObject(type);

            TryAddRef(json, obj);

            var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var constructorParameters = new object[constructor.GetParameters().Length];
            var properties = type.GetProperties(Options.PropertyBindingFlags);

            var i = 0;
            foreach (var property in properties)
            {
                TryDeserializeMember(property, property.PropertyType, json, out object value);
                constructorParameters[i] = value;

                i++;
            }

            constructor.Invoke(obj, constructorParameters);
            NotifyDeserialized(obj);
            return obj;
        }

        private void NotifyDeserialized(object obj)
        {
            if (obj is IDeserializationCallback deserializationCallback)
                deserializationCallback.OnDeserialization(this);
        }

        private bool TryDeserializeMember(MemberInfo memberInfo, Type type, IJson json, out object value)
        {
            value = null;

            if (memberInfo.TryGetCustomAttribute<NonSerializedAttribute>(out _))
                return false;

            var key = memberInfo.Name;

            if (memberInfo.TryGetCustomAttribute(out JsonPropertyAttribute jsonPropertyAttribute))
            {
                if (jsonPropertyAttribute.DeserializerIgnore)
                    return false;

                if (jsonPropertyAttribute.DeserializerName != null)
                    key = jsonPropertyAttribute.DeserializerName;

                if (jsonPropertyAttribute.DeserializeToType != null)
                    type = jsonPropertyAttribute.DeserializeToType;
            }

            key = Options.KeyTransformer.TransformKey(key);

            if (!json.ContainsKey(key))
            {
                if (Options.IgnoreMissingProperties)
                    return false;
                else
                    throw new Exception($"Json does not include a field named {key}");
            }

            value = DeserializeValue(json[key], type);
            return true;
        }

        private object DeserializeArray(IJson json, Type type, object obj, bool createObject, bool useJsonDeserialize)
        {
            var elementType = GetElementType(type);

            if (createObject)
                obj = CreateArray(json, type, elementType);

            if (useJsonDeserialize && TryJsonDeserialize(obj, json))
                return obj;

            if (!(json is JsonArray))
                throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");

            if (type.IsEqualsOrAssignableTo(typeof(IDictionary)))
            {
                var genericDictionaryType = type.GetInterface("IDictionary`2");

                if (genericDictionaryType == null)
                    throw new Exception();

                var keyType = genericDictionaryType.GetGenericArguments()[0];
                var valueType = genericDictionaryType.GetGenericArguments()[1];
                var dictionary = (IDictionary)obj;

                foreach (var arrayValue in json.AsArray())
                    dictionary.Add(
                        DeserializeValue(arrayValue[_keyPropertyName], keyType),
                        DeserializeValue(arrayValue[_valuePropertyName], valueType)
                    );

                return dictionary;
            }
            else
            {
                var array = (IList)obj;

                for (var i = 0; i < json.Count; i++)
                {
                    if (!array.IsFixedSize)
                        array.Add(null);

                    var localIndex = i;
                    array[localIndex] = DeserializeValue(json[i], elementType);
                }

                return array;
            }
        }

        private bool TryJsonDeserialize(object obj, IJson json)
        {
            if (obj is IJsonDeserializable jsonDeserializable)
            {
                jsonDeserializable.Deserialize(this, json);
                return true;
            }
            return false;
        }

        private Type GetElementType(Type type)
        {
            var enumerableType = type;
            if (type.Name != "IEnumerable`1")
                enumerableType = type.GetInterface("IEnumerable`1");

            if (enumerableType == null)
                return typeof(object);
            else
                return enumerableType.GenericTypeArguments[0];
        }

        private IEnumerable CreateArray(IJson json, Type type, Type elementType)
        {
            if (type.IsArray || type == typeof(IEnumerable) || type.Name == "IEnumerable`1")
                return Array.CreateInstance(elementType, json.Count);
            else
                return (IEnumerable)CreateObject(json, type);
        }

        private object CreateObject(IJson json, Type type)
        {
            var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new Type[] { typeof(IJson) }, null);
            if (ctor != null)
                return ctor.Invoke(new object[] { json });
            ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, Type.EmptyTypes, null);
            if (ctor != null)
                return ctor.Invoke(Array.Empty<object>());
            if (type.IsValueType())
                return FormatterServices.GetUninitializedObject(type);

            throw new Exception($"No matching constructors found for object of type {type}");
        }

        private static Type TryGetUnderlayingType(Type type)
        {
            var underlayingType = Nullable.GetUnderlyingType(type);
            if (underlayingType != null)
                type = underlayingType;
            return type;
        }
    }
}
