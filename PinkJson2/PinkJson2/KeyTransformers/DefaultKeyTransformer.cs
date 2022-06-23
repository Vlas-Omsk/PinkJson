using System;

namespace PinkJson2.KeyTransformers
{
    public sealed class DefaultKeyTransformer : IKeyTransformer
    {
        public string TransformKey(string key)
        {
            return key;
        }
    }
}
