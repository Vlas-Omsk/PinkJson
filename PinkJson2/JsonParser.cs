using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2
{
    public sealed class JsonParser : IEnumerable<JsonEnumerableItem>
    {
        private readonly IEnumerable<Token> _lexer;

        private sealed class JsonParserEnumerator : IEnumerator<JsonEnumerableItem>
        {
            private readonly IEnumerator<Token> _enumerator;
            private readonly Stack<IJsonPathSegment> _path = new Stack<IJsonPathSegment>();
            private IEnumerator<JsonEnumerableItem> _jsonEnumerator;

            public JsonParserEnumerator(IEnumerator<Token> enumerator)
            {
                _enumerator = enumerator;
                _jsonEnumerator = GetJsonEnumerable().GetEnumerator();
            }

            public JsonEnumerableItem Current => _jsonEnumerator.Current;
            object IEnumerator.Current => Current;

            private JsonPath Path => new JsonPath(_path.Reverse().ToArray());

            public bool MoveNext()
            {
                return _jsonEnumerator.MoveNext();
            }

            public void Reset()
            {
                _path.Clear();
                _jsonEnumerator = GetJsonEnumerable().GetEnumerator();
            }

            public void Dispose()
            {
                _jsonEnumerator.Dispose();
                _enumerator.Dispose();
            }

            private IEnumerable<JsonEnumerableItem> GetJsonEnumerable()
            {
                TryMoveNext(TokenType.LeftBrace, TokenType.LeftBracket);
                IEnumerable<JsonEnumerableItem> enumerable;

                switch (_enumerator.Current.Type)
                {
                    case TokenType.LeftBrace:
                        enumerable = ParseObject();
                        break;
                    case TokenType.LeftBracket:
                        enumerable = ParseArray();
                        break;
                    default:
                        throw new InvalidJsonFormatException(Path);
                }

                return enumerable;
            }

            private IEnumerable<JsonEnumerableItem> ParseValue()
            {
                switch (_enumerator.Current.Type)
                {
                    case TokenType.LeftBrace:
                        foreach (var item in ParseObject())
                            yield return item;
                        break;
                    case TokenType.LeftBracket:
                        foreach (var item in ParseArray())
                            yield return item;
                        break;
                    case TokenType.Boolean:
                    case TokenType.Null:
                    case TokenType.Number:
                    case TokenType.String:
                        yield return new JsonEnumerableItem(JsonEnumerableItemType.Value, _enumerator.Current.Value);
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
            }

            private IEnumerable<JsonEnumerableItem> ParseObject()
            {
                yield return new JsonEnumerableItem(JsonEnumerableItemType.ObjectBegin, null);

                while (true)
                {
                    TryMoveNext(TokenType.RightBrace, TokenType.String);
                    if (_enumerator.Current.Type == TokenType.RightBrace)
                        break;
                    foreach (var item in ParseKeyValue())
                        yield return item;
                    TryMoveNext(TokenType.RightBrace, TokenType.Comma);
                    if (_enumerator.Current.Type == TokenType.RightBrace)
                        break;
                    CheckToken(TokenType.Comma);
                }

                yield return new JsonEnumerableItem(JsonEnumerableItemType.ObjectEnd, null);
            }

            private IEnumerable<JsonEnumerableItem> ParseKeyValue()
            {
                CheckToken(TokenType.String);
                var key = (string)_enumerator.Current.Value;
                _path.Push(new JsonPathObjectSegment(key));
                yield return new JsonEnumerableItem(JsonEnumerableItemType.Key, key);
                TryMoveNext(TokenType.Colon);
                CheckToken(TokenType.Colon);
                TryMoveNext(TokenType.LeftBrace, TokenType.LeftBracket, TokenType.Boolean, TokenType.Null, TokenType.Number, TokenType.String);
                foreach (var item in ParseValue())
                    yield return item;
                _path.Pop();
            }

            private IEnumerable<JsonEnumerableItem> ParseArray()
            {
                yield return new JsonEnumerableItem(JsonEnumerableItemType.ArrayBegin, null);

                var i = 0;
                while (true)
                {
                    TryMoveNext(TokenType.RightBracket, TokenType.LeftBrace, TokenType.LeftBracket, TokenType.Boolean, TokenType.Null, TokenType.Number, TokenType.String);
                    if (_enumerator.Current.Type == TokenType.RightBracket)
                        break;
                    _path.Push(new JsonPathArraySegment(i++));
                    foreach (var item in ParseValue())
                        yield return item;
                    _path.Pop();
                    TryMoveNext(TokenType.RightBracket, TokenType.Comma);
                    if (_enumerator.Current.Type == TokenType.RightBracket)
                        break;
                    CheckToken(TokenType.Comma);
                }

                yield return new JsonEnumerableItem(JsonEnumerableItemType.ArrayEnd, null);
            }

            private void TryMoveNext(params TokenType[] expectedTokenTypes)
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
            return new JsonParserEnumerator(_lexer.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
