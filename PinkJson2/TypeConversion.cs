using System;

namespace PinkJson2
{
    public delegate object TypeConversionCallback(object obj, Type targetType, ref bool handled);

    public sealed class TypeConversion
    {
        public TypeConversion()
        {
        }

        public TypeConversion(TypeConversionCallback typeConversionCallback, TypeConversionCallback typeConversionBackCallback)
        {
            ConvertCallback = typeConversionCallback;
            ConvertBackCallback = typeConversionBackCallback;
        }

        public TypeConversion(TypeConversionCallback typeConversionCallback)
        {
            ConvertCallback = typeConversionCallback;
        }

        public TypeConversionType Type { get; set; } = TypeConversionType.Static;
        public TypeConversionCallback ConvertCallback { get; set; }
        public TypeConversionCallback ConvertBackCallback { get; set; }
    }
}
