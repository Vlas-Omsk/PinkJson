using System;
using System.Collections;
using System.Collections.Generic;

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
            private State _state = State.Start;
            private readonly Stack<State> _nextState = new Stack<State>();

            private enum State
            {
                Start = 1,
                ParseObjectBegin = 3,
                ParseKeyValue = 6,
                ParseObjectEnd = 5,
                ParseArrayBegin = 8,
                ParseArrayEnd = 9,
                End = 999,
                Disposed
            }

            public Enumerator(IEnumerable<Token> enumerator)
            {
                _source = enumerator;
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
                    case State.Start:
                        if (_enumerator == null)
                            _enumerator = _source.GetEnumerator();

                        EnsureEnumeratorMoveNext(TokenType.LeftBrace, TokenType.LeftBracket);

                        if (_enumerator.Current.Type != TokenType.LeftBrace && _enumerator.Current.Type != TokenType.LeftBracket)
                            throw new UnexpectedTokenException(
                                _enumerator.Current,
                                new TokenType[]
                                {
                                    TokenType.LeftBrace,
                                    TokenType.LeftBracket
                                },
                                Path
                            );

                        _nextState.Push(State.End);
                        Current = ParseValue();
                        return true;

                    // Parse object
                    case State.ParseObjectBegin:
                        Current = ParseObjectBegin();
                        return true;
                    case State.ParseKeyValue:
                        Current = ParseKeyValue();
                        return true;
                    case State.ParseObjectEnd:
                        Current = ParseObjectEnd();
                        return true;

                    // Parse array
                    case State.ParseArrayBegin:
                        Current = ParseArrayBegin();
                        return true;
                    case State.ParseArrayEnd:
                        Current = ParseArrayEnd();
                        return true;
                }

                return false;
            }

            private JsonEnumerableItem ParseValue()
            {
                switch (_enumerator.Current.Type)
                {
                    case TokenType.LeftBrace:
                        _state = State.ParseObjectBegin;
                        return new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);
                    case TokenType.LeftBracket:
#if USEPATH
                        _arrayIndex = 0;
#endif
                        _state = State.ParseArrayBegin;
                        return new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);
                    case TokenType.Boolean:
                    case TokenType.Null:
                    case TokenType.Number:
                    case TokenType.String:
                        _state = _nextState.Pop();
                        return new JsonEnumerableItem(JsonEnumerableItemType.Value, _enumerator.Current.Value);
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
            }

            private JsonEnumerableItem ParseObjectBegin()
            {
                EnsureEnumeratorMoveNext(TokenType.RightBrace, TokenType.String);

                switch (_enumerator.Current.Type)
                {
                    case TokenType.RightBrace:
                        _state = _nextState.Pop();
                        return new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);
                    case TokenType.String:
                        var key = (string)_enumerator.Current.Value;
#if USEPATH
                        _path.Push(new JsonPathObjectSegment(key));
#endif
                        _state = State.ParseKeyValue;
                        return new JsonEnumerableItem(JsonEnumerableItemType.Key, key);
                    default:
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
            }

            private JsonEnumerableItem ParseKeyValue()
            {
                EnsureEnumeratorMoveNext(TokenType.Colon);

                if (_enumerator.Current.Type != TokenType.Colon)
                    throw new UnexpectedTokenException(
                        _enumerator.Current,
                        new TokenType[]
                        {
                            TokenType.Colon
                        },
                        Path
                    );

                EnsureEnumeratorMoveNext(
                    TokenType.LeftBrace,
                    TokenType.LeftBracket,
                    TokenType.Boolean,
                    TokenType.Null,
                    TokenType.Number,
                    TokenType.String
                );

                _nextState.Push(State.ParseObjectEnd);
                return ParseValue();
            }

            private JsonEnumerableItem ParseObjectEnd()
            {
#if USEPATH
                _path.Pop();
#endif

                EnsureEnumeratorMoveNext(TokenType.RightBrace, TokenType.Comma);

                switch (_enumerator.Current.Type)
                {
                    case TokenType.RightBrace:
                        _state = _nextState.Pop();
                        return new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);
                    case TokenType.Comma:
                        return ParseObjectBegin();
                    default:
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
            }

            private JsonEnumerableItem ParseArrayBegin()
            {
                EnsureEnumeratorMoveNext(
                    TokenType.RightBracket,
                    TokenType.LeftBrace,
                    TokenType.LeftBracket,
                    TokenType.Boolean,
                    TokenType.Null,
                    TokenType.Number,
                    TokenType.String
                );

                if (_enumerator.Current.Type == TokenType.RightBracket)
                {
                    _state = _nextState.Pop();
                    return new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);
                }
#if USEPATH
                _path.Push(new JsonPathArraySegment(_arrayIndex++));
#endif

                _nextState.Push(State.ParseArrayEnd);
                return ParseValue();
            }

            private JsonEnumerableItem ParseArrayEnd()
            {
#if USEPATH
                _path.Pop();
#endif
                EnsureEnumeratorMoveNext(TokenType.RightBracket, TokenType.Comma);

                switch (_enumerator.Current.Type)
                {
                    case TokenType.RightBracket:
                        _state = _nextState.Pop();
                        return new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);
                    case TokenType.Comma:
                        return ParseArrayBegin();
                    default:
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

            public void Reset()
            {
                if (_state == State.Disposed)
                    throw new ObjectDisposedException(GetType().FullName);

                if (_enumerator != null)
                    _enumerator.Reset();

#if USEPATH
                _path.Clear();
#endif
                _nextState.Clear();

                Current = default;
                _state = State.Start;
            }

            public void Dispose()
            {
                if (_state == State.Disposed)
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
                _state = State.Disposed;
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
