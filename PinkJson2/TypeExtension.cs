using System;

namespace PinkJson2
{
    public static class TypeExtension
    {
        // from .Net 5
        public static bool IsAssignableTo(this Type sourceType, Type targetType) => targetType?.IsAssignableFrom(sourceType) ?? false;
    }
}
