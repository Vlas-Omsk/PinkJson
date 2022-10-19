using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PinkJson2
{
    public static class TypeExtension
    {
        private static readonly Dictionary<Type, bool> _isArrayTypeCache = new Dictionary<Type, bool>();
        //private static readonly Dictionary<int, bool> _isEqualsOrAssignableToCache = new Dictionary<int, bool>();
        private static readonly Dictionary<Type, bool> _isValueTypeCache = new Dictionary<Type, bool>();
        //private static readonly Dictionary<int, bool> _isAssignableToCache = new Dictionary<int, bool>();

        public static bool IsArrayType(this Type type)
        {
            if (_isArrayTypeCache.TryGetValue(type, out var result))
                return result;

            result = 
                type.GetInterface(nameof(IEnumerable)) != null && 
                type != typeof(string);

            _isArrayTypeCache.Add(type, result);
            return result;
        }

        public static bool IsEqualsOrAssignableTo(this Type type, Type targetType)
        {
            //var hash = type.GetHashCode() + targetType.GetHashCode();

            //if (_isEqualsOrAssignableToCache.TryGetValue(hash, out var result))
            //    return result;

            var result =
                type == targetType ||
                type.IsAssignableToCached(targetType);

            //_isEqualsOrAssignableToCache.Add(hash, result);
            return result;
        }

        public static bool IsValueType(this Type type)
        {
            if (_isValueTypeCache.TryGetValue(type, out var result))
                return result;

            result = (type.IsValueType && !type.IsPrimitive) || type == typeof(string);

            _isValueTypeCache.Add(type, result);
            return result;
        }

        public static bool IsPrimitiveType(this Type type, TypeConverter typeConverter)
        {
            return
                type.IsPrimitive ||
                type.IsEnum ||
                typeConverter.PrimitiveTypes.Contains(type);
        }

        public static bool IsAnonymousType(this Type type)
        {
            return 
                Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false) && 
                (type.IsGenericType || IsEmptyAnonymousType(type)) && 
                type.Name.Contains("AnonymousType") && 
                (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")) && 
                (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public static bool IsEmptyAnonymousType(this Type type)
        {
            var name = type.Name;
            while (char.IsDigit(name[name.Length - 1]))
                name = name.Substring(0, name.Length - 1);
            return name == "<>f__AnonymousType";
        }

        public static bool IsAssignableToCached(this Type sourceType, Type targetType)
        {
            //var hash = sourceType.GetHashCode() + targetType.GetHashCode();

            //if (_isAssignableToCache.TryGetValue(hash, out var result))
            //    return result;

            var result = sourceType.IsAssignableTo(targetType);

            //_isAssignableToCache.Add(hash, result);
            return result;
        }

#if !NET5_0_OR_GREATER
        public static bool IsAssignableTo(this Type sourceType, Type targetType) => targetType?.IsAssignableFrom(sourceType) ?? false;
#endif
    }
}
