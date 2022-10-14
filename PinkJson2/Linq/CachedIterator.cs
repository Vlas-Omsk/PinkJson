using System;
using System.Collections.Generic;

namespace PinkJson2.Linq
{
    internal sealed class CachedIterator<T> : Iterator<T, T>
    {
        private readonly List<T> _buffer;
        private int _index = -1;

        public CachedIterator(IEnumerable<T> source) : this(source, null, new List<T>())
        {
        }

        public CachedIterator(IEnumerator<T> enumerator) : this(null, enumerator, new List<T>())
        {
        }

        private CachedIterator(IEnumerable<T> source, IEnumerator<T> enumerator, List<T> buffer) : base(source, enumerator, true)
        {
            _buffer = buffer;
        }

        public override Iterator<T, T> Clone()
        {
            if (Enumerator == null)
                Enumerator = Source.GetEnumerator();

            return new CachedIterator<T>(Source, Enumerator, _buffer);
        }

        public override bool MoveNext()
        {
            switch (State)
            {
                case 1:
                    if (Enumerator == null)
                        Enumerator = Source.GetEnumerator();

                    State = 2;
                    goto case 2;
                case 2:
                    _index++;

                    if (_index >= _buffer.Count)
                    {
                        if (!Enumerator.MoveNext())
                            break;

                        _buffer.Add(Enumerator.Current);
                        Current = Enumerator.Current;
                    }
                    else
                    {
                        Current = _buffer[_index];
                    }
                    return true;
            }

            Dispose();
            return false;
        }

        public override void Reset()
        {
            _index = -1;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
