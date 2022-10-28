using System;

namespace PinkJson2.Exceptions
{
    public class UnexpectedEndOfEnumerableException : Exception
    {
        public UnexpectedEndOfEnumerableException() :
            this($"Unexpected end of enumerable")
        {
        }

        public UnexpectedEndOfEnumerableException(string message) :
            base(message)
        {
        }
    }
}
