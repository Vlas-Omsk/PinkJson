using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2.Linq
{
    internal abstract class JsonIterator<TResult> : Iterator<JsonEnumerableItem, TResult>
    {
        protected JsonIterator(IEnumerable<JsonEnumerableItem> source, IEnumerator<JsonEnumerableItem> enumerator) : base(source, enumerator)
        {
        }

        protected void MoveEnumeratorToObjectBegin()
        {
            switch (Enumerator.Current.Type)
            {
                case JsonEnumerableItemType.Key:
                    EnsureEnumeratorMoveNext();
                    break;
                case JsonEnumerableItemType.ObjectBegin:
                case JsonEnumerableItemType.ArrayBegin:
                    break;
                default:
                    throw new UnexpectedJsonEnumerableItemException(
                        Enumerator.Current,
                        new JsonEnumerableItemType[]
                        {
                            JsonEnumerableItemType.Key,
                            JsonEnumerableItemType.ObjectBegin,
                            JsonEnumerableItemType.ArrayBegin
                        }
                    );
            }
        }

        protected JsonEnumerableItem[] ReadOne()
        {
            var list = new List<JsonEnumerableItem>();

            while (true)
            {
                switch (Enumerator.Current.Type)
                {
                    case JsonEnumerableItemType.Key:
                        list.Add(Enumerator.Current);
                        EnsureEnumeratorMoveNext();
                        break;
                    case JsonEnumerableItemType.Value:
                        list.Add(Enumerator.Current);
                        return list.ToArray();
                    case JsonEnumerableItemType.ObjectBegin:
                    case JsonEnumerableItemType.ArrayBegin:
                        var depth = 1;

                        list.Add(Enumerator.Current);

                        while (depth != 0)
                        {
                            EnsureEnumeratorMoveNext();

                            if (new[] { JsonEnumerableItemType.ObjectBegin, JsonEnumerableItemType.ArrayBegin }.Contains(Enumerator.Current.Type))
                                depth++;
                            if (new[] { JsonEnumerableItemType.ObjectEnd, JsonEnumerableItemType.ArrayEnd }.Contains(Enumerator.Current.Type))
                                depth--;

                            list.Add(Enumerator.Current);
                        }
                        return list.ToArray();
                    default:
                        throw new UnexpectedJsonEnumerableItemException(
                            Enumerator.Current,
                            new JsonEnumerableItemType[]
                            {
                                JsonEnumerableItemType.Key,
                                JsonEnumerableItemType.Value,
                                JsonEnumerableItemType.ObjectBegin,
                                JsonEnumerableItemType.ArrayBegin
                            }
                        );
                }
            }
        }

        protected void SkipOne()
        {
            while (true)
            {
                switch (Enumerator.Current.Type)
                {
                    case JsonEnumerableItemType.Key:
                        EnsureEnumeratorMoveNext();
                        break;
                    case JsonEnumerableItemType.Value:
                        return;
                    case JsonEnumerableItemType.ObjectBegin:
                    case JsonEnumerableItemType.ArrayBegin:
                        var depth = 1;
                        while (depth != 0)
                        {
                            EnsureEnumeratorMoveNext();

                            if (new[] { JsonEnumerableItemType.ObjectBegin, JsonEnumerableItemType.ArrayBegin }.Contains(Enumerator.Current.Type))
                                depth++;
                            if (new[] { JsonEnumerableItemType.ObjectEnd, JsonEnumerableItemType.ArrayEnd }.Contains(Enumerator.Current.Type))
                                depth--;
                        }
                        return;
                    default:
                        throw new UnexpectedJsonEnumerableItemException(
                            Enumerator.Current,
                            new JsonEnumerableItemType[]
                            {
                                JsonEnumerableItemType.Key,
                                JsonEnumerableItemType.Value,
                                JsonEnumerableItemType.ObjectBegin,
                                JsonEnumerableItemType.ArrayBegin
                            }
                        );
                }
            }
        }

        protected override void EnsureEnumeratorMoveNext()
        {
            if (!Enumerator.MoveNext())
                throw new UnexpectedEndOfJsonEnumerableException();
        }
    }
}
