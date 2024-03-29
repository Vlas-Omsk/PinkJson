﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly IEnumerable<JsonEnumerableItem> _data;
        private readonly TypeConverter _typeConverter;
        private readonly Encoding _encoding;

        public JsonContent(IEnumerable<JsonEnumerableItem> data) : 
            this(data, _defaultEncoding, _defaultMediaType)
        {
        }

        public JsonContent(IEnumerable<JsonEnumerableItem> data, TypeConverter typeConverter) :
            this(data, typeConverter, _defaultEncoding, _defaultMediaType)
        {
        }

        public JsonContent(IEnumerable<JsonEnumerableItem> data, Encoding encoding) : 
            this(data, encoding, _defaultMediaType)
        {
        }

        public JsonContent(IEnumerable<JsonEnumerableItem> data, TypeConverter typeConverter, Encoding encoding) :
            this(data, typeConverter, encoding, _defaultMediaType)
        {
        }

        public JsonContent(IEnumerable<JsonEnumerableItem> data, string mediaType) : 
            this(data, _defaultEncoding, mediaType)
        {
        }

        public JsonContent(IEnumerable<JsonEnumerableItem> data, TypeConverter typeConverter, string mediaType) :
            this(data, typeConverter, _defaultEncoding, mediaType)
        {
        }

        public JsonContent(IEnumerable<JsonEnumerableItem> data, Encoding encoding, string mediaType) : 
            this(data, encoding, new MediaTypeHeaderValue(mediaType) { CharSet = encoding.WebName })
        {
        }

        public JsonContent(IEnumerable<JsonEnumerableItem> data, TypeConverter typeConverter, Encoding encoding, string mediaType) : 
            this(data, typeConverter, encoding, new MediaTypeHeaderValue(mediaType) { CharSet = encoding.WebName })
        {
        }

        public JsonContent(IEnumerable<JsonEnumerableItem> data, Encoding encoding, MediaTypeHeaderValue mediaType) : 
            this(data, TypeConverter.Default, encoding, mediaType)
        {
        }

        public JsonContent(IEnumerable<JsonEnumerableItem> data, TypeConverter typeConverter, Encoding encoding, MediaTypeHeaderValue mediaType)
        {
            _data = data;
            _typeConverter = typeConverter;
            _encoding = encoding;
            Headers.ContentType = mediaType;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return Task.Run(() =>
            {
                using (var streamWriter = new StreamWriter(stream, _encoding, 1024, true))
                    _data.ToStream(streamWriter, _typeConverter);
            });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}