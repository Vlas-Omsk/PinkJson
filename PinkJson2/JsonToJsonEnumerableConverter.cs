using System;
using System.Collections;
using System.Collections.Generic;

namespace PinkJson2
{
    public sealed class JsonToJsonEnumerableConverter : IEnumerable<JsonEnumerableItem>
    {
        private readonly IJson _json;

        private class Enumerator : IEnumerator<JsonEnumerableItem>
        {
            private readonly Stack<object> _stack = new Stack<object>();
            private readonly Stack<int> _nextState = new Stack<int>();
            private int _state = 1;

            public Enumerator(IJson json)
            {
                _stack.Push(json);
            }

            public JsonEnumerableItem Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _nextState.Push(999);
                        goto case 2;
                    case 2:
                        Current = ConvertValue();
                        return true;
                    case 3:
                        Current = ConvertObject();
                        return true;
                    case 4:
                        Current = ConvertArray();
                        return true;
                }

                return false;
            }

            private JsonEnumerableItem ConvertValue()
            {
                var obj = _stack.Pop();

                if (obj is JsonObject jsonObject)
                {
                    _stack.Push(jsonObject.First);
                    _state = 3;
                    return new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                }
                else if (obj is JsonArray jsonArray)
                {
                    _stack.Push(jsonArray.First);
                    _state = 4;
                    return new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);
                }
                else if (obj is JsonKeyValue jsonKeyValue)
                {
                    _stack.Push(jsonKeyValue.Value);
                    _state = 2;
                    return new JsonEnumerableItem(JsonEnumerableItemType.Key, jsonKeyValue.Key);
                }
                else if (obj is JsonArrayValue jsonArrayValue)
                {
                    _stack.Push(jsonArrayValue.Value);
                    return ConvertValue();
                }
                else
                {
                    _state = _nextState.Pop();
                    return new JsonEnumerableItem(JsonEnumerableItemType.Value, obj);
                }
            }

            private JsonEnumerableItem ConvertObject()
            {
                var objectNode = (LinkedListNode<JsonKeyValue>)_stack.Pop();

                if (objectNode?.Value == null)
                {
                    _state = _nextState.Pop();
                    return new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);
                }

                _nextState.Push(3);
                _stack.Push(objectNode.Next);
                _stack.Push(objectNode.Value);
                return ConvertValue();
            }

            private JsonEnumerableItem ConvertArray()
            {
                var arrayNode = (LinkedListNode<JsonArrayValue>)_stack.Pop();

                if (arrayNode?.Value == null)
                {
                    _state = _nextState.Pop();
                    return new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);
                }

                _nextState.Push(4);
                _stack.Push(arrayNode.Next);
                _stack.Push(arrayNode.Value);
                return ConvertValue();
            }

            public void Reset()
            {
                if (_state == -1)
                    throw new ObjectDisposedException(GetType().FullName);

                _stack.Clear();
                _nextState.Clear();

                Current = default;
                _state = 1;
            }

            public void Dispose()
            {
                if (_state == -1)
                    return;

                _stack.Clear();
                _nextState.Clear();

                Current = default;
                _state = -1;
            }
        }

        public JsonToJsonEnumerableConverter(IJson json)
        {
            _json = json;
        }

        public IEnumerator<JsonEnumerableItem> GetEnumerator()
        {
            return new Enumerator(_json);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
