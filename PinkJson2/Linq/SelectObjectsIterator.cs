using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Linq
{
    internal sealed class SelectObjectsIterator<TResult> : JsonIterator<TResult>
    {
        private readonly Func<IEnumerable<JsonEnumerableItem>, TResult> _selector;

        public SelectObjectsIterator(IEnumerable<JsonEnumerableItem> source, Func<IEnumerable<JsonEnumerableItem>, TResult> selector) : this(source, null, selector)
        {
        }

        public SelectObjectsIterator(IEnumerator<JsonEnumerableItem> enumerator, Func<IEnumerable<JsonEnumerableItem>, TResult> selector) : this(null, enumerator, selector)
        {
        }

        private SelectObjectsIterator(IEnumerable<JsonEnumerableItem> source, IEnumerator<JsonEnumerableItem> enumerator, Func<IEnumerable<JsonEnumerableItem>, TResult> selector) : base(source, enumerator)
        {
            _selector = selector;
        }

        public override Iterator<JsonEnumerableItem, TResult> Clone()
        {
            return new SelectObjectsIterator<TResult>(Source, Enumerator, _selector);
        }

        public override bool MoveNext()
        {
            switch (State)
            {
                case 1:
                    if (Enumerator == null)
                    {
                        Enumerator = Source.GetEnumerator();

                        EnsureEnumeratorMoveNext();
                    }

                    MoveEnumeratorToObjectBegin();

                    State = 2;
                    goto case 2;
                case 2:
                    EnsureEnumeratorMoveNext();

                    if (new[] { JsonEnumerableItemType.ObjectEnd, JsonEnumerableItemType.ArrayEnd }.Contains(Enumerator.Current.Type))
                        break;

                    var objectEnumerable = new OneTimeIterator<JsonEnumerableItem>((IEnumerable<JsonEnumerableItem>)new SelectObjectIterator(Enumerator));

                    Current = _selector(objectEnumerable);

                    var enumerator = objectEnumerable.GetEnumerator();
                    while (enumerator.MoveNext()) ;
                    enumerator.Dispose();

                    return true;
            }

            Dispose();
            return false;
        }
    }
}
