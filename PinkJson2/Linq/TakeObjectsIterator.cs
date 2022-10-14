using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Linq
{
    internal sealed class TakeObjectsIterator : JsonIterator<JsonEnumerableItem>
    {
        private readonly int _count;
        private JsonEnumerableItemType _beginType;
        private int _depth;
        private int _taked;

        public TakeObjectsIterator(IEnumerable<JsonEnumerableItem> source, int count) : this(source, null, count)
        {
        }

        public TakeObjectsIterator(IEnumerator<JsonEnumerableItem> enumerator, int count) : this(null, enumerator, count)
        {
        }

        private TakeObjectsIterator(IEnumerable<JsonEnumerableItem> source, IEnumerator<JsonEnumerableItem> enumerator, int count) : base(source, enumerator)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            _count = count;
        }

        public override Iterator<JsonEnumerableItem, JsonEnumerableItem> Clone()
        {
            return new TakeObjectsIterator(Source, Enumerator, _count);
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

                    _beginType = Enumerator.Current.Type;
                    Current = Enumerator.Current;
                    _depth = 1;
                    State = 2;
                    return true;
                case 2:
                    EnsureEnumeratorMoveNext();

                    if (new[] { JsonEnumerableItemType.ObjectBegin, JsonEnumerableItemType.ArrayBegin }.Contains(Enumerator.Current.Type))
                        _depth++;
                    if (new[] { JsonEnumerableItemType.ObjectEnd, JsonEnumerableItemType.ArrayEnd }.Contains(Enumerator.Current.Type))
                        _depth--;

                    if (_depth == 1 && new[] { JsonEnumerableItemType.ObjectEnd, JsonEnumerableItemType.ArrayEnd, JsonEnumerableItemType.Value }.Contains(Enumerator.Current.Type))
                        _taked++;

                    if (_taked == _count)
                        State = 3;
                    if (_depth == 0)
                        throw new Exception();

                    Current = Enumerator.Current;
                    return true;
                case 3:
                    switch (_beginType)
                    {
                        case JsonEnumerableItemType.ObjectBegin:
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);
                            break;
                        case JsonEnumerableItemType.ArrayBegin:
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);
                            break;
                    }
                    State = 4;
                    return true;
            }

            Dispose();
            return false;
        }
    }
}
