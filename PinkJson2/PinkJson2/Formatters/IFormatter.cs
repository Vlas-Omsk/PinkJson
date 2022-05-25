using System;

namespace PinkJson2.Formatters
{
    public interface IFormatter
    {
        string Format(IJson json);
    }
}
