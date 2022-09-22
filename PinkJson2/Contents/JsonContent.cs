using System;
using System.Net.Http;
using System.Text;

namespace PinkJson2.Contents
{
    public sealed class JsonContent : StringContent
    {
        private const string _defaultMediaType = "application/json";

        public JsonContent(IJson data) : base(data.ToString(), Encoding.UTF8, _defaultMediaType)
        {
        }
    }
}