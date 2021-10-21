using System;

namespace PinkJson2
{
    public interface IFormatter
    {
        string Format(IJson json);
    }
}
