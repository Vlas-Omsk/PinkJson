using System;
using System.Collections;
using System.Collections.Generic;

namespace PinkJson2.Linq
{
    internal abstract class Iterator<TSource, TResult> : IEnumerable<TResult>, IEnumerator<TResult>
    {
        private readonly int _threadId;
        private readonly bool _leaveOpen;

        protected IEnumerable<TSource> Source { get; }
        protected IEnumerator<TSource> Enumerator { get; set; }
        protected int State { get; set; }

        protected Iterator(IEnumerable<TSource> source, IEnumerator<TSource> enumerator) : this(source, enumerator, enumerator != null)
        {
        }

        protected Iterator(IEnumerable<TSource> source, IEnumerator<TSource> enumerator, bool leaveOpen)
        {
            _threadId = Environment.CurrentManagedThreadId;
            Source = source;
            Enumerator = enumerator;
            _leaveOpen = leaveOpen;
        }

        public TResult Current { get; protected set; }
        object IEnumerator.Current => throw new NotImplementedException();

        public abstract Iterator<TSource, TResult> Clone();

        public IEnumerator<TResult> GetEnumerator()
        {
            var iterator = State == 0 && _threadId == Environment.CurrentManagedThreadId ? this : Clone();
            iterator.State = 1;
            return iterator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract bool MoveNext();

        public virtual void Reset()
        {
            throw new NotSupportedException();
        }

        public virtual void Dispose()
        {
            if (Enumerator != null && !_leaveOpen)
            {
                Enumerator.Dispose();
                Enumerator = null;
            }

            Current = default;
            State = -1;
        }

        protected void EnsureEnumeratorMoveNext()
        {
            if (!Enumerator.MoveNext())
                throw new Exception();
        }
    }
}
