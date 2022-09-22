using System;

namespace PinkJson2.KeyTransformers
{
    public interface IKeyTransformer
    {
        string TransformKey(string key);
    }
}
