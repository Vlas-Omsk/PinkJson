using System;
using System.Collections.Generic;

namespace PinkJson2.Linq
{
    public static class JsonEnumerable
    {
        public static IEnumerable<T> MakeCached<T>(this IEnumerable<T> self)
        {
            return new CachedIterator<T>(self);
        }

        public static IEnumerable<JsonEnumerableItem> SkipObjects(this IEnumerable<JsonEnumerableItem> self, int count)
        {
            return new SkipObjectsIterator(self, count);
        }

        public static IEnumerable<JsonEnumerableItem> TakeObjects(this IEnumerable<JsonEnumerableItem> self, int count)
        {
            return new TakeObjectsIterator(self, count);
        }

        public static IEnumerable<JsonEnumerableItem> SkipLastObjects(this IEnumerable<JsonEnumerableItem> self, int count)
        {
            return new SkipLastObjectsIterator(self, count);
        }

        public static IEnumerable<JsonEnumerableItem> TakeLastObjects(this IEnumerable<JsonEnumerableItem> self, int count)
        {
            return new TakeLastObjectsIterator(self, count);
        }

        public static IEnumerable<JsonEnumerableItem> SelectPath(this IEnumerable<JsonEnumerableItem> self, JsonPath path)
        {
            return new SelectPathIterator(self, path);
        }

        public static IEnumerable<JsonEnumerableItem> SelectObject(this IEnumerable<JsonEnumerableItem> self)
        {
            return new SelectObjectIterator(self);
        }

        public static IEnumerable<TResult> SelectObjects<TResult>(this IEnumerable<JsonEnumerableItem> self, Func<IEnumerable<JsonEnumerableItem>, TResult> selector)
        {
            return new SelectObjectsIterator<TResult>(self, selector);
        }

        public static IEnumerable<JsonEnumerableItem> WhereObjects(this IEnumerable<JsonEnumerableItem> self, Func<IEnumerable<JsonEnumerableItem>, bool> predicate)
        {
            return new WhereObjectsIterator(self, predicate);
        }
    }
}
