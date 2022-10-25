using PinkJson2.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PinkJson2.Serializers
{
    public sealed class ObjectSerializer : ISerializer
    {
        private readonly ObjectSerializerOptions _options;

        private sealed class Enumerator : IEnumerator<JsonEnumerableItem>
        {
            private const string _indexerPropertyName = "Item";
            private static readonly ConcurrentDictionary<int, List<IKey>> _keysCache = new ConcurrentDictionary<int, List<IKey>>();
            private readonly ObjectSerializerOptions _options;
            private readonly object _rootObject;
            private readonly List<object> _references;
            private readonly Stack<object> _stack = new Stack<object>();
            private readonly Stack<State> _nextState = new Stack<State>();
            private State _state = State.SerializeJsonValue;
            private readonly bool _useJsonSerializeOnRootObject;
            private int _referenceId;

            private enum State
            {
                SerializeJsonValue,
                Enumerate,
                BeforeBeginArray,
                BeforeBeginObject,
                SerializeReferenceValue,
                EndObject,
                SerializeReferenceId,
                SerializeKeys,
                TrySelfSerialize,
                BeginObject,
                BeginArray,
                EndArray,
                SerializeValues,
                SerializeDictionaryValues,
                Disposed,
                SerializeReference,
                BeforeEndObject,
                BeforeEndArray,
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
                                SetNextState();
                                return true;
                            }

                            var type = value.GetType();

                            if (_options.TypeConverter.IsPrimitiveType(type))
                                goto case State.SerializeValue;

                            var enumerator = TryGetJsonEnumerator(value, type);

                            if (enumerator != null)
                            {
                                if (!enumerator.MoveNext())
                                    throw new Exception();

                                _stack.Pop();
                                _stack.Push(enumerator);
                                _state = State.Enumerate;
                                goto case State.Enumerate;
                            }
                            else if (type.IsArrayType())
                            {
                                goto case State.BeforeBeginArray;
                            }
                            else
                            {
                                goto case State.BeforeBeginObject;
                            }
                        }

                    // Value

                    case State.SerializeValue:
                        {
                            var value = _options.TypeConverter.ChangeType(_stack.Pop(), typeof(object));
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Value, value);
                            SetNextState();
                            return true;
                        }
                    case State.Enumerate:
                        {
                            var nextState = State.Enumerate;
                            var enumerator = (IEnumerator<JsonEnumerableItem>)_stack.Peek();
                            var item = enumerator.Current;

                            if (!enumerator.MoveNext())
                            {
                                enumerator.Dispose();
                                _stack.Pop();
                                nextState = _nextState.Pop();
                            }

                            if (item.Type == JsonEnumerableItemType.Value)
                            {
                                _stack.Push(item.Value);
                                _nextState.Push(nextState);
                                goto case State.SerializeJsonValue;
                            }

                            Current = item;
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
                                _nextState.Push(State.BeforeEndObject);
                            }
                            else
                            {
                                _nextState.Push(State.SerializeKeys);
                            }
                            return true;
                        }
                    case State.SerializeDictionaryValues:
                        {
                            var enumerator = (IDictionaryEnumerator)_stack.Peek();
                            
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, enumerator.Key);

                            _stack.Push(enumerator.Value);
                            _state = State.SerializeJsonValue;

                            if (!enumerator.MoveNext())
                                _nextState.Push(State.BeforeEndObject);
                            else
                                _nextState.Push(State.SerializeDictionaryValues);
                            return true;
                        }
                    case State.SerializeValues:
                        {
                            var enumerator = (IEnumerator)_stack.Peek();

                            _stack.Push(enumerator.Current);

                            if (!enumerator.MoveNext())
                                _nextState.Push(State.BeforeEndArray);
                            else
                                _nextState.Push(State.SerializeValues);
                            goto case State.SerializeJsonValue;
                        }

                    // Self serialize

                    case State.TrySelfSerialize:
                        {
                            var value = _stack.Peek();
                            SetNextState();

                            if (_stack.Count <= 1 && !_useJsonSerializeOnRootObject && value == _rootObject)
                                goto start;

                            if (TryGetEnumeratorFromJsonSerializable(value, out IEnumerator<JsonEnumerableItem> enumerator))
                            {
                                if (!enumerator.MoveNext())
                                    throw new Exception();

#if !USELOOPDETECTING
                                _stack.Pop();
#endif

                                if (enumerator.Current.Type == JsonEnumerableItemType.ArrayBegin || enumerator.Current.Type == JsonEnumerableItemType.ObjectBegin)
                                {
                                    _stack.Push(enumerator);
                                    _state = State.Enumerate;
                                    goto case State.Enumerate;
                                }
                                else if (enumerator.Current.Type == JsonEnumerableItemType.Value)
                                {
                                    _stack.Push(enumerator.Current.Value);

                                    if (enumerator.MoveNext())
                                        throw new Exception();

                                    enumerator.Dispose();

                                    goto case State.SerializeJsonValue;
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

                                AddReferenceIfNeeded(value);

                                Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                                SetStateOrAddReferenceId(State.SerializeKeys);
                                return true;
                            }

                            goto start;
                        }

                    // References

                    case State.SerializeReference:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, Serializer.RefName);
                        _state = State.SerializeReferenceValue;
                        return true;
                    case State.SerializeReferenceId:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, Serializer.IdName);
                        _state = State.SerializeReferenceValue;
                        return true;
                    case State.SerializeReferenceValue:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Value, _referenceId);
                        SetNextState();
                        return true;

                    // Array

                    case State.BeforeBeginArray:
                        _nextState.Push(State.BeginArray);
                        goto case State.TrySelfSerialize;
                    case State.BeginArray:
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
                                    _state = State.SerializeDictionaryValues;
                                    return true;
                                }
                            }

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);
                            _state = State.SerializeValues;
                            return true;
                        }
                    case State.BeforeEndArray:
#if USELOOPDETECTING
                        _stack.Pop();
#endif
                        goto case State.EndArray;
                    case State.EndArray:
                        {
                            var enumerator = (IEnumerator)_stack.Pop();
                            enumerator.TryDispose();

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);
                            SetNextState();
                        }
                        return true;

                    // Object

                    case State.BeforeBeginObject:
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

                            _nextState.Push(State.BeginObject);
                            goto case State.TrySelfSerialize;
                        }
                    case State.BeginObject:
                        {
                            var value =
#if USELOOPDETECTING
                                _stack.Peek();
#else
                                _stack.Pop();
#endif

                            AddReferenceIfNeeded(value);

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);

                            State nextState;

                            if (TryGetKeysFromObject(value, out KeysQueue queue))
                            {
                                nextState = State.SerializeKeys;
                                _stack.Push(queue);
                            }
                            else
                            {
                                nextState = State.EndObject;
                            }

                            SetStateOrAddReferenceId(nextState);
                            return true;
                        }
                    case State.BeforeEndObject:
#if USELOOPDETECTING
                        _stack.Pop();
#endif
                        goto case State.EndObject;
                    case State.EndObject:
                        {
                            _stack.Pop();
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);
                            SetNextState();
                        }
                        return true;
                }

                return false;
            }

            private void SetNextState()
            {
                _state = _nextState.Pop();
            }

            public void Reset()
            {
                if (_state == State.Disposed)
                    throw new ObjectDisposedException(GetType().FullName);

                _references.Clear();
                _stack.Clear();
                _nextState.Clear();

                Current = default;
                _state = State.SerializeJsonValue;
            }

            public void Dispose()
            {
                if (_state == State.Disposed)
                    return;

                _references.Clear();
                _stack.Clear();
                _nextState.Clear();

                Current = default;
                _state = State.Disposed;
            }

            private void SetStateOrAddReferenceId(State state)
            {
                if (_options.PreserveObjectsReferences)
                {
                    _nextState.Push(state);
                    _state = State.SerializeReferenceId;
                }
                else
                {
                    _state = state;
                }
            }

            private static IEnumerator<JsonEnumerableItem> TryGetJsonEnumerator(object value, Type type)
            {
                IEnumerator<JsonEnumerableItem> enumerator = null;

                if (type.IsEqualsOrAssignableTo(typeof(IJson)))
                    enumerator = ((IJson)value).ToJsonEnumerable().GetEnumerator();
                else if (type.IsEqualsOrAssignableTo(typeof(IEnumerable<JsonEnumerableItem>)))
                    enumerator = ((IEnumerable<JsonEnumerableItem>)value).GetEnumerator();

                return enumerator;
            }

            private void AddReferenceIfNeeded(object obj)
            {
                if (!_options.PreserveObjectsReferences)
                    return;

                _referenceId = _references.Count;
                _references.Add(obj);
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
                var hash = type.GetHashCodeCached();

                if (_keysCache.TryGetValue(hash, out List<IKey> keys))
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
                    keys = new List<IKey>();

                    foreach (var property in properties)
                        if (property.CanRead && property.Name != _indexerPropertyName && TryGetKeyFromMember(new MemberAccessor(property), out var key))
                            keys.Add(key);

                    foreach (var field in fields)
                        if (TryGetKeyFromMember(new MemberAccessor(field), out var key))
                            keys.Add(key);

                    if (keys.Count == 0)
                    {
                        queue = null;
                        _keysCache.TryAdd(hash, null);
                        return false;
                    }

                    keys.TrimExcess();

                    _keysCache.TryAdd(hash, keys);
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
