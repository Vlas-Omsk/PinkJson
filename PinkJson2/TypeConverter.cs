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
        private readonly static TypeConversion _dateTimeTypeConversion = new TypeConversion(
            typeof(DateTime),
            TypeConversionDirection.ToType,
            (object obj, Type targetType, ref bool handled) =>
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
            }
        );
        private readonly static TypeConversion _enumTypeConversion = new TypeConversion(
            typeof(Enum),
            TypeConversionDirection.ToType,
            (object obj, Type targetType, ref bool handled) =>
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
            }
        );
        private readonly static TypeConversion _guidTypeConversion = new TypeConversion(
            typeof(Guid),
            TypeConversionDirection.ToType,
            (object obj, Type targetType, ref bool handled) =>
            {
                if (obj is string value)
                {
                    handled = true;
                    return Guid.Parse(value);
                }

                return null;
            }
        );
        private readonly List<TypeConversion> _registeredTypes;
        private readonly ConcurrentDictionary<int, bool> _isPrimitiveTypeCache = new ConcurrentDictionary<int, bool>();
        private readonly ConcurrentDictionary<int, IEnumerable<TypeConversion>> _tryConvertCache = new ConcurrentDictionary<int, IEnumerable<TypeConversion>>();
        private readonly HashSet<Type> _primitiveTypes = new HashSet<Type>()
        {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(Guid),
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
            _registeredTypes = new List<TypeConversion>()
            {
                _dateTimeTypeConversion,
                _enumTypeConversion,
                _guidTypeConversion,
            };
        }

        private TypeConverter(IEnumerable<TypeConversion> registeredTypes)
        {
            _registeredTypes = new List<TypeConversion>(registeredTypes);
        }

        public static TypeConverter Default { get; set; } = new TypeConverter();

        public bool IsPrimitiveType(Type type)
        {
            var hash = MemoizationHelper.GetHashCode(type);

            if (_isPrimitiveTypeCache.TryGetValue(hash, out var result))
                return result;

            result =
                type.IsPrimitiveType() ||
                _primitiveTypes.Contains(type);

            _isPrimitiveTypeCache.TryAdd(hash, result);
            return result;
        }

        public bool TryChangeType(object value, Type targetType, out object result)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            if (value == null)
            {
                result = value;
                return true;
            }

            var underlyingTargetType = Nullable.GetUnderlyingType(targetType);

            if (underlyingTargetType != null)
                targetType = underlyingTargetType;

            var valueType = value.GetType();

            if (valueType.IsEqualsOrAssignableTo(targetType))
            {
                result = value;
                return true;
            }

            if (TryConvert(valueType, value, targetType, out var targetObj))
            {
                result = targetObj;
                return true;
            }

            if (targetType == typeof(object))
            {
                result = value;
                return true;
            }

            try
            {
                result = Convert.ChangeType(value, targetType);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public object ChangeType(object value, Type targetType)
        {
            if (!TryChangeType(value, targetType, out var result))
                throw new ArgumentException($"Cannot convert value of type {value.GetType()} to type {targetType}");

            return result;
        }

        private bool TryConvert(Type type, object obj, Type targetType, out object targetObj)
        {
            targetObj = null;

            var hash = MemoizationHelper.GetHashCode(targetType, type);

            if (_tryConvertCache.TryGetValue(hash, out var cacheItem))
            {
                if (cacheItem == null)
                    return false;

                return TryConvertUsingTypeConversions(cacheItem, obj, targetType, out targetObj, out _);
            }
            else
            {
                TypeConversion conversion;
                var conversions = GetRegisteredTypeConversions(
                    targetType, 
                    TypeConversionDirection.ToType, 
                    TypeConversionType.Static
                );

                if (TryConvertUsingTypeConversions(conversions, obj, targetType, out targetObj, out conversion))
                {
                    _tryConvertCache.TryAdd(hash, new TypeConversion[] { conversion });
                    return true;
                }

                conversions = GetRegisteredTypeConversions(
                    type, 
                    TypeConversionDirection.FromType, 
                    TypeConversionType.Static
                );

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

        private IEnumerable<TypeConversion> GetRegisteredTypeConversions(
            Type type, 
            TypeConversionDirection conversionDirection, 
            TypeConversionType conversionType
        )
        {
            var currentType = type;

            while (currentType != null)
            {
                foreach (var conversion in _registeredTypes.AsEnumerable().Reverse())
                {
                    if (conversion.TargetType != currentType ||
                        conversion.Type != conversionType ||
                        conversion.Direction != conversionDirection)
                        continue;

                    yield return conversion;
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

        public void Register(TypeConversion typeConversion)
        {
            _tryConvertCache.Clear();

            _registeredTypes.Add(typeConversion);
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
