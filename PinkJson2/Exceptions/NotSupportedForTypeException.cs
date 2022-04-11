using System;

namespace PinkJson2
{
    public class NotSupportedForTypeException : PinkJsonException
    {
        public NotSupportedForTypeException(Type type) : base($"An object of type {type} does not support operation")
        {
        }
    }
}
