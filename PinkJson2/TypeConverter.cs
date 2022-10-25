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
        private readonly static TypeConversion _dateTimeTypeConversion = new TypeConversion(TypeConversionDirection.ToType, (object obj, Type targetType, ref bool handled) =>
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
        private readonly static TypeConversion _enumTypeConversion = new TypeConversion(TypeConversionDirection.ToType, (object obj, Type targetType, ref bool handled) =>
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
        private readonly static TypeConversion _guidTypeConversion = new TypeConversion(TypeConversionDirection.ToType, (object obj, Type targetType, ref bool handled) =>
        {
            if (obj is string value)
            {
                handled = true;
                return Guid.Parse(value);
            }

            return null;
        });
        private readonly Dictionary<Type, List<TypeConversion>> _registeredTypes = new Dictionary<Type, List<TypeConversion>>();
        private readonly ConcurrentDictionary<int, bool> _isPrimitiveTypeCache = new ConcurrentDictionary<int, bool>();
        private readonly ConcurrentDictionary<int, IEnumerable<TypeConversion>> _tryConvertCache = new ConcurrentDictionary<int, IEnumerable<TypeConversion>>();
        private readonly HashSet<Type> _primitiveTypes = new HashSet<Type>()
        {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(Guid)
        };

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
            var hash = type.GetHashCodeCached();

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

            if (valueType.IsEqualsOrAssignableTo(targetType))
                return value;

            if (TryConvert(valueType, value, targetType, out var targetObj))
                return targetObj;

            if (targetType == typeof(object))
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

        private bool TryConvert(Type type, object obj, Type targetType, out object targetObj)
        {
            targetObj = null;

            var hash = unchecked(targetType.GetHashCodeCached() + type.GetHashCodeCached());

            if (_tryConvertCache.TryGetValue(hash, out var cacheItem))
            {
                if (cacheItem == null)
                    return false;

                if (!TryConvertUsingTypeConversions(cacheItem, obj, targetType, out targetObj, out _))
                    throw new Exception();
                return true;
            }
            else
            {
                TypeConversion conversion;
                var conversions = GetRegisteredTypeConversions(targetType, TypeConversionDirection.ToType, TypeConversionType.Static);

                if (TryConvertUsingTypeConversions(conversions, obj, targetType, out targetObj, out conversion))
                {
                    _tryConvertCache.TryAdd(hash, new TypeConversion[] { conversion });
                    return true;
                }

                conversions = GetRegisteredTypeConversions(type, TypeConversionDirection.FromType, TypeConversionType.Static);

                if (TryConvertUsingTypeConversions(conversions, obj, targetType, out targetObj, out conversion))
                {
                    _tryConvertCache.TryAdd(hash, new TypeConversion[] { conversion });
                    return true;
                }

                conversions = GetRegisteredTypeConversions(targetType, TypeConversionDirection.ToType, TypeConversionType.Dynamic)
                    .Concat(GetRegisteredTypeConversions(type, TypeConversionDirection.FromType, TypeConversionType.Dynamic))
                    .ToArray();

                if (TryConvertUsingTypeConversions(conversions, obj, targetType, out targetObj, out _))
                {
                    _tryConvertCache.TryAdd(hash, conversions);
                    return true;
                }
            }

            _tryConvertCache.TryAdd(hash, null);
            return false;
        }

        private bool TryConvertUsingTypeConversions(IEnumerable<TypeConversion> conversions, object obj, Type targetType, out object targetObj, out TypeConversion targetConversion)
        {
            var handled = false;
            
            foreach (var conversion in conversions)
            {
                targetObj = conversion.ConvertCallback.Invoke(obj, targetType, ref handled);

                if (handled)
                {
                    CompareObjectToType(targetObj, targetType);
                    targetConversion = conversion;
                    return true;
                }
            }

            targetObj = null;
            targetConversion = null;
            return false;
        }

        private IEnumerable<TypeConversion> GetRegisteredTypeConversions(Type type, TypeConversionDirection conversionDirection, TypeConversionType conversionType)
        {
            var currentType = type;

            while (currentType != null)
            {
                if (_registeredTypes.TryGetValue(currentType, out var conversions))
                {
                    foreach (var conversion in conversions)
                    {
                        if (conversion.Type != conversionType || conversion.Direction != conversionDirection)
                            continue;

                        yield return conversion;
                    }
                }

                currentType = currentType.BaseType;
            }
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
