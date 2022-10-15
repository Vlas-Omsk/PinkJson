using System;

namespace PinkJson2.Formatters
{
    public interface ITextWriter
    {
        void Write(string value);
        void Write(char value);
        void WriteLine(string value);
        void WriteLine();
    }
}
