﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PinkJson2
{
    public static class TypeExtension
    {
        private static readonly ConcurrentDictionary<int, bool> _isArrayTypeCache = new ConcurrentDictionary<int, bool>();
        private static readonly ConcurrentDictionary<int, bool> _isAssignableToCache = new ConcurrentDictionary<int, bool>();

        public static bool IsArrayType(this Type type)
        {
            var hash = MemoizationHelper.GetHashCode(type);

            if (_isArrayTypeCache.TryGetValue(hash, out var result))
                return result;

            result = 
                type.GetInterface(nameof(IEnumerable)) != null && 
                type != typeof(string);

            _isArrayTypeCache.TryAdd(hash, result);
            return result;
        }

        public static bool IsEqualsOrAssignableTo(this Type type, Type targetType)
        {
            return
                type == targetType ||
                type.IsAssignableToCached(targetType);
        }

        public static bool IsValueType(this Type type)
        {
            return (type.IsValueType && !type.IsPrimitive) || type == typeof(string);
        }

        public static bool IsPrimitiveType(this Type type)
        {
            return
                type.IsPrimitive ||
                type.IsEnum;
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
            var hash = MemoizationHelper.GetHashCode(sourceType, targetType);

            if (_isAssignableToCache.TryGetValue(hash, out var result))
                return result;

            result = sourceType.IsAssignableTo(targetType);

            _isAssignableToCache.TryAdd(hash, result);
            return result;
        }

        public static object GetDefaultValue(this Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        public static Type GetElementTypeFromEnumerable(this Type type)
        {
            var enumerableType = type;

            if (type.Name != "IEnumerable`1")
                enumerableType = type.GetInterface("IEnumerable`1");

            if (enumerableType == null)
                return typeof(object);
            else
                return enumerableType.GenericTypeArguments[0];
        }

#if !NET5_0_OR_GREATER
        public static bool IsAssignableTo(this Type sourceType, Type targetType) => targetType?.IsAssignableFrom(sourceType) ?? false;
#endif
    }
}
