﻿using System;

namespace PinkJson2
{
    public sealed class JsonKeyValue : JsonChild
    {
        public string Key { get; set; }

        public JsonKeyValue(string key, object value) : base(value)
        {
            Key = key;
        }
    }
}