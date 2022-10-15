using PinkJson2.Formatters;
using System;
using System.Collections.Generic;
using System.Text;

namespace PinkJson2
{
    public static class FormatterExtensions
    {
        public static string FormatToString(this IFormatter self, IEnumerable<JsonEnumerableItem> json)
        {
            var stringBuilder = new StringBuilder();

            self.Format(json, new StringBuilderTextWriter(stringBuilder));

            return stringBuilder.ToString();
        }
    }
}
