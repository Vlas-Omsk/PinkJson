using System;

namespace PinkJson2
{
    public delegate object TypeConversionCallback(object obj, Type targetType, ref bool handled);

    public sealed class TypeConversion
    {
        public TypeConversionCallback TypeConversionCallback { get; set; }
        public TypeConversionCallback TypeConversionBackCallback { get; set; }

        public TypeConversion()
        {
        }

        public TypeConversion(TypeConversionCallback typeConversionCallback, TypeConversionCallback typeConversionBackCallback)
        {
            TypeConversionCallback = typeConversionCallback;
            TypeConversionBackCallback = typeConversionBackCallback;
        }

        public TypeConversion(TypeConversionCallback typeConversionCallback)
        {
            TypeConversionCallback = typeConversionCallback;
        }
    }
}
