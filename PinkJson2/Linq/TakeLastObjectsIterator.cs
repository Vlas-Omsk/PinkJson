using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Linq
{
    internal sealed class TakeLastObjectsIterator : JsonIterator<JsonEnumerableItem>
    {
        private readonly int _count;
        private readonly Queue<JsonEnumerableItem[]> _buffer;
        private JsonEnumerableItem[] _currentObject;
        private int _currentObjectIndex;

        public TakeLastObjectsIterator(IEnumerable<JsonEnumerableItem> source, int count) : this(source, null, count)
        {
        }

        public TakeLastObjectsIterator(IEnumerator<JsonEnumerableItem> enumerator, int count) : this(null, enumerator, count)
        {
        }

        private TakeLastObjectsIterator(IEnumerable<JsonEnumerableItem> source, IEnumerator<JsonEnumerableItem> enumerator, int count) : base(source, enumerator)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            _count = count;
            _buffer = new Queue<JsonEnumerableItem[]>(_count);
        }

        public override Iterator<JsonEnumerableItem, JsonEnumerableItem> Clone()
        {
            return new TakeLastObjectsIterator(Source, Enumerator, _count);
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
                    while (true)
                    {
                        EnsureEnumeratorMoveNext();

                        if (new[] { JsonEnumerableItemType.ObjectEnd, JsonEnumerableItemType.ArrayEnd }.Contains(Enumerator.Current.Type))
                            break;

                        if (_buffer.Count == _count)
                            _buffer.Dequeue();

                        _buffer.Enqueue(ReadOne());
                    }
                    
                    State = 3;
                    goto case 3;
                case 3:
                    if (_buffer.Count == 0)
                    {
                        Current = Enumerator.Current;
                        State = 5;
                        return true;
                    }

                    _currentObject = _buffer.Dequeue();
                    _currentObjectIndex = 0;
                    State = 4;
                    goto case 4;
                case 4:
                    Current = _currentObject[_currentObjectIndex];

                    _currentObjectIndex++;

                    if (_currentObject.Length == _currentObjectIndex)
                        State = 3;
                    return true;
            }

            Dispose();
            return false;
        }
    }
}
