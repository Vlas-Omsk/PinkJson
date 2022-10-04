using System;
using System.IO;

namespace PinkJson2.Formatters
{
    public interface IFormatter
    {
        void Format(IJson json, StreamWriter stream);
    }
}
