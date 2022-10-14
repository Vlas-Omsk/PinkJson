using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Linq
{
    internal sealed class WhereObjectsIterator : JsonIterator<JsonEnumerableItem>
    {
        private readonly Func<IEnumerable<JsonEnumerableItem>, bool> _predicate;
        private IEnumerator<JsonEnumerableItem> _objectEnumerator;

        public WhereObjectsIterator(IEnumerable<JsonEnumerableItem> source, Func<IEnumerable<JsonEnumerableItem>, bool> predicate) : this(source, null, predicate)
        {
        }

        public WhereObjectsIterator(IEnumerator<JsonEnumerableItem> enumerator, Func<IEnumerable<JsonEnumerableItem>, bool> predicate) : this(null, enumerator, predicate)
        {
        }

        private WhereObjectsIterator(IEnumerable<JsonEnumerableItem> source, IEnumerator<JsonEnumerableItem> enumerator, Func<IEnumerable<JsonEnumerableItem>, bool> predicate) : base(source, enumerator)
        {
            _predicate = predicate;
        }

        public override Iterator<JsonEnumerableItem, JsonEnumerableItem> Clone()
        {
            return new WhereObjectsIterator(Source, Enumerator, _predicate);
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

                    Current = Enumerator.Current;

                    State = 2;
                    return true;
                case 2:
                    EnsureEnumeratorMoveNext();

                    if (new[] { JsonEnumerableItemType.ObjectEnd, JsonEnumerableItemType.ArrayEnd }.Contains(Enumerator.Current.Type))
                    {
                        Current = Enumerator.Current;

                        State = 4;
                        return true;
                    }

                    var objectEnumerable = new CachedIterator<JsonEnumerableItem>((IEnumerable<JsonEnumerableItem>)new SelectObjectIterator(Enumerator));
                    _objectEnumerator = objectEnumerable.GetEnumerator();

                    if (_predicate(objectEnumerable))
                    {
                        State = 3;
                        goto case 3;
                    }

                    while (_objectEnumerator.MoveNext()) ;
                    _objectEnumerator.Dispose();

                    goto case 2;
                case 3:
                    if (_objectEnumerator.MoveNext())
                    {
                        Current = _objectEnumerator.Current;

                        return true;
                    }

                    _objectEnumerator.Dispose();

                    State = 2;
                    goto case 2;
            }

            Dispose();
            return false;
        }
    }
}
