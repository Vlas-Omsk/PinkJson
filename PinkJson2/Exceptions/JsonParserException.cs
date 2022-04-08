using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2
{
    public class JsonParserException : Exception
    {
        public IEnumerable<string> Path { get; }
        public new JsonLexerException InnerException { get; }

        public JsonParserException(string message, IEnumerable<string> path) : base(Create(message, path))
        {
            Path = path;
        }

        public JsonParserException(string message, IEnumerable<string> path, JsonLexerException innerException) : base(Create(message, path), innerException)
        {
            Path = path;
            InnerException = innerException;
        }

        private static string Create(string message, IEnumerable<string> path)
        {
            return $"{message}\r\nPath: {GetPath(path)}";
        }

        private static string GetPath(IEnumerable<string> path)
        {
            var pathString = string.Empty;
            for (var i = 1; i <= path.Count(); i++)
            {
                var current = path.ElementAt(path.Count() - i);
                if (current.Any(ch => char.IsWhiteSpace(ch)))
                {
                    pathString += $"[\"{current.EscapeString()}\"]";
                }
                else
                {
                    if (i != 1)
                        pathString += ".";
                    pathString += current;
                }
            }
            return pathString;
        }
    }
}
