using PinkJson2.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PinkJson2
{
    public static class FormatterExtensions
    {
        public static string FormatToString(this IFormatter self, IEnumerable<JsonEnumerableItem> json)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, -1, true))
                    self.Format(json, streamWriter);

                memoryStream.Position = 0;

                using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8, false, -1, true))
                    return streamReader.ReadToEnd();
            }
        }
    }
}
