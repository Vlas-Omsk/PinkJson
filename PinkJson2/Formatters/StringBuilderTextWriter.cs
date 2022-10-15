using System;
using System.Text;

namespace PinkJson2.Formatters
{
    public sealed class StringBuilderTextWriter : ITextWriter
    {
        private readonly StringBuilder _writer;

        public StringBuilderTextWriter(StringBuilder writer)
        {
            _writer = writer;
        }

        public void Write(string value)
        {
            _writer.Append(value);
        }

        public void Write(char value)
        {
            _writer.Append(value);
        }

        public void WriteLine(string value)
        {
            _writer.AppendLine(value);
        }

        public void WriteLine()
        {
            _writer.AppendLine();
        }
    }
}
