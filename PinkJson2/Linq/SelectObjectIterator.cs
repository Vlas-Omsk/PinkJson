using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Linq
{
    internal sealed class SelectObjectIterator : JsonIterator<JsonEnumerableItem>
    {
        private int _depth;

        public SelectObjectIterator(IEnumerable<JsonEnumerableItem> source) : this(source, null)
        {
        }

        public SelectObjectIterator(IEnumerator<JsonEnumerableItem> enumerator) : this(null, enumerator)
        {
        }

        private SelectObjectIterator(IEnumerable<JsonEnumerableItem> source, IEnumerator<JsonEnumerableItem> enumerator) : base(source, enumerator)
        {
        }

        public override Iterator<JsonEnumerableItem, JsonEnumerableItem> Clone()
        {
            return new SelectObjectIterator(Source, Enumerator);
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

                    State = 2;
                    goto case 2;
                case 2:
                    switch (Enumerator.Current.Type)
                    {
                        case JsonEnumerableItemType.ObjectBegin:
                        case JsonEnumerableItemType.ArrayBegin:
                            Current = Enumerator.Current;

                            _depth = 1;
                            State = 3;
                            return true;
                        case JsonEnumerableItemType.Key:
                            Current = Enumerator.Current;

                            EnsureEnumeratorMoveNext();

                            return true;
                        case JsonEnumerableItemType.Value:
                            Current = Enumerator.Current;

                            State = 4;
                            return true;
                        default:
                            throw new Exception();
                    }
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
