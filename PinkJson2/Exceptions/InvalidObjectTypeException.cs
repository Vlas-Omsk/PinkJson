using System;

namespace PinkJson2
{
    public class InvalidObjectTypeException : Exception
    {
        public InvalidObjectTypeException(Type type) : base($"The object must be of type {type}")
        {
        }
    }
}
