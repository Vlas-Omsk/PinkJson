using PinkJson2.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            private State _state = State.SerializeJsonValueAndSetNextState;
            private readonly bool _useJsonSerializeOnRootObject;
            private int _referenceId;

            private enum State
            {
                Enumerate,
                SerializeReferenceValue,
                EndObject,
                SerializeReferenceId,
                SerializeKeys,
                BeginObject,
                BeginArray,
                EndArray,
                SerializeValues,
                SerializeDictionaryValues,
                Disposed,
                SerializeReference,
                BeforeEndObject,
                BeforeEndArray,
                SerializeJsonValueAndSetNextState,
                SerializeValueAndSetNextState
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
                switch (_state)
                {
                    case State.SerializeJsonValueAndSetNextState:
                        Current = SerializeJsonValue(_stack.Pop());
                        return true;

                    // Value

                    case State.SerializeValueAndSetNextState:
                        Current = SerializeValue(_stack.Pop());
                        return true;
                    case State.Enumerate:
                        Current = Enumerate();
                        return true;
                    case State.SerializeKeys:
                        Current = SerializeKeys();
                        return true;
                    case State.SerializeDictionaryValues:
                        Current = SerializeDictionaryValues();
                        return true;
                    case State.SerializeValues:
                        Current = SerializeValues();
                        return true;

                    // References

                    case State.SerializeReference:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, _options.RefName);
                        _state = State.SerializeReferenceValue;
                        return true;
                    case State.SerializeReferenceId:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, _options.IdName);
                        _state = State.SerializeReferenceValue;
                        return true;
                    case State.SerializeReferenceValue:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Value, _referenceId);
                        SetNextState();
                        return true;

                    // Array

                    case State.BeginArray:
                        Current = BeginArray(_stack.Pop());
                        return true;
                    case State.BeforeEndArray:
#if USELOOPDETECTING
                        _stack.Pop();
#endif
                        goto case State.EndArray;
                    case State.EndArray:
                        Current = EndArray();
                        return true;

                    // Object

                    case State.BeginObject:
                        Current = BeginObject(_stack.Pop());
                        return true;
                    case State.BeforeEndObject:
#if USELOOPDETECTING
                        _stack.Pop();
#endif
                        goto case State.EndObject;
                    case State.EndObject:
                        Current = EndObject();
                        return true;
                }

                return false;
            }

            private JsonEnumerableItem SerializeJsonValue(object value)
            {
                if (value == null)
                {
                    SetNextState();
                    return new JsonEnumerableItem(JsonEnumerableItemType.Value, null);
                }

                var type = value.GetType();

                if (_options.TypeConverter.IsPrimitiveType(type))
                    return SerializeValue(value);

                if (TryGetJsonEnumerator(value, type, out var enumerator))
                {
                    if (!enumerator.MoveNext())
                        throw new UnexpectedEndOfJsonEnumerableException();

                    _stack.Push(enumerator);
                    return Enumerate();
                }
                else if (type.IsArrayType())
                {
                    return BeforeBeginArray(value);
                }
                else
                {
                    return BeforeBeginObject(value);
                }
            }

            private JsonEnumerableItem SerializeValue(object value)
            {
                SetNextState();
                value = _options.TypeConverter.ChangeType(value, typeof(object));
                return new JsonEnumerableItem(JsonEnumerableItemType.Value, value);
            }

            private JsonEnumerableItem Enumerate()
            {
                var enumerator = (IEnumerator<JsonEnumerableItem>)_stack.Peek();
                var item = enumerator.Current;

                if (!enumerator.MoveNext())
                {
                    enumerator.Dispose();
                    _stack.Pop();
                    _state = _nextState.Pop();
                }

                if (item.Type == JsonEnumerableItemType.Value)
                {
                    _nextState.Push(State.Enumerate);
                    return SerializeJsonValue(item.Value);
                }

                _state = State.Enumerate;
                return item;
            }

            private JsonEnumerableItem SerializeKeys()
            {
                var keys = (KeysQueue)_stack.Peek();
                var current = new JsonEnumerableItem(JsonEnumerableItemType.Key, keys.Current.Name);

                _stack.Push(keys.GetCurrentValue());

                if (keys.Current.IsValueType)
                    _state = State.SerializeValueAndSetNextState;
                else
                    _state = State.SerializeJsonValueAndSetNextState;

                if (!keys.MoveNext())
                {
                    keys.Dispose();
                    _nextState.Push(State.BeforeEndObject);
                }
                else
                {
                    _nextState.Push(State.SerializeKeys);
                }
                return current;
            }

            private JsonEnumerableItem SerializeDictionaryValues()
            {
                var enumerator = (IDictionaryEnumerator)_stack.Peek();
                var current = new JsonEnumerableItem(JsonEnumerableItemType.Key, enumerator.Key);

                _stack.Push(enumerator.Value);
                _state = State.SerializeJsonValueAndSetNextState;

                if (!enumerator.MoveNext())
                {
                    enumerator.TryDispose();
                    _nextState.Push(State.BeforeEndObject);
                }
                else
                {
                    _nextState.Push(State.SerializeDictionaryValues);
                }
                return current;
            }

            private JsonEnumerableItem SerializeValues()
            {
                var enumerator = (IEnumerator)_stack.Peek();
                var current = enumerator.Current;

                if (!enumerator.MoveNext())
                {
                    enumerator.TryDispose();
                    _nextState.Push(State.BeforeEndArray);
                }
                else
                {
                    _nextState.Push(State.SerializeValues);
                }
                return SerializeJsonValue(current);
            }

            private bool TrySelfSerialize(object value, out JsonEnumerableItem item)
            {
                if (_stack.Count <= 1 && !_useJsonSerializeOnRootObject && value == _rootObject)
                {
                    item = default;
                    return false;
                }

                if (TryGetEnumeratorFromJsonSerializable(value, out IEnumerator<JsonEnumerableItem> enumerator))
                {
                    if (!enumerator.MoveNext())
                        throw new UnexpectedEndOfJsonEnumerableException();

                    if (
                        enumerator.Current.Type == JsonEnumerableItemType.ArrayBegin ||
                        enumerator.Current.Type == JsonEnumerableItemType.ObjectBegin
                    )
                    {
                        _stack.Push(enumerator);
                        item = Enumerate();
                        return true;
                    }
                    else if (enumerator.Current.Type == JsonEnumerableItemType.Value)
                    {
                        var current = enumerator.Current.Value;

                        if (enumerator.MoveNext())
                            throw new UnexpectedJsonEnumerableItemException(enumerator.Current, Array.Empty<JsonEnumerableItemType>());

                        enumerator.Dispose();

                        item = SerializeJsonValue(current);
                        return true;
                    }
                    else
                    {
                        throw new UnexpectedJsonEnumerableItemException(
                            enumerator.Current,
                            new JsonEnumerableItemType[]
                            {
                                JsonEnumerableItemType.ArrayBegin,
                                JsonEnumerableItemType.ObjectBegin,
                                JsonEnumerableItemType.Value
                            }
                        );
                    }
                }

                if (TryGetKeysFromSerializable(value, out KeysQueue queue))
                {
                    _stack.Push(queue);

                    AddReferenceIfNeeded(value);
                    SetStateOrAddReferenceId(State.SerializeKeys);

                    item = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                    return true;
                }

                item = default;
                return false;
            }

            private JsonEnumerableItem BeforeBeginArray(object value)
            {
#if USELOOPDETECTING
                _stack.Push(value);
#endif

                if (TrySelfSerialize(value, out var item))
                    return item;

                return BeginArray(value);
            }

            private JsonEnumerableItem BeginArray(object value)
            {
                var enumerator = ((IEnumerable)value).GetEnumerator();
                _stack.Push(enumerator);

                if (!enumerator.MoveNext())
                {
                    _state = State.EndArray;
                    return new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);
                }

                var type = value.GetType();

                if (type.IsEqualsOrAssignableTo(typeof(IDictionary)))
                {
                    var keyType = ((IDictionaryEnumerator)enumerator).Key.GetType();

                    if (keyType == typeof(string))
                    {
                        _state = State.SerializeDictionaryValues;
                        return new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                    }
                }

                _state = State.SerializeValues;
                return new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);
            }

            private JsonEnumerableItem EndArray()
            {
                _stack.Pop();
                SetNextState();
                return new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);
            }

            private JsonEnumerableItem BeforeBeginObject(object value)
            {
#if USELOOPDETECTING
                _stack.Push(value);
#endif

                if (_options.PreserveObjectsReferences)
                {
                    _referenceId = _references.IndexOf(value);

                    if (_referenceId != -1)
                    {
                        _nextState.Push(State.EndObject);
                        _state = State.SerializeReference;
                        return new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                    }
                }
                else
                {
#if USELOOPDETECTING
                    if (_stack.Skip(1).Contains(value))
                        throw new JsonSerializationException($"Self referencing loop detected");
#endif
                }

                if (TrySelfSerialize(value, out var item))
                    return item;

                return BeginObject(value);
            }

            private JsonEnumerableItem BeginObject(object value)
            {
                AddReferenceIfNeeded(value);

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
                return new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
            }
            
            private JsonEnumerableItem EndObject()
            {
                _stack.Pop();
                SetNextState();
                return new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);
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
                _state = State.SerializeJsonValueAndSetNextState;
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

            private static bool TryGetJsonEnumerator(object value, Type type, out IEnumerator<JsonEnumerableItem> enumerator)
            {
                if (type.IsEqualsOrAssignableTo(typeof(IJson)))
                {
                    enumerator = ((IJson)value).ToJsonEnumerable().GetEnumerator();
                    return true;
                }
                else if (type.IsEqualsOrAssignableTo(typeof(IEnumerable<JsonEnumerableItem>)))
                {
                    enumerator = ((IEnumerable<JsonEnumerableItem>)value).GetEnumerator();
                    return true;
                }

                enumerator = null;
                return false;
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
                var hash = type.GetHashCode();

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
