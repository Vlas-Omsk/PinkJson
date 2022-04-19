using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace PinkJson2.Serializers
{
    public class ObjectDeserializer : IDeserializer
    {
        public ObjectSerializerOptions Options { get; set; }

        private readonly Dictionary<int, object> _ids = new Dictionary<int, object>();
        private readonly Dictionary<int, List<Action<object>>> _refs = new Dictionary<int, List<Action<object>>>();

        public ObjectDeserializer()
        {
            Options = ObjectSerializerOptions.Default;
        }

        public ObjectDeserializer(ObjectSerializerOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public object Deserialize(IJson json, Type type)
        {
            return DeserializeInternal(json, null, type, true, true);
        }

        public object Deserialize(IJson json, object instance, bool useJsonDeserialize)
        {
            return DeserializeInternal(json, instance, instance.GetType(), false, useJsonDeserialize);
        }

        private object DeserializeInternal(IJson json, object instance, Type type, bool createObject, bool useJsonDeserialize)
        {
            try
            {
                if (type.IsArrayType())
                    instance = DeserializeArray(json, type, instance, GetElementType(type), useJsonDeserialize);
                else if (!type.IsValueType())
                    DeserializeObject(json, type, instance, createObject, value => instance = value, useJsonDeserialize);
                else
                    throw new Exception($"Can't convert json of type {json.GetType()} to an object of type {type}");

                while (_refs.Any())
                {
                    foreach (var @ref in _refs)
                    {
                        if (!_ids.TryGetValue(@ref.Key, out object value))
                            continue;

                        @ref.Value.ForEach(x => x.Invoke(value));
                        _refs.Remove(@ref.Key);
                    }
                }

                return instance;
            }
            finally
            {
                _ids.Clear();
                _refs.Clear();
            }
        }

        private void DeserializeValue(object value, Type type, Action<object> setValue)
        {
            if (value == null)
                setValue(null);
            else if (type.IsAssignableTo(typeof(IJson)))
                setValue(value);
            else if (type.IsArrayType())
                setValue(DeserializeArray((IJson)value, type, true));
            else if (!type.IsValueType())
                DeserializeObject((IJson)value, type, null, true, setValue, true);
            else
                setValue(TypeHelper.ChangeType(type, value));
        }

        private void DeserializeObject(IJson json, Type type, object obj, bool createObject, Action<object> setValue, bool useJsonDeserialize)
        {
            if (json == null)
                setValue(null);

            if (!(json is JsonObject))
                throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");

            if (json.ContainsKey("$ref"))
            {
                var @ref = json["$ref"].Get<int>();
                if (!_refs.TryGetValue(@ref, out List<Action<object>> actions))
                {
                    actions = new List<Action<object>>();
                    _refs.Add(@ref, actions);
                }
                actions.Add(setValue);
                return;
            }

            if (type.IsAnonymousType())
            {
                DeserializeAnonymouseObject(json, type, setValue);
                return;
            }

            if (createObject)
                obj = CreateObject(json, type);

            if (useJsonDeserialize && TryJsonDeserialize(obj, json))
            {
                TryAddToCache((JsonObject)json, obj, true);
                setValue(obj);
                return;
            }

            TryAddToCache((JsonObject)json, obj);

            var properties = type.GetProperties(Options.PropertyBindingFlags);
            var fields = type.GetFields(Options.FieldBindingFlags);

            foreach (var property in properties)
                if (property.SetMethod != null)
                    TryDeserializeMember(property, property.PropertyType, json, value => property.SetValue(obj, value));

            foreach (var field in fields)
                TryDeserializeMember(field, field.FieldType, json, value => field.SetValue(obj, value));

            setValue(obj);
        }

        private void TryAddToCache(JsonObject json, object obj, bool canReplace = false)
        {
            if (json.ContainsKey("$id"))
            {
                var id = json["$id"].Get<int>();
                if (canReplace)
                    _ids[id] = obj;
                else
                    _ids.Add(id, obj);
            }
        }

        private void DeserializeAnonymouseObject(IJson json, Type type, Action<object> setValue)
        {
            if (!(json is JsonObject))
                throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");

            var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var constructorParameters = new (bool HasValue, object Value)[constructor.GetParameters().Length];
            var properties = type.GetProperties(Options.PropertyBindingFlags);

            var i = 0;
            foreach (var property in properties)
            {
                var localIndex = i;
                TryDeserializeMember(property, property.PropertyType, json, value =>
                {
                    constructorParameters[localIndex].Value = value;
                    constructorParameters[localIndex].HasValue = true;

                    if (constructorParameters.All(x => x.HasValue))
                    {
                        var obj = constructor.Invoke(constructorParameters.Select(x => x.Value).ToArray());
                        TryAddToCache((JsonObject)json, obj);
                        setValue(obj);
                    }
                });

                i++;
            }
        }

        private void TryDeserializeMember(MemberInfo memberInfo, Type type, IJson json, Action<object> setValue)
        {
            var key = memberInfo.Name;

            if (memberInfo.TryGetCustomAttribute(out JsonPropertyAttribute jsonPropertyAttribute))
            {
                if (jsonPropertyAttribute.DeserializerIgnore)
                    return;

                if (jsonPropertyAttribute.DeserializerName != null)
                    key = jsonPropertyAttribute.DeserializerName;

                if (jsonPropertyAttribute.DeserializeToType != null)
                    type = jsonPropertyAttribute.DeserializeToType;
            }

            key = TransformKey(key);

            if (!json.ContainsKey(key))
            {
                if (Options.IgnoreMissingProperties)
                    return;
                else
                    throw new Exception($"Json does not include a field named {key}");
            }

            var jsonValue = json[key].Value;

            DeserializeValue(jsonValue, type, value =>
            {
                setValue(value);
            });
        }

        private object DeserializeArray(IJson json, Type type, bool useJsonDeserialize)
        {
            var elementType = GetElementType(type);
            var obj = CreateArray(json, type, elementType);

            return DeserializeArray(json, type, obj, elementType, useJsonDeserialize);
        }

        private object DeserializeArray(IJson json, Type type, object obj, Type elementType, bool useJsonDeserialize)
        {
            if (useJsonDeserialize && TryJsonDeserialize(obj, json))
                return obj;

            if (!(json is JsonArray))
                throw new Exception($"Json of type {json.GetType()} cannot be converted to an object of type {type}");

            var array = (IList)obj;

            for (var i = 0; i < json.Count; i++)
            {
                if (!array.IsFixedSize)
                    array.Add(null);

                var localIndex = i;
                DeserializeValue(json[i].Value, elementType, value => array[localIndex] = value);
            }

            return array;
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
            var enumerableType = type.GetInterface("IEnumerable`1");
            if (enumerableType == null)
                return typeof(object);
            else
                return enumerableType.GenericTypeArguments[0];
        }

        private IEnumerable CreateArray(IJson json, Type type, Type elementType)
        {
            if (type.IsArray)
                return Array.CreateInstance(elementType, json.Count);
            else
                return (IEnumerable)CreateObject(json, type);
        }

        private object CreateObject(IJson json, Type type)
        {
            var cctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new Type[] { typeof(IJson) }, null);
            if (cctor != null)
                return cctor.Invoke(new object[] { json });
            cctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, Type.EmptyTypes, null);
            if (cctor != null)
                return cctor.Invoke(Array.Empty<object>());
            if (type.IsValueType())
                return FormatterServices.GetUninitializedObject(type);

            throw new Exception($"No matching constructors found for object of type {type}");
        }

        protected virtual string TransformKey(string key)
        {
            return key;
        }
    }
}
