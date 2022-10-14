using System;
using System.Collections.Generic;

namespace PinkJson2
{
    public static class EnumerableExtensions
    {
        public static void SkipAll<T>(this IEnumerable<T> self)
        {
            var enumerator = self.GetEnumerator();

            while (enumerator.MoveNext()) ;

            enumerator.Dispose();
        }
    }
}
