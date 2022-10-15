using System;

namespace PinkJson2.KeyTransformers
{
    public sealed class CamelCaseKeyTransformer : IKeyTransformer
    {
        public string TransformKey(string key)
        {
            return char.ToLower(key[0]) + key.Substring(1);
        }
    }
}
