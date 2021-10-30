﻿using System;

namespace PinkJson2
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonPropertyAttribute : Attribute
    {
        public string SerializerName { get; set; }
        public string DeserializerName { get; set; }
        public bool SerializerIgnore { get; set; }
        public bool DeserializerIgnore { get; set; }

        public JsonPropertyAttribute()
        {
        }

        public JsonPropertyAttribute(string name)
        {
            Name = name;
        }

        public string Name { set => SerializerName = DeserializerName = value; }
        public bool Ignore { set => SerializerIgnore = DeserializerIgnore = value; }
    }
}