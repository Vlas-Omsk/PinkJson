using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson2.Contents
{
    public sealed class JsonContent : HttpContent
    {
        private const string _defaultMediaType = "application/json";
        private static readonly Encoding _defaultEncoding = new UTF8Encoding(false);
        private readonly IJson _data;
        private readonly Encoding _encoding;

        public JsonContent(IJson data) : this(data, _defaultEncoding, _defaultMediaType)
        {
        }

        public JsonContent(IJson data, Encoding encoding) : this(data, encoding, _defaultMediaType)
        {
        }

        public JsonContent(IJson data, string mediaType) : this(data, _defaultEncoding, mediaType)
        {
        }

        public JsonContent(IJson data, Encoding encoding, string mediaType) : this(data, encoding, new MediaTypeHeaderValue(mediaType) { CharSet = encoding.WebName })
        {
        }

        public JsonContent(IJson data, Encoding encoding, MediaTypeHeaderValue mediaType)
        {
            _data = data;
            _encoding = encoding;
            Headers.ContentType = mediaType;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return Task.Run(() =>
            {
                using (var streamWriter = new StreamWriter(stream, _encoding, 1024, true))
                    _data.ToStream(streamWriter);
            });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}