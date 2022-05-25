using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PinkJson2
{
    public static class TypeConverter
    {
        private static Dictionary<Type, List<TypeConversion>> _registeredTypes = new Dictionary<Type, List<TypeConversion>>();
        private static MethodInfo _enumTryParseMethodInfo;

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

            Register(typeof(DateTime), new TypeConversion((object obj, Type targetType, ref bool handled) =>
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
            }));
            Register(typeof(Enum), new TypeConversion((object obj, Type targetType, ref bool handled) =>
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
            }));
        }

        public static object ChangeType(object value, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var valueType = value.GetType();

            if (valueType == type || type.IsAssignableFrom(valueType))
                return value;

            if (TryConvert(value, type, out var targetObj))
                return targetObj;

            try
            {
                return Convert.ChangeType(value, type);
            }
            catch
            {
                throw new ArgumentException($"Cannot convert value of type {value.GetType()} to type {type}");
            }
        }

        private static IEnumerable<TypeConversion> GetRegisteredTypeConversions(Type type)
        {
            var typeConversions = new List<TypeConversion>();

            while (type != null)
            {
                foreach (var keyValue in _registeredTypes)
                {
                    if (type == keyValue.Key)
                        typeConversions.AddRange(((IEnumerable<TypeConversion>)keyValue.Value).Reverse());
                }

                type = type.BaseType;
            }

            return typeConversions;
        }

        private static bool TryConvert(object obj, Type targetType, out object targetObj)
        {
            targetObj = null;
            var handled = false;

            foreach (var typeConversion in GetRegisteredTypeConversions(targetType))
            {
                if (typeConversion.TypeConversionCallback == null)
                    continue;

                targetObj = typeConversion.TypeConversionCallback.Invoke(obj, targetType, ref handled);
                if (handled)
                {
                    if (targetObj.GetType() != targetType)
                        throw new InvalidObjectTypeException(targetType);
                    return true;
                }
            }

            var type = obj.GetType();

            foreach (var typeConversion in GetRegisteredTypeConversions(type))
            {
                if (typeConversion.TypeConversionBackCallback == null)
                    continue;

                targetObj = typeConversion.TypeConversionBackCallback.Invoke(obj, targetType, ref handled);
                if (handled)
                {
                    if (targetObj.GetType() != targetType)
                        throw new InvalidObjectTypeException(targetType);
                    return true;
                }
            }

            return false;
        }

        public static void Register(Type type, TypeConversion typeConversion)
        {
            if (!_registeredTypes.TryGetValue(type, out List<TypeConversion> typeConversions))
                _registeredTypes[type] = typeConversions = new List<TypeConversion>();

            typeConversions.Add(typeConversion);
        }
    }
}
