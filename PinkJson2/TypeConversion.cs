using System;

namespace PinkJson2
{
    public delegate object TypeConversionCallback(object obj, Type targetType, ref bool handled);

    public sealed class TypeConversion
    {
        public TypeConversion(TypeConversionDirection direction, TypeConversionCallback сallback)
        {
            Direction = direction;
            ConvertCallback = сallback;
        }

        public TypeConversionType Type { get; set; } = TypeConversionType.Static;
        public TypeConversionDirection Direction { get; }
        public TypeConversionCallback ConvertCallback { get; }
    }

    public enum TypeConversionType
    {
        Static,
        Dynamic
    }

    public enum TypeConversionDirection
    {
        FromType,
        ToType
    }
}
