using System;

namespace PinkJson2
{
    public class UnexpectedEndOfStreamException : Exception
    {
        public UnexpectedEndOfStreamException() : base("Unexpected end of stream")
        {
        }
    }
}
