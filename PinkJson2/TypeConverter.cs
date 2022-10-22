using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PinkJson2
{
    public sealed class TypeConverter : ICloneable
    {
        private readonly static MethodInfo _enumTryParseMethodInfo;
        private readonly static TypeConversion _dateTimeTypeConversion = new TypeConversion((object obj, Type targetType, ref bool handled) =>
        {
            if (obj is string @string)
            {
                handled = true;
                return DateTime.Parse(@string);
            }
            else if (obj is long @long)
            {
                handled = true;
                return DateTime.FromBinary(@long);
            }

            return null;
        });
        private readonly static TypeConversion _enumTypeConversion = new TypeConversion((object obj, Type targetType, ref bool handled) =>
        {
            if (obj is string @string)
            {
                var parameters = new object[] { @string, true, null };
                handled = (bool)_enumTryParseMethodInfo
                    .MakeGenericMethod(new Type[] { targetType })
                    .Invoke(null, parameters);
                return parameters[2];
            }

            return null;
        });
        private readonly static TypeConversion _guidTypeConversion = new TypeConversion((object obj, Type targetType, ref bool handled) =>
        {
            if (obj is string value)
            {
                handled = true;
                return Guid.Parse(value);
            }

            return null;
        });
        private readonly Dictionary<Type, List<TypeConversion>> _registeredTypes = new Dictionary<Type, List<TypeConversion>>();
        private readonly ConcurrentDictionary<int, List<TypeConversion>> _registeredTypeConversionsCache = new ConcurrentDictionary<int, List<TypeConversion>>();
        private readonly ConcurrentDictionary<int, bool> _isPrimitiveTypeCache = new ConcurrentDictionary<int, bool>();
        private readonly ConcurrentDictionary<int, bool> _tryConvertCache = new ConcurrentDictionary<int, bool>();
        private readonly HashSet<Type> _primitiveTypes = new HashSet<Type>()
        {
            typeof(string),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(Guid)
        };

        private sealed class TryConvertCacheItem
        {
            public bool Result { get; }
            public TypeConversionType Type { get; }
            public IEnumerable<TypeConversion> Conversions { get; }
        }

        static TypeConverter()
        {
            var methods = typeof(Enum).GetMethods(BindingFlags.Static | BindingFlags.Public);
            _enumTryParseMethodInfo = methods.First(x =>
            {
                if (x.Name != nameof(Enum.TryParse))
                    return false;
                var parameters = x.GetParameters();
                if (parameters.Length < 2 ||
                    parameters[0].ParameterType != typeof(string) ||
                    parameters[1].ParameterType != typeof(bool))
                    return false;
                return true;
            });
        }

        public TypeConverter()
        {
            Register(typeof(DateTime), _dateTimeTypeConversion);
            Register(typeof(Enum), _enumTypeConversion);
            Register(typeof(Guid), _guidTypeConversion);
        }

        private TypeConverter(Dictionary<Type, List<TypeConversion>> registeredTypes)
        {
            foreach (var registeredType in registeredTypes)
                _registeredTypes.Add(registeredType.Key, new List<TypeConversion>(registeredType.Value));
        }

        public static TypeConverter Default { get; set; } = new TypeConverter();

        public bool IsPrimitiveType(Type type)
        {
            var hash = type.GetHashCode();

            if (_isPrimitiveTypeCache.TryGetValue(hash, out var result))
                return result;

            result =
                type.IsPrimitiveType() ||
                _primitiveTypes.Contains(type);

            _isPrimitiveTypeCache.TryAdd(hash, result);
            return result;
        }

        public object ChangeType(object value, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            if (value == null)
                return value;

            var valueType = value.GetType();

            if (TryConvert(value, valueType, targetType, out var targetObj))
                return targetObj;

            if (targetType == typeof(object))
                return value;

            if (valueType.IsEqualsOrAssignableTo(targetType))
                return value;

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Cannot convert value of type {value.GetType()} to type {targetType}", ex);
            }
        }

        private bool TryConvert(object obj, Type type, Type targetType, out object targetObj)
        {
            targetObj = null;

            var hash = unchecked(targetType.GetHashCode() + type.GetHashCode());

            if (_tryConvertCache.TryGetValue(hash, out _))
                return false;

            var handled = false;
            var conversions = GetRegisteredTypeConversions(targetType, true, false);

            if (conversions != null)
            {
                foreach (var typeConversion in conversions)
                {
                    targetObj = typeConversion.ConvertCallback.Invoke(obj, targetType, ref handled);
                    if (handled)
                    {
                        CompareObjectToType(targetObj, targetType);
                        return true;
                    }
                }
            }

            var backConversions = GetRegisteredTypeConversions(type, false, true);

            if (backConversions != null)
            {
                foreach (var typeConversion in backConversions)
                {
                    targetObj = typeConversion.ConvertBackCallback.Invoke(obj, targetType, ref handled);
                    if (handled)
                    {
                        CompareObjectToType(targetObj, targetType);
                        return true;
                    }
                }
            }

            if (conversions == null && backConversions == null)
                _tryConvertCache.TryAdd(hash, false);
            return false;
        }

        private IEnumerable<TypeConversion> GetRegisteredTypeConversions(Type type, bool convertCallback, bool convertBackCallback)
        {
            var hash = unchecked(type.GetHashCode() + (convertCallback ? 1 : 0) + (convertBackCallback ? 2 : 0));

            if (_registeredTypeConversionsCache.TryGetValue(hash, out List<TypeConversion> collectedConversions))
                return collectedConversions;

            collectedConversions = new List<TypeConversion>();
            var currentType = type;

            while (currentType != null)
            {
                foreach (var keyValue in _registeredTypes)
                    if (currentType == keyValue.Key)
                    {
                        var conversions = (IEnumerable<TypeConversion>)keyValue.Value;

                        if (convertCallback)
                            conversions = conversions.Where(x => x.ConvertCallback != null);
                        if (convertBackCallback)
                            conversions = conversions.Where(x => x.ConvertBackCallback != null);

                        collectedConversions.AddRange(conversions);
                    }

                currentType = currentType.BaseType;
            }

            if (collectedConversions.Count == 0)
            {
                _registeredTypeConversionsCache.TryAdd(hash, null);
                return null;
            }

            collectedConversions.TrimExcess();

            _registeredTypeConversionsCache.TryAdd(hash, collectedConversions);
            return collectedConversions;
        }

        private static void CompareObjectToType(object obj, Type targetType)
        {
            if (obj == null)
                return;

            var objType = obj.GetType();

            if (objType != targetType && !objType.IsAssignableToCached(targetType))
                throw new InvalidObjectTypeException(targetType);
        }

        public void Register(Type type, TypeConversion typeConversion)
        {
            _tryConvertCache.Clear();
            _registeredTypeConversionsCache.Clear();

            if (!_registeredTypes.TryGetValue(type, out List<TypeConversion> typeConversions))
                _registeredTypes[type] = typeConversions = new List<TypeConversion>();

            typeConversions.Add(typeConversion);
        }

        public void AddPrimitiveType(Type type)
        {
            _isPrimitiveTypeCache.Clear();
            _primitiveTypes.Add(type);
        }

        public void RemovePrimitiveType(Type type)
        {
            _isPrimitiveTypeCache.Clear();
            _primitiveTypes.Remove(type);
        }

        public TypeConverter Clone()
        {
            return new TypeConverter(_registeredTypes);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
