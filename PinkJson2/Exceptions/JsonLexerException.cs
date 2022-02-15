using System;
using System.IO;

namespace PinkJson2
{
    public class JsonLexerException : Exception
    {
        public int Position { get; }

        private const short _range = 200;

        public JsonLexerException(string message, int position, StreamReader stream) : base(Create(message, position, stream))
        {
            Position = position;
        }

        public JsonLexerException(string message, int position, StreamReader stream, Exception innerException) : base(Create(message, position, stream), innerException)
        {
            Position = position;
        }

        private static string Create(string message, int pos, StreamReader stream)
        {
            var content = "Impossible to seek in stream.";

            if (stream.BaseStream.CanSeek)
            {
                var startPos = pos - _range;
                var arrowPos = (int)_range;
                if (startPos < 0)
                {
                    startPos = 0;
                    arrowPos = pos;
                }
                var endPos = pos + _range;
                var length = endPos - startPos;

                var buffer = new char[startPos + length];
                stream.BaseStream.Position = 0;
                stream.DiscardBufferedData();
                length = stream.Read(buffer, 0, startPos + length);
                Array.Resize(ref buffer, length);
                content = "\r\n" + string.Join("", buffer);
                content = content.Substring(startPos).Insert(arrowPos, " --->");
            }

            return $"{message} (Position: {pos})\r\nWhere: {content}";
        }
    }
}
