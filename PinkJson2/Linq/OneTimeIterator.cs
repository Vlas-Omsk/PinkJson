using System;
using System.Collections.Generic;

namespace PinkJson2.Linq
{
    internal sealed class OneTimeIterator<T> : Iterator<T, T>
    {
        public OneTimeIterator(IEnumerable<T> source) : this(source, null)
        {
        }

        public OneTimeIterator(IEnumerator<T> enumerator) : this(null, enumerator)
        {
        }

        private OneTimeIterator(IEnumerable<T> source, IEnumerator<T> enumerator) : base(source, enumerator, true)
        {
        }

        public override Iterator<T, T> Clone()
        {
            if (Enumerator == null)
                Enumerator = Source.GetEnumerator();

            return new OneTimeIterator<T>(Source, Enumerator);
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
                    if (!Enumerator.MoveNext())
                        break;

                    Current = Enumerator.Current;

                    return true;
            }

            Dispose();
            return false;
        }
    }
}
