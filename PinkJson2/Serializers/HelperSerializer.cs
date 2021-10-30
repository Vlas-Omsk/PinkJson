using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PinkJson2.Serializers
{
    public static class HelperSerializer
    {
        public static bool IsArray(Type type)
        {
            return type.GetInterface(nameof(IEnumerable)) != null && type != typeof(string);
        }

        public static bool IsAnonymousType(Type type)
        {
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && (type.IsGenericType || IsEmptyAnonymousType(type))
                && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public static bool IsEmptyAnonymousType(Type type)
        {
            var name = type.Name;
            while (char.IsDigit(name[name.Length - 1]))
                name = name.Substring(0, name.Length - 1);
            return name == "<>f__AnonymousType";
        }
    }
}
