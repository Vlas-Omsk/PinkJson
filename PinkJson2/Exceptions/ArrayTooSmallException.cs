using System;

namespace PinkJson2
{
    internal sealed class ArrayTooSmallException : JsonSerializationException
    {
        public ArrayTooSmallException() : base("Array too small")
        {
        }
    }
}
