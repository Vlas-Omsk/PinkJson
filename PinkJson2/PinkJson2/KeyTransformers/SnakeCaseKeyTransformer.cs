using System;
using System.Text.RegularExpressions;

namespace PinkJson2.KeyTransformers
{
    public sealed class SnakeCaseKeyTransformer : IKeyTransformer
    {
        public string TransformKey(string key)
        {
            return char.ToLower(key[0]) + Regex.Replace(key.Substring(1), "[A-Z]", m => '_' + char.ToLower(m.Value[0]).ToString());
        }
    }
}
