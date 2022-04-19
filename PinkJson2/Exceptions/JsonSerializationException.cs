using System;

namespace PinkJson2
{
    public class JsonSerializationException : PinkJsonException
    {
        public JsonSerializationException(string message) : base(message)
        {
        }
    }
}
