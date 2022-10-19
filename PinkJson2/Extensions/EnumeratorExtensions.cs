using System;
using System.Collections;

namespace PinkJson2
{
    public static class EnumeratorExtensions
    {
        public static bool TryDispose(this IEnumerator enumerator)
        {
            if (enumerator is IDisposable disposable)
            {
                disposable.Dispose();
                return false;
            }

            return true;
        }
    }
}
