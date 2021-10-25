using System;

namespace PinkJson2
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonPropertyAttribute : Attribute
    {
        public string Name { get; set; }
        public bool Ignore { get; set; }

        public JsonPropertyAttribute()
        {
        }

        public JsonPropertyAttribute(string name)
        {
            Name = name;
        }
    }
}
