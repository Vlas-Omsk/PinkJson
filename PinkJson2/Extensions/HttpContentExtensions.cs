using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PinkJson2
{
    public static class HttpContentExtensions
    {
        public static async Task<IEnumerable<JsonEnumerableItem>> ReadAsJsonAsync(this HttpContent self)
        {
            using (var stream = await self.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream, null, true, 0))
                return Json.Parse(reader).ToArray();
        }
    }
}
