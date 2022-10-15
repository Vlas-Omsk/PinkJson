using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PinkJson2
{
    public sealed class JsonParser : IEnumerable<JsonEnumerableItem>
    {
        private readonly IEnumerable<Token> _lexer;

        private sealed class JsonParserEnumerator : IEnumerator<JsonEnumerableItem>
        {
            private readonly IEnumerable<Token> _source;
            private readonly Stack<IJsonPathSegment> _path = new Stack<IJsonPathSegment>();
            private IEnumerator<Token> _enumerator;
            private int _state = 1;
            private readonly Stack<int> _nextState = new Stack<int>();
            private int _arrayIndex;

            public JsonParserEnumerator(IEnumerable<Token> source)
            {
                _source = source;
            }

            public JsonEnumerableItem Current { get; private set; }

            private JsonPath Path => new JsonPath(_path.Reverse().ToArray());
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        if (_enumerator == null)
                            _enumerator = _source.GetEnumerator();

                        EnsureEnumeratorMoveNext(TokenType.LeftBrace, TokenType.LeftBracket);

                        _nextState.Push(999);
                        _state = 2;
                        goto case 2;

                    // Parse value
                    case 2:
                        switch (_enumerator.Current.Type)
                        {
                            case TokenType.LeftBrace:
                                Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);

                                _state = 3;
                                break;
                            case TokenType.LeftBracket:
                                _arrayIndex = 0;
                                Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);

                                _state = 8;
                                break;
                            case TokenType.Boolean:
                            case TokenType.Null:
                            case TokenType.Number:
                            case TokenType.String:
                                Current = new JsonEnumerableItem(JsonEnumerableItemType.Value, _enumerator.Current.Value);

                                _state = _nextState.Pop();
                                break;
                            default:
                                throw new UnexpectedTokenException(
                                    _enumerator.Current,
                                    new TokenType[]
                                    {
                                        TokenType.LeftBrace,
                                        TokenType.LeftBracket,
                                        TokenType.Boolean,
                                        TokenType.Null,
                                        TokenType.Number,
                                        TokenType.String
                                    },
                                    Path
                                );
                        }

                        return true;

                    // Parse object
                    case 3:
                        EnsureEnumeratorMoveNext(TokenType.RightBrace, TokenType.String);
                        if (_enumerator.Current.Type == TokenType.RightBrace)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);

                            _state = _nextState.Pop();
                            return true;
                        }

                        var key = (string)_enumerator.Current.Value;
                        _path.Push(new JsonPathObjectSegment(key));
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, key);

                        _state = 6;
                        return true;
                    case 6:
                        EnsureEnumeratorMoveNext(TokenType.Colon);
                        EnsureEnumeratorMoveNextValue();

                        _nextState.Push(5);
                        _state = 2;
                        goto case 2;
                    case 5:
                        _path.Pop();

                        EnsureEnumeratorMoveNext(TokenType.RightBrace, TokenType.Comma);
                        if (_enumerator.Current.Type == TokenType.RightBrace)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);

                            _state = _nextState.Pop();
                            return true;
                        }

                        _state = 3;
                        goto case 3;

                    // Parse array
                    case 8:
                        EnsureEnumeratorMoveNext(TokenType.RightBracket, TokenType.LeftBrace, TokenType.LeftBracket, TokenType.Boolean, TokenType.Null, TokenType.Number, TokenType.String);
                        if (_enumerator.Current.Type == TokenType.RightBracket)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);

                            _state = _nextState.Pop();
                            return true;
                        }
                        _path.Push(new JsonPathArraySegment(_arrayIndex++));

                        _nextState.Push(9);
                        _state = 2;
                        goto case 2;
                    case 9:
                        _path.Pop();
                        EnsureEnumeratorMoveNext(TokenType.RightBracket, TokenType.Comma);
                        if (_enumerator.Current.Type == TokenType.RightBracket)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);

                            _state = _nextState.Pop();
                            return true;
                        }
                        CheckToken(TokenType.Comma);

                        _state = 8;
                        goto case 8;
                }

                Dispose();
                return false;
            }

            private void EnsureEnumeratorMoveNextValue()
            {
                EnsureEnumeratorMoveNext(TokenType.LeftBrace, TokenType.LeftBracket, TokenType.Boolean, TokenType.Null, TokenType.Number, TokenType.String);
            }

            public void Reset()
            {
                if (_enumerator != null)
                    _enumerator.Reset();

                _path.Clear();
                _nextState.Clear();
                Current = default;
                _state = 1;
            }

            public void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                Debug.Assert(_path.Count == 0, "_path was not empty");
                Debug.Assert(_nextState.Count == 0, "_nextState was not empty");

                _path.Clear();
                _nextState.Clear();
                Current = default;
                _state = -1;
            }

            private void EnsureEnumeratorMoveNext(params TokenType[] expectedTokenTypes)
            {
                bool success;
                try
                {
                    success = _enumerator.MoveNext();
                }
                catch (JsonLexerException ex)
                {
                    throw new JsonParserException("See inner exception", Path, ex);
                }
                if (!success)
                    throw new UnexpectedEndOfStreamException(expectedTokenTypes, Path);
                CheckToken(expectedTokenTypes);
            }

            private void CheckToken(params TokenType[] expectedTokenTypes)
            {
                if (!expectedTokenTypes.Contains(_enumerator.Current.Type))
                    throw new UnexpectedTokenException(_enumerator.Current, expectedTokenTypes, Path);
            }
        }

        public JsonParser(IEnumerable<Token> lexer)
        {
            _lexer = lexer;
        }

        public IEnumerator<JsonEnumerableItem> GetEnumerator()
        {
            return new JsonParserEnumerator(_lexer);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
