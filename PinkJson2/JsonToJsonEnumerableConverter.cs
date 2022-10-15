using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PinkJson2
{
    public sealed class JsonToJsonEnumerableConverter : IEnumerable<JsonEnumerableItem>
    {
        private readonly IJson _json;

        private class JsonToJsonEnumerableConverterEnumerator : IEnumerator<JsonEnumerableItem>
        {
            private readonly Stack<object> _stack = new Stack<object>();
            private readonly Stack<LinkedListNode<JsonKeyValue>> _objectIndex = new Stack<LinkedListNode<JsonKeyValue>>();
            private readonly Stack<LinkedListNode<JsonArrayValue>> _arrayIndex = new Stack<LinkedListNode<JsonArrayValue>>();
            private readonly Stack<int> _nextState = new Stack<int>();
            private int _state = 1;

            public JsonToJsonEnumerableConverterEnumerator(IJson json)
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
                        _state = 2;
                        goto case 2;
                    case 2:
                        var obj = _stack.Pop();

                        if (obj is JsonObject jsonObject)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);

                            _objectIndex.Push(jsonObject.First);
                            _state = 3;
                        }
                        else if (obj is JsonArray jsonArray)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);

                            _arrayIndex.Push(jsonArray.First);
                            _state = 4;
                        }
                        else if (obj is JsonKeyValue jsonKeyValue)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, jsonKeyValue.Key);

                            _stack.Push(jsonKeyValue.Value);
                        }
                        else if (obj is JsonArrayValue jsonArrayValue)
                        {
                            _stack.Push(jsonArrayValue.Value);
                            goto case 2;
                        }
                        else
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.Value, obj);

                            _state = _nextState.Pop();
                        }
                        return true;
                    case 3:
                        var objectNode = _objectIndex.Pop();

                        if (objectNode?.Value == null)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);

                            _state = _nextState.Pop();
                            return true;
                        }

                        _nextState.Push(3);
                        _objectIndex.Push(objectNode.Next);
                        _stack.Push(objectNode.Value);
                        _state = 2;
                        goto case 2;
                    case 4:
                        var arrayNode = _arrayIndex.Pop();

                        if (arrayNode?.Value == null)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);

                            _state = _nextState.Pop();
                            return true;
                        }

                        _nextState.Push(4);
                        _arrayIndex.Push(arrayNode.Next);
                        _stack.Push(arrayNode.Value);
                        _state = 2;
                        goto case 2;
                }

                Dispose();
                return false;
            }

            public void Reset()
            {
                _stack.Clear();
                _objectIndex.Clear();
                _arrayIndex.Clear();
                _nextState.Clear();
                Current = default;
                _state = 1;
            }

            public void Dispose()
            {
                Debug.Assert(_stack.Count == 0, "_stack was not empty");
                Debug.Assert(_objectIndex.Count == 0, "_objectIndex was not empty");
                Debug.Assert(_arrayIndex.Count == 0, "_arrayIndex was not empty");
                Debug.Assert(_nextState.Count == 0, "_nextState was not empty");

                _stack.Clear();
                _objectIndex.Clear();
                _arrayIndex.Clear();
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
            return new JsonToJsonEnumerableConverterEnumerator(_json);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
