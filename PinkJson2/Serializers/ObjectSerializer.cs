using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            private static readonly string[] _customSerializableTypes =
            {
                "Dictionary`2"
            };
            private readonly ObjectSerializerOptions _options;
            private readonly List<object> _references;
            private readonly object _rootObject;
            private readonly Stack<object> _stack = new Stack<object>();
            private readonly Stack<State> _nextState = new Stack<State>();
            private State _state = State.SerializeValue;
            private readonly bool _useJsonSerializeOnRootObject;
            private int _referenceId;
            private Type _keyType;

            private enum State
            {
                SerializeValue,
                Enumerating,
                BeginArray,
                BeginObject,
                SerializeReferenceValue,
                EndObject,
                SerializeReferenceId,
                SerializeKeys,
                JsonSerialize,
                ContinueObject,
                ContinueArray,
                EndArray,
                SerializeValues,
                SerializeDictionaryStringsValues,
                Disposed,
                SerializeReference,
                PreEndObject,
                PreEndArray,
                SerializeNativeValue
            }

            private abstract class Key
            {
                public Key(string name, bool isValueType)
                {
                    Name = name;
                    IsValueType = isValueType;
                }

                public string Name { get; }
                public bool IsValueType { get; }

                public abstract object GetValue();
            }

            private sealed class StaticKey : Key
            {
                private readonly object _value;

                public StaticKey(object value, string name, bool isValueType) : base(name, isValueType)
                {
                    _value = value;
                }

                public override object GetValue()
                {
                    return _value;
                }
            }

            private sealed class MemberKey : Key
            {
                private readonly IMemberInfoWrapper _memberInfo;
                private readonly object _obj;

                public MemberKey(IMemberInfoWrapper memberInfo, object obj, string name, bool isValueType) : base(name, isValueType)
                {
                    _memberInfo = memberInfo;
                    _obj = obj;
                }

                public override object GetValue()
                {
                    return _memberInfo.GetValue(_obj);
                }
            }

            private interface IMemberInfoWrapper
            {
                MemberInfo MemberInfo { get; }

                object GetValue(object obj);
            }

            private sealed class PropertyInfoWrapper : IMemberInfoWrapper
            {
                private readonly PropertyInfo _propertyInfo;

                public PropertyInfoWrapper(PropertyInfo propertyInfo)
                {
                    _propertyInfo = propertyInfo;
                }

                public MemberInfo MemberInfo => _propertyInfo;

                public object GetValue(object obj)
                {
                    return _propertyInfo.GetValue(obj);
                }
            }

            private sealed class FieldInfoWrapper : IMemberInfoWrapper
            {
                private readonly FieldInfo _fieldInfo;

                public FieldInfoWrapper(FieldInfo fieldInfo)
                {
                    _fieldInfo = fieldInfo;
                }

                public MemberInfo MemberInfo => _fieldInfo;

                public object GetValue(object obj)
                {
                    return _fieldInfo.GetValue(obj);
                }
            }

            public Enumerator(object rootObject, object instance, ObjectSerializerOptions options, List<object> references, bool useJsonSerializeOnRootObject)
            {
                _rootObject = rootObject;
                _nextState.Push(State.Disposed);
                _stack.Push(instance);
                _options = options;
                _references = references;
                _useJsonSerializeOnRootObject = useJsonSerializeOnRootObject;
            }

            public JsonEnumerableItem Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
            start:

                switch (_state)
                {
                    case State.SerializeValue:
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

                            if (value.GetType().IsAssignableTo(type))
                                type = value.GetType();

                            if (!type.IsPrimitiveType(_options.TypeConverter))
                            {
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
                                    _state = State.Enumerating;
                                    goto case State.Enumerating;
                                }
                                else if (type.IsArrayType())
                                {
                                    goto case State.BeginArray;
                                }
                                
                                goto case State.BeginObject;
                            }

                            goto case State.SerializeNativeValue;
                        }
                    case State.SerializeNativeValue:
                        {
                            var value = _stack.Peek();
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Value, _options.TypeConverter.ChangeType(value, typeof(object)));

                            _stack.Pop();
                            _state = _nextState.Pop();
                            return true;
                        }
                    case State.Enumerating:
                        {
                            var nextState = State.Enumerating;
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
                                goto case State.SerializeValue;
                            }
                            else
                            {
                                _state = nextState;
                            }
                            return true;
                        }
                    case State.JsonSerialize:
                        {
                            var value = _stack.Peek();
                            var nextState = _nextState.Pop();

                            if (_stack.Count > 1 || _useJsonSerializeOnRootObject || value != _rootObject)
                            {
                                if (TryJsonSerialize(value, out IEnumerator<JsonEnumerableItem> enumerator))
                                {
                                    if (!enumerator.MoveNext())
                                        throw new Exception();

#if !USELOOPDETECTING
                                    _stack.Pop();
#endif
                                    _stack.Push(enumerator);

                                    if (enumerator.Current.Type == JsonEnumerableItemType.ObjectBegin)
                                        AddReference(value);

                                    if (enumerator.Current.Type == JsonEnumerableItemType.ArrayBegin || enumerator.Current.Type == JsonEnumerableItemType.ObjectBegin)
                                    {
                                        _state = State.Enumerating;
                                        goto case State.Enumerating;
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

                                if (TrySerializable(value, out Queue<Key> keys))
                                {
#if !USELOOPDETECTING
                                    _stack.Pop();
#endif
                                    _stack.Push(keys);

                                    AddReference(value);

                                    Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                                    _nextState.Push(State.SerializeKeys);
                                    _state = State.SerializeReferenceId;
                                    return true;
                                }
                            }

                            _state = nextState;
                            goto start;
                        }
                    case State.BeginArray:
                        _nextState.Push(State.ContinueArray);
                        goto case State.JsonSerialize;
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
                                _keyType = ((IDictionaryEnumerator)enumerator).Key.GetType();

                                if (_keyType == typeof(string))
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
                    case State.EndArray:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);
                        _stack.Pop();
                        _state = _nextState.Pop();
                        return true;
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
                            goto case State.JsonSerialize;
                        }
                    case State.ContinueObject:
                        {
                            var value =
#if USELOOPDETECTING
                                _stack.Peek();
#else
                                _stack.Pop();
#endif

                            AddReference(value);

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                            if (TryPushKeys(value))
                                _nextState.Push(State.SerializeKeys);
                            else
                                _nextState.Push(State.EndObject);
                            _state = State.SerializeReferenceId;
                            return true;
                        }
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
                    case State.PreEndObject:
#if USELOOPDETECTING
                        _stack.Pop();
#endif
                        goto case State.EndObject;
                    case State.PreEndArray:
#if USELOOPDETECTING
                        _stack.Pop();
#endif
                        goto case State.EndArray;
                    case State.EndObject:
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);
                        _stack.Pop();
                        _state = _nextState.Pop();
                        return true;
                    case State.SerializeKeys:
                        {
                            var keys = (Queue<Key>)_stack.Peek();
                            var key = keys.Dequeue();

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, key.Name);
                            _stack.Push(key.GetValue());

                            if (key.IsValueType)
                                _state = State.SerializeNativeValue;
                            else
                                _state = State.SerializeValue;

                            if (keys.Count == 0)
                                _nextState.Push(State.PreEndObject);
                            else
                                _nextState.Push(State.SerializeKeys);
                            return true;
                        }
                    case State.SerializeDictionaryStringsValues:
                        {
                            var enumerator = (IDictionaryEnumerator)_stack.Peek();
                            var key = enumerator.Key;
                            var value = enumerator.Value;

                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, key);
                            _stack.Push(value);
                            _state = State.SerializeValue;

                            if (!enumerator.MoveNext())
                            {
                                DisposeArrayEnumerator(enumerator);
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
                                DisposeArrayEnumerator(enumerator);
                                _nextState.Push(State.PreEndArray);
                            }
                            else
                            {
                                _nextState.Push(State.SerializeValues);
                            }
                            goto case State.SerializeValue;
                        }
                }

                Dispose();
                return false;
            }

            private bool TryJsonSerialize(object obj, out IEnumerator<JsonEnumerableItem> enumerator)
            {
                if (obj is IJsonSerializable serializable)
                {
                    enumerator = serializable.Serialize(new InternalSerializer(obj, _options, _references)).GetEnumerator();
                    return true;
                }

                enumerator = null;
                return false;
            }

            private bool TrySerializable(object obj, out Queue<Key> keys)
            {
                var type = obj.GetType();

                if (type.Name == "Dictionary`2" /*_customSerializableTypes.Contains(type.Name)*/ || !(obj is ISerializable serializable))
                {
                    keys = null;
                    return false;
                }

                var formatter = new FormatterConverter();
                var info = new SerializationInfo(obj.GetType(), formatter);
                serializable.GetObjectData(info, new StreamingContext());

                keys = new Queue<Key>(info.MemberCount);

                foreach (var prop in info)
                    keys.Enqueue(new StaticKey(prop.Value, _options.KeyTransformer.TransformKey(prop.Name), false));

                if (keys.Count == 0)
                    return false;

                return true;
            }

            private bool TryPushKeys(object obj)
            {
                var type = obj.GetType();

                var properties = type
                    .GetProperties(_options.PropertyBindingFlags)
                    .Where(x => x.CanRead && x.Name != _indexerPropertyName);
                var fields = type.GetFields(_options.FieldBindingFlags);

                var keys = new Queue<Key>();

                foreach (var property in properties)
                    if (TryGetKey(new PropertyInfoWrapper(property), obj, out Key key))
                        keys.Enqueue(key);
                foreach (var field in fields)
                    if (TryGetKey(new FieldInfoWrapper(field), obj, out Key key))
                        keys.Enqueue(key);

                if (keys.Count == 0)
                    return false;

                _stack.Push(keys);
                return true;
            }
            
            private bool TryGetKey(IMemberInfoWrapper member, object obj, out Key key)
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

                key = new MemberKey(member, obj, name, isValueType);
                return true;
            }

            private void DisposeArrayEnumerator(IEnumerator enumerator)
            {
                var disposeMethod = enumerator.GetType().GetMethod("Dispose");
                if (disposeMethod != null)
                    disposeMethod.Invoke(enumerator, null);
            }

            private void AddReference(object obj)
            {
                if (_options.PreserveObjectsReferences)
                {
                    _referenceId = _references.Count;
                    _references.Add(obj);
                }
            }

            public void Reset()
            {
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
