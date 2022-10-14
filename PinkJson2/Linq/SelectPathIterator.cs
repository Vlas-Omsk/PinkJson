using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Linq
{
    internal sealed class SelectPathIterator : JsonIterator<JsonEnumerableItem>
    {
        private readonly JsonPath _path;
        private int _pathIndex;
        private int _depth;

        public SelectPathIterator(IEnumerable<JsonEnumerableItem> source, JsonPath path) : this(source, null, path)
        {
        }

        public SelectPathIterator(IEnumerator<JsonEnumerableItem> enumerator, JsonPath path) : this(null, enumerator, path)
        {
        }

        private SelectPathIterator(IEnumerable<JsonEnumerableItem> source, IEnumerator<JsonEnumerableItem> enumerator, JsonPath path) : base(source, enumerator)
        {
            _path = path;
        }

        public override Iterator<JsonEnumerableItem, JsonEnumerableItem> Clone()
        {
            return new SelectPathIterator(Source, Enumerator, _path);
        }

        public override bool MoveNext()
        {
            switch (State)
            {
                case 1:
                    bool hasEnumerator = Enumerator != null;
                    bool isFirstIteration = true;

                    if (!hasEnumerator)
                        Enumerator = Source.GetEnumerator();

                    while (_pathIndex < _path.Count)
                    {
                        if (hasEnumerator)
                            hasEnumerator = false;
                        else
                            EnsureEnumeratorMoveNext();

                        if (isFirstIteration)
                        {
                            MoveEnumeratorToObjectBegin();

                            isFirstIteration = false;
                        }

                        var currentSegment = _path[_pathIndex++];

                        if (currentSegment is JsonPathObjectSegment objectSegment)
                        {
                            if (Enumerator.Current.Type != JsonEnumerableItemType.ObjectBegin)
                                throw new Exception();

                            while (true)
                            {
                                EnsureEnumeratorMoveNext();

                                if (Enumerator.Current.Type == JsonEnumerableItemType.ObjectEnd)
                                    throw new Exception();

                                if (Enumerator.Current.Type != JsonEnumerableItemType.Key)
                                    throw new Exception();

                                var key = (string)Enumerator.Current.Value;

                                if (key == objectSegment.Value)
                                    break;

                                SkipOne();
                            }
                        }
                        else if (currentSegment is JsonPathArraySegment arraySegment)
                        {
                            if (Enumerator.Current.Type != JsonEnumerableItemType.ArrayBegin)
                                throw new Exception();

                            var i = 0;
                            while (i++ < arraySegment.Value)
                            {
                                EnsureEnumeratorMoveNext();

                                if (Enumerator.Current.Type == JsonEnumerableItemType.ArrayEnd)
                                    throw new Exception();

                                SkipOne();
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }

                    State = 2;
                    goto case 2;
                case 2:
                    EnsureEnumeratorMoveNext();

                    if (Enumerator.Current.Type == JsonEnumerableItemType.Key)
                    {
                    }
                    else if (Enumerator.Current.Type == JsonEnumerableItemType.Value)
                    {
                        State = 4;
                    }
                    else if (new[] { JsonEnumerableItemType.ObjectBegin, JsonEnumerableItemType.ArrayBegin }.Contains(Enumerator.Current.Type))
                    {
                        _depth = 1;
                        State = 3;
                    }
                    else
                    {
                        throw new Exception();
                    }

                    Current = Enumerator.Current;
                    return true;
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
