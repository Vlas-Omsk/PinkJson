using System;

namespace PinkJson2
{
    public class PinkJsonException : Exception
    {
        public PinkJsonException() : base()
        {
        }

        public PinkJsonException(string message) : base(message)
        {
        }

        public PinkJsonException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
