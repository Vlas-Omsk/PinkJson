using System;

namespace PinkJson2
{
    public class InvalidObjectTypeException : Exception
    {
        public InvalidObjectTypeException(Type type) : this(type.ToString())
        {
        }

        public InvalidObjectTypeException(string type) : base($"The object must be of type {type}")
        {
        }
    }
}
