using System;

namespace PinkJson2
{
    public class KeyNotFoundException : Exception
    {
        public KeyNotFoundException(string keyName) : base($"Key '{keyName}' not found")
        {
        }
    }
}
