using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PinkJson2
{
    public static class TypeExtension
    {
        public static bool IsArrayType(this Type type)
        {
            return 
                type.GetInterface(nameof(IEnumerable)) != null && 
                type != typeof(string);
        }

        public static bool IsDictionaryType(this Type type)
        {
            return
                type == typeof(IDictionary) ||
                type.IsAssignableTo(typeof(IDictionary));
        }

        public static bool IsJsonType(this Type type)
        {
            return
                type == typeof(IJson) ||
                type.IsAssignableTo(typeof(IJson));
        }

        public static bool IsValueType(this Type type)
        {
            return (type.IsValueType && !type.IsPrimitive) || type == typeof(string);
        }

        public static bool IsPrimitiveType(this Type type, TypeConverter typeConverter)
        {
            return 
                type.IsPrimitive || 
                type.IsEnum ||
                typeConverter.PrimitiveTypes.Any(x => x == type);
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

#if NET5_0_OR_GREATER == false
        public static bool IsAssignableTo(this Type sourceType, Type targetType) => targetType?.IsAssignableFrom(sourceType) ?? false;
#endif
    }
}
