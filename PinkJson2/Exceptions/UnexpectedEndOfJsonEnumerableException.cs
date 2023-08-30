using System;

namespace PinkJson2
{
    public class UnexpectedEndOfJsonEnumerableException : UnexpectedEndOfEnumerableException
    {
        public UnexpectedEndOfJsonEnumerableException() :
            base($"Unexpected end of json enumerable")
        {
        }
    }
}
