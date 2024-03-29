﻿using System;

namespace PinkJson2
{
    public class JsonParserException : PinkJsonException
    {
        public JsonParserException(string message, JsonPath path) : base(Create(message, path))
        {
            Path = path;
        }

        public JsonParserException(string message, JsonPath path, JsonLexerException innerException) : base(Create(message, path), innerException)
        {
            Path = path;
            InnerException = innerException;
        }

        public JsonPath Path { get; }
        public new JsonLexerException InnerException { get; }

        private static string Create(string message, JsonPath path)
        {
            return $"{message}\r\nPath: {path}";
        }
    }
}
