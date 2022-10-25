using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2
{
    public sealed class JsonParser : IEnumerable<JsonEnumerableItem>
    {
        private readonly IEnumerable<Token> _lexer;

        private sealed class Enumerator : IEnumerator<JsonEnumerableItem>
        {
            private readonly IEnumerable<Token> _source;
#if USEPATH
            private readonly Stack<IJsonPathSegment> _path = new Stack<IJsonPathSegment>();
            private int _arrayIndex;
#endif
            private IEnumerator<Token> _enumerator;
            private int _state = 1;
            private readonly Stack<int> _nextState = new Stack<int>();

            public Enumerator(IEnumerable<Token> source)
            {
                _source = source;
            }

            public JsonEnumerableItem Current { get; private set; }

            private JsonPath Path =>
#if USEPATH
                new JsonPath(_path.Reverse().ToArray());
#else
                new JsonPath();
#endif
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        if (_enumerator == null)
                            _enumerator = _source.GetEnumerator();

                        EnsureEnumeratorMoveNext(TokenType.LeftBrace, TokenType.LeftBracket);
                        CheckToken(TokenType.LeftBrace, TokenType.LeftBracket);

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
#if USEPATH
                                _arrayIndex = 0;
#endif
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
                        else if (_enumerator.Current.Type != TokenType.String)
                        {
                            throw new UnexpectedTokenException(
                                _enumerator.Current,
                                new TokenType[]
                                {
                                    TokenType.RightBrace,
                                    TokenType.String
                                },
                                Path
                            );
                        }

                        var key = (string)_enumerator.Current.Value;
#if USEPATH
                        _path.Push(new JsonPathObjectSegment(key));
#endif
                        Current = new JsonEnumerableItem(JsonEnumerableItemType.Key, key);

                        _state = 6;
                        return true;
                    case 6:
                        EnsureEnumeratorMoveNext(TokenType.Colon);
                        if (_enumerator.Current.Type != TokenType.Colon)
                        {
                            throw new UnexpectedTokenException(
                                _enumerator.Current,
                                new TokenType[]
                                {
                                    TokenType.Colon
                                },
                                Path
                            );
                        }
                        EnsureEnumeratorMoveNextValue();

                        _nextState.Push(5);
                        _state = 2;
                        goto case 2;
                    case 5:
#if USEPATH
                        _path.Pop();
#endif

                        EnsureEnumeratorMoveNext(TokenType.RightBrace, TokenType.Comma);
                        if (_enumerator.Current.Type == TokenType.RightBrace)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);

                            _state = _nextState.Pop();
                            return true;
                        }
                        else if (_enumerator.Current.Type != TokenType.Comma)
                        {
                            throw new UnexpectedTokenException(
                                _enumerator.Current,
                                new TokenType[]
                                {
                                    TokenType.RightBrace,
                                    TokenType.Comma
                                },
                                Path
                            );
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
#if USEPATH
                        _path.Push(new JsonPathArraySegment(_arrayIndex++));
#endif

                        _nextState.Push(9);
                        _state = 2;
                        goto case 2;
                    case 9:
#if USEPATH
                        _path.Pop();
#endif
                        EnsureEnumeratorMoveNext(TokenType.RightBracket, TokenType.Comma);
                        if (_enumerator.Current.Type == TokenType.RightBracket)
                        {
                            Current = new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);

                            _state = _nextState.Pop();
                            return true;
                        }
                        else if (_enumerator.Current.Type != TokenType.Comma)
                        {
                            throw new UnexpectedTokenException(
                                _enumerator.Current,
                                new TokenType[]
                                {
                                    TokenType.RightBracket,
                                    TokenType.Comma
                                },
                                Path
                            );
                        }

                        _state = 8;
                        goto case 8;
                }

                return false;
            }

            public void Reset()
            {
                if (_state == -1)
                    throw new ObjectDisposedException(GetType().FullName);

                if (_enumerator != null)
                    _enumerator.Reset();

#if USEPATH
                _path.Clear();
#endif
                _nextState.Clear();

                Current = default;
                _state = 1;
            }

            public void Dispose()
            {
                if (_state == -1)
                    return;

                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

#if USEPATH
                _path.Clear();
#endif
                _nextState.Clear();

                Current = default;
                _state = -1;
            }

            private void EnsureEnumeratorMoveNextValue()
            {
                EnsureEnumeratorMoveNext(TokenType.LeftBrace, TokenType.LeftBracket, TokenType.Boolean, TokenType.Null, TokenType.Number, TokenType.String);
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
            }

            private void CheckToken(params TokenType[] expectedTokenTypes)
            {
                if (!expectedTokenTypes.Any(x => x == _enumerator.Current.Type))
                    throw new UnexpectedTokenException(_enumerator.Current, expectedTokenTypes, Path);
            }
        }

        public JsonParser(IEnumerable<Token> lexer)
        {
            _lexer = lexer;
        }

        public IEnumerator<JsonEnumerableItem> GetEnumerator()
        {
            return new Enumerator(_lexer);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
