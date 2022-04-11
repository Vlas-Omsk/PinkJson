using System;

namespace PinkJson2
{
    public class KeyNotFoundException : PinkJsonException
    {
        public KeyNotFoundException(string keyName) : base($"Key '{keyName}' not found")
        {
        }
    }
}
