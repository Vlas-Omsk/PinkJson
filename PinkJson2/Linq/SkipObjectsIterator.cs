using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Linq
{
    internal sealed class SkipObjectsIterator : JsonIterator<JsonEnumerableItem>
    {
        private readonly int _count;
        private int _depth;

        public SkipObjectsIterator(IEnumerable<JsonEnumerableItem> source, int count) : this(source, null, count)
        {
        }

        public SkipObjectsIterator(IEnumerator<JsonEnumerableItem> enumerator, int count) : this(null, enumerator, count)
        {
        }

        private SkipObjectsIterator(IEnumerable<JsonEnumerableItem> source, IEnumerator<JsonEnumerableItem> enumerator, int count) : base(source, enumerator)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            _count = count;
        }

        public override Iterator<JsonEnumerableItem, JsonEnumerableItem> Clone()
        {
            return new SkipObjectsIterator(Source, Enumerator, _count);
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
                    for (var i = 0; i < _count; i++)
                    {
                        EnsureEnumeratorMoveNext();
                        SkipOne();
                    }

                    _depth = 1;
                    State = 3;
                    goto case 3;
                case 3:
                    EnsureEnumeratorMoveNext();

                    if (new[] { JsonEnumerableItemType.ObjectBegin, JsonEnumerableItemType.ArrayBegin }.Contains(Enumerator.Current.Type))
                        _depth++;
                    if (new[] { JsonEnumerableItemType.ObjectEnd, JsonEnumerableItemType.ArrayEnd }.Contains(Enumerator.Current.Type))
                        _depth--;

                    if (_depth == 0)
                        State = 4;

                    Current = Enumerator.Current;
                    return true;
            }

            Dispose();
            return false;
        }
    }
}
