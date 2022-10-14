using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Linq
{
    internal sealed class SkipLastObjectsIterator : JsonIterator<JsonEnumerableItem>
    {
        private readonly int _count;
        private readonly int _bufferCapacity;
        private readonly Queue<JsonEnumerableItem[]> _buffer;
        private JsonEnumerableItem[] _currentObject;
        private int _currentObjectIndex;

        public SkipLastObjectsIterator(IEnumerable<JsonEnumerableItem> source, int count) : this(source, null, count)
        {
        }

        public SkipLastObjectsIterator(IEnumerator<JsonEnumerableItem> enumerator, int count) : this(null, enumerator, count)
        {
        }

        private SkipLastObjectsIterator(IEnumerable<JsonEnumerableItem> source, IEnumerator<JsonEnumerableItem> enumerator, int count) : base(source, enumerator)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            _count = count;
            _bufferCapacity = _count + 1;
            _buffer = new Queue<JsonEnumerableItem[]>(_bufferCapacity);
        }

        public override Iterator<JsonEnumerableItem, JsonEnumerableItem> Clone()
        {
            return new SkipLastObjectsIterator(Source, Enumerator, _count);
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
                    while (_buffer.Count != _bufferCapacity)
                    {
                        EnsureEnumeratorMoveNext();

                        if (new[] { JsonEnumerableItemType.ObjectEnd, JsonEnumerableItemType.ArrayEnd }.Contains(Enumerator.Current.Type))
                        {
                            Current = Enumerator.Current;
                            State = 4;
                            return true;
                        }

                        _buffer.Enqueue(ReadOne());
                    }

                    _currentObject = _buffer.Dequeue();
                    _currentObjectIndex = 0;
                    State = 3;
                    goto case 3;
                case 3:
                    Current = _currentObject[_currentObjectIndex];

                    _currentObjectIndex++;

                    if (_currentObject.Length == _currentObjectIndex)
                        State = 2;
                    return true;
            }

            Dispose();
            return false;
        }
    }
}
