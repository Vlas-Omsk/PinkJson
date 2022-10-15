using System;
using System.IO;

namespace PinkJson2.Formatters
{
    public sealed class StreamTextWriter : ITextWriter
    {
        private readonly StreamWriter _writer;

        public StreamTextWriter(StreamWriter writer)
        {
            _writer = writer;
        }

        public void Write(string value)
        {
            _writer.Write(value);
        }

        public void Write(char value)
        {
            _writer.Write(value);
        }

        public void WriteLine(string value)
        {
            _writer.WriteLine(value);
        }

        public void WriteLine()
        {
            _writer.WriteLine();
        }
    }
}
