using PinkJson2.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PinkJson2.Serializers
{
    public sealed class ObjectSerializer : ISerializer
    {
        private readonly ObjectSerializerOptions _options;

        private sealed class Enumerator : IEnumerator<JsonEnumerableItem>
        {
            private const string _refName = "$ref";
            private const string _idName = "$id";
            private const string _indexerPropertyName = "Item";
            private static readonly Dictionary<Type, IKey[]> _keysCache = new Dictionary<Type, IKey[]>();
            private readonly ObjectSerializerOptions _options;
            private readonly List<object> _references;
            private readonly object _rootObject;
            private readonly Stack<object> _stack = new Stack<object>();
            private readonly Stack<State> _nextState = new Stack<State>();
            private State _state = State.SerializeJsonValue;
            private readonly bool _useJsonSerializeOnRootObject;
            private int _referenceId;

            private enum State
            {
                SerializeJsonValue,
                Enumerate,
                BeginArray,
                BeginObject,
                SerializeReferenceValue,
                EndObject,
                SerializeReferenceId,
                SerializeKeys,
                TrySelfSerialize,
                ContinueObject,
                ContinueArray,
                EndArray,
                SerializeValues,
                SerializeDictionaryStringsValues,
                Disposed,
                SerializeReference,
                PreEndObject,
                PreEndArray,
                SerializeValue
            }

            private interface IKey
            {
                string Name { get; }
                bool IsValueType { get; }

                object GetValue(object obj);
            }

            private sealed class StaticKey : IKey
            {
                private readonly object _value;

                public StaticKey(object value, string name, bool isValueType)
                {
                    Name = name;
                    IsValueType = isValueType;
                    _value = value;
                }

                public string Name { get; }
                public bool IsValueType { get; }

                public object GetValue(object obj)
                {
                    return _value;
                }
            }

            private sealed class MemberKey : IKey
            {
                private readonly MemberAccessor _memberAccessor;

                public MemberKey(MemberAccessor memberAccessor, string name, bool isValueType)
                {
                    _memberAccessor = memberAccessor;
                    Name = name;
                    IsValueType = isValueType;
                }

                public string Name { get; }
                public bool IsValueType { get; }

                public object GetValue(object obj)
                {
                    return _memberAccessor.GetValue(obj);
                }
            }

            private sealed class KeysQueue : IDisposable
            {
                private readonly IEnumerator<IKey> _enumerator;
                private readonly object _obj;

                public KeysQueue(IEnumerable<IKey> enumerable, object obj)
                {
                    _enumerator = enumerable.GetEnumerator();
                    _enumerator.MoveNext();
                    _obj = obj;
                }

                public IKey Current => _enumerator.Current;

                public bool MoveNext()
                {
                    return _enumerator.MoveNext();
                }

                public object GetCurrentValue()
                {
                    return _enumerator.Current.GetValue(_obj);
                }

                public void Dispose()
                {
                    _enumerator.Dispose();
                }
            }

            public Enumerator(object rootObject, object instance, ObjectSerializerOptions options, List<object> references, bool useJsonSerializeOnRootObject)
            {
                _rootObject = rootObject;
                _options = options;
                _references = references;
                _useJsonSerializeOnRootObject = useJsonSerializeOnRootObject;

                _nextState.Push(State.Disposed);
                _stack.Push(instance);
            }

            public JsonEnumerableItem Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
            start:

                switch (_state)
                {
                    case State.SerializeJsonValue:
                        {
                            var value = _stack.Peek();

                            if (value == null)
                            {
                                Current = new JsonEnumerableItem(JsonEnumerableItemType.Value, null);

                                _stack.Pop();
                                _state = _nextState.Pop();
                                return true;
                            }

                            var type = value.GetType();

                            if (type.IsPrimitiveType(_options.TypeConverter))
                                goto case State.SerializeValue;

                            var isJson = type.IsEqualsOrAssignableTo(typeof(IJson));
                            var isJsonEnumerable = type.IsEqualsOrAssignableTo(typeof(IEnumerable<JsonEnumerableItem>));

                            if (isJson || isJsonEnumerable)
                            {
                                IEnumerator<JsonEnumerableItem> enumerator;

                                if (isJson)
                                    enumerator = ((IJson)value).ToJsonEnumerable().GetEnumerator();
                                else
                                    enumerator = ((IEnumerable<JsonEnumerableItem>)value).GetEnumerator();

                                if (!enumerator.MoveNext())
                                    throw new Exception();
                                    
                                _stack.Pop();
                                _stack.Push(enumerator);
                                _state = State.Enumerate;
                                goto case State.Enumerate;
                            }
                            else if (type.IsArrayType())
                            {
                                goto case State.BeginArray;
                            }
                                
                            goto case State.BeginObject;
                        }

                    // Value

                    case State.SerializeValue:
                        {
                            var value = _options.TypeConverter.ChangeType(_stack.Peek(), typeof(object));
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Value, value);

                            _stack.Pop();
                            _state = _nextState.Pop();
                            return true;
                        }
                    case State.Enumerate:
                        {
                            var nextState = State.Enumerate;
                            var enumerator = (IEnumerator<JsonEnumerableItem>)_stack.Peek();
                            var isCurrentValueType = enumerator.Current.Type == JsonEnumerableItemType.Value;

                            if (isCurrentValueType)
                                _stack.Push(enumerator.Current.Value);
                            else
                                Current = enumerator.Current;

                            if (!enumerator.MoveNext())
                            {
                                enumerator.Dispose();
                                _stack.Pop();
                                nextState = _nextState.Pop();
                            }

                            if (isCurrentValueType)
                            {
                                _nextState.Push(nextState);
                                goto case State.SerializeJsonValue;
                            }
                            else
                            {
                                _state = nextState;
                            }
                            return true;
                        }
                    case State.SerializeKeys:
                        {
                            var keys = (KeysQueue)_stack.Peek();

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, keys.Current.Name);

                            _stack.Push(keys.GetCurrentValue());

                            if (keys.Current.IsValueType)
                                _state = State.SerializeValue;
                            else
                                _state = State.SerializeJsonValue;

                            if (!keys.MoveNext())
                            {
                                keys.Dispose();
                                _nextState.Push(State.PreEndObject);
                            }
                            else
                            {
                                _nextState.Push(State.SerializeKeys);
                            }
                            return true;
                        }
                    case State.SerializeDictionaryStringsValues:
                        {
                            var enumerator = (IDictionaryEnumerator)_stack.Peek();
                            var key = enumerator.Key;
                            var value = enumerator.Value;

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, key);

                            _stack.Push(value);
                            _state = State.SerializeJsonValue;

                            if (!enumerator.MoveNext())
                            {
                                enumerator.TryDispose();
                                _nextState.Push(State.PreEndObject);
                            }
                            else
                            {
                                _nextState.Push(State.SerializeDictionaryStringsValues);
                            }
                            return true;
                        }
                    case State.SerializeValues:
                        {
                            var enumerator = (IEnumerator)_stack.Peek();

                            _stack.Push(enumerator.Current);

                            if (!enumerator.MoveNext())
                            {
                                enumerator.TryDispose();
                                _nextState.Push(State.PreEndArray);
                            }
                            else
                            {
                                _nextState.Push(State.SerializeValues);
                            }
                            goto case State.SerializeJsonValue;
                        }

                    // Self serialize

                    case State.TrySelfSerialize:
                        {
                            var value = _stack.Peek();
                            var nextState = _nextState.Pop();

                            if (_stack.Count > 1 || _useJsonSerializeOnRootObject || value != _rootObject)
                            {
                                if (TryGetEnumeratorFromJsonSerializable(value, out IEnumerator<JsonEnumerableItem> enumerator))
                                {
                                    if (!enumerator.MoveNext())
                                        throw new Exception();

#if !USELOOPDETECTING
                                    _stack.Pop();
#endif
                                    _stack.Push(enumerator);

                                    if (enumerator.Current.Type == JsonEnumerableItemType.ObjectBegin)
                                        TryAddReference(value);

                                    if (enumerator.Current.Type == JsonEnumerableItemType.ArrayBegin || enumerator.Current.Type == JsonEnumerableItemType.ObjectBegin)
                                    {
                                        _state = State.Enumerate;
                                        goto case State.Enumerate;
                                    }
                                    else if (enumerator.Current.Type == JsonEnumerableItemType.Value)
                                    {
                                        Current = enumerator.Current;
                                        return true;
                                    }
                                    else
                                    {
                                        throw new Exception();
                                    }
                                }

                                if (TryGetKeysFromSerializable(value, out KeysQueue queue))
                                {
#if !USELOOPDETECTING
                                    _stack.Pop();
#endif
                                    _stack.Push(queue);

                                    TryAddReference(value);

                                    Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                                    _nextState.Push(State.SerializeKeys);
                                    _state = State.SerializeReferenceId;
                                    return true;
                                }
                            }

                            _state = nextState;
                            goto start;
                        }

                    // References

                    case State.SerializeReference:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, _refName);
                        _state = State.SerializeReferenceValue;
                        return true;
                    case State.SerializeReferenceId:
                        if (!_options.PreserveObjectsReferences)
                        {
                            _state = _nextState.Pop();
                            goto start;
                        }

                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, _idName);
                        _state = State.SerializeReferenceValue;
                        return true;
                    case State.SerializeReferenceValue:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Value, _referenceId);
                        _state = _nextState.Pop();
                        return true;

                    // Array

                    case State.BeginArray:
                        _nextState.Push(State.ContinueArray);
                        goto case State.TrySelfSerialize;
                    case State.ContinueArray:
                        {
                            var value =
#if USELOOPDETECTING
                                _stack.Peek();
#else
                                _stack.Pop();
#endif

                            var enumerator = ((IEnumerable)value).GetEnumerator();
                            _stack.Push(enumerator);

                            if (!enumerator.MoveNext())
                            {
                                Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);
                                _state = State.EndArray;
                                return true;
                            }

                            var type = value.GetType();

                            if (type.IsEqualsOrAssignableTo(typeof(IDictionary)))
                            {
                                var keyType = ((IDictionaryEnumerator)enumerator).Key.GetType();

                                if (keyType == typeof(string))
                                {
                                    Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                                    _state = State.SerializeDictionaryStringsValues;
                                    return true;
                                }
                            }

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);
                            _state = State.SerializeValues;
                            return true;
                        }
                    case State.PreEndArray:
#if USELOOPDETECTING
                        _stack.Pop();
#endif
                        goto case State.EndArray;
                    case State.EndArray:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);
                        _stack.Pop();
                        _state = _nextState.Pop();
                        return true;

                    // Object

                    case State.BeginObject:
                        {
                            var value = _stack.Peek();

                            if (_options.PreserveObjectsReferences)
                            {
                                _referenceId = _references.IndexOf(value);

                                if (_referenceId != -1)
                                {
                                    Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                                    _nextState.Push(State.EndObject);
                                    _state = State.SerializeReference;
                                    return true;
                                }
                            }
                            else
                            {
#if USELOOPDETECTING
                                if (_stack.Skip(1).Contains(value))
                                    throw new JsonSerializationException($"Self referencing loop detected");
#endif
                            }

                            _nextState.Push(State.ContinueObject);
                            goto case State.TrySelfSerialize;
                        }
                    case State.ContinueObject:
                        {
                            var value =
#if USELOOPDETECTING
                                _stack.Peek();
#else
                                _stack.Pop();
#endif

                            TryAddReference(value);

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                            if (TryGetKeysFromObject(value, out KeysQueue queue))
                            {
                                _nextState.Push(State.SerializeKeys);
                                _stack.Push(queue);
                            }
                            else
                            {
                                _nextState.Push(State.EndObject);
                            }
                            _state = State.SerializeReferenceId;
                            return true;
                        }
                    case State.PreEndObject:
#if USELOOPDETECTING
                        _stack.Pop();
#endif
                        goto case State.EndObject;
                    case State.EndObject:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);
                        _stack.Pop();
                        _state = _nextState.Pop();
                        return true;
                }

                Dispose();
                return false;
            }

            private void TryAddReference(object obj)
            {
                if (_options.PreserveObjectsReferences)
                {
                    _referenceId = _references.Count;
                    _references.Add(obj);
                }
            }

            private bool TryGetEnumeratorFromJsonSerializable(object obj, out IEnumerator<JsonEnumerableItem> enumerator)
            {
                if (obj is IJsonSerializable serializable)
                {
                    enumerator = serializable.Serialize(new InternalSerializer(obj, _options, _references)).GetEnumerator();
                    return true;
                }

                enumerator = null;
                return false;
            }

            private static bool IsCustomSerializableType(Type type)
            {
                return type.Name == "Dictionary`2";
            }

            private bool TryGetKeysFromSerializable(object obj, out KeysQueue queue)
            {
                var type = obj.GetType();

                if (IsCustomSerializableType(type) || !(obj is ISerializable serializable))
                {
                    queue = null;
                    return false;
                }

                var formatter = new FormatterConverter();
                var info = new SerializationInfo(obj.GetType(), formatter);

                serializable.GetObjectData(info, new StreamingContext());

                if (info.MemberCount == 0)
                {
                    queue = null;
                    return false;
                }

                queue = new KeysQueue(GetKeysFromSerializationInfo(info), null);
                return true;
            }

            private IEnumerable<IKey> GetKeysFromSerializationInfo(SerializationInfo info)
            {
                foreach (var prop in info)
                    yield return new StaticKey(prop.Value, _options.KeyTransformer.TransformKey(prop.Name), false);
            }

            private bool TryGetKeysFromObject(object obj, out KeysQueue queue)
            {
                var type = obj.GetType();

                if (_keysCache.TryGetValue(type, out IKey[] keys))
                {
                    if (keys == null)
                    {
                        queue = null;
                        return false;
                    }
                }
                else
                {
                    var properties = type.GetProperties(_options.PropertyBindingFlags);
                    var fields = type.GetFields(_options.FieldBindingFlags);
                    var keysList = new List<IKey>();

                    foreach (var property in properties)
                        if (property.CanRead && property.Name != _indexerPropertyName && TryGetKeyFromMember(new MemberAccessor(property), out var key))
                            keysList.Add(key);

                    foreach (var field in fields)
                        if (TryGetKeyFromMember(new MemberAccessor(field), out var key))
                            keysList.Add(key);

                    if (keysList.Count == 0)
                    {
                        queue = null;
                        _keysCache.Add(type, null);
                        return false;
                    }

                    keys = keysList.ToArray();
                    _keysCache.Add(type, keys);
                }

                queue = new KeysQueue(keys, obj);
                return true;
            }
            
            private bool TryGetKeyFromMember(MemberAccessor member, out MemberKey key)
            {
                if (member.MemberInfo.TryGetCustomAttribute<NonSerializedAttribute>(out _))
                {
                    key = null;
                    return false;
                }

                var name = member.MemberInfo.Name;
                var isValueType = false;

                if (member.MemberInfo.TryGetCustomAttribute(out JsonPropertyAttribute jsonProperty))
                {
                    if (jsonProperty.SerializerIgnore)
                    {
                        key = null;
                        return false;
                    }

                    if (jsonProperty.SerializerName != null)
                        name = jsonProperty.SerializerName;

                    isValueType = jsonProperty.IsValueType;
                }

                name = _options.KeyTransformer.TransformKey(name);

                key = new MemberKey(member, name, isValueType);
                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }

        private sealed class Enumerable : IEnumerable<JsonEnumerableItem>
        {
            private readonly object _rootObject;
            private readonly object _instance;
            private readonly ObjectSerializerOptions _options;
            private readonly List<object> _references;
            private readonly bool _useJsonSerializeOnRootObject;

            public Enumerable(object rootObject, object instance, ObjectSerializerOptions options, List<object> references, bool useJsonSerializeOnRootObject)
            {
                _rootObject = rootObject;
                _instance = instance;
                _options = options;
                _references = references;
                _useJsonSerializeOnRootObject = useJsonSerializeOnRootObject;
            }

            public IEnumerator<JsonEnumerableItem> GetEnumerator()
            {
                return new Enumerator(_rootObject, _instance, _options, _references ?? new List<object>(), _useJsonSerializeOnRootObject);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class InternalSerializer : ISerializer
        {
            private readonly object _rootObject;
            private readonly ObjectSerializerOptions _options;
            private readonly List<object> _references;

            public InternalSerializer(object rootObject, ObjectSerializerOptions options, List<object> references)
            {
                _rootObject = rootObject;
                _options = options;
                _references = references;
            }

            public IEnumerable<JsonEnumerableItem> Serialize(object instance)
            {
                return new Enumerable(_rootObject, instance, _options, _references, false);
            }
        }

        public ObjectSerializer() : this(ObjectSerializerOptions.Default)
        {
        }

        public ObjectSerializer(ObjectSerializerOptions options)
        {
            _options = options;
        }

        public IEnumerable<JsonEnumerableItem> Serialize(object instance)
        {
            return new Enumerable(null, instance, _options, null, true);
        }
    }
}
