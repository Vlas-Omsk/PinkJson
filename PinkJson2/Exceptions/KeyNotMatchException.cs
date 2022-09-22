using System;

namespace PinkJson2
{
    public class KeyNotMatchException : PinkJsonException
    {
        public KeyNotMatchException(string keyName, string objectKeyName) : base($"The object key \"{objectKeyName}\" and the specified key \"{keyName}\" do not match")
        {
        }
    }
}
