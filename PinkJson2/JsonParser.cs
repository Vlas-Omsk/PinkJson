using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2
{
    public sealed class JsonParser
    {
        private readonly IEnumerator<Token> _enumerator;
        private readonly Stack<string> _path = new Stack<string>();

        private const string _rootObjectName = "root";

        private JsonParser(JsonLexer lexer)
        {
            _enumerator = lexer.GetEnumerator();
        }

        public static IJson Parse(JsonLexer lexer)
        {
            return new JsonParser(lexer).Parse();
        }

        private IJson Parse()
        {
            _path.Push(_rootObjectName);
            TryMoveNext(TokenType.LeftBrace, TokenType.LeftBracket);
            IJson json;

            switch (_enumerator.Current.Type)
            {
                case TokenType.LeftBrace:
                    json = ParseObject();
                    break;
                case TokenType.LeftBracket:
                    json = ParseArray();
                    break;
                default:
                    _path.Pop();
                    throw new InvalidJsonFormatException(new string[] { _rootObjectName });
            }

            _path.Pop();
            return json;
        }

        private object ParseValue()
        {
            switch (_enumerator.Current.Type)
            {
                case TokenType.LeftBrace:
                    return ParseObject();
                case TokenType.LeftBracket:
                    return ParseArray();
                case TokenType.Boolean:
                case TokenType.Null:
                case TokenType.Number:
                case TokenType.String:
                    return _enumerator.Current.Value;
                default:
                    throw new UnexpectedTokenException(_enumerator.Current, new TokenType[] { TokenType.LeftBrace, TokenType.LeftBracket, TokenType.Boolean, TokenType.Null, TokenType.Number, TokenType.String }, _path);
            }
        }

        private JsonObject ParseObject()
        {
            var json = new JsonObject();

            while (true)
            {
                TryMoveNext(TokenType.RightBrace, TokenType.String);
                if (_enumerator.Current.Type == TokenType.RightBrace)
                    break;
                json.AddLast(ParseKeyValue());
                TryMoveNext(TokenType.RightBrace, TokenType.Comma);
                if (_enumerator.Current.Type == TokenType.RightBrace)
                    break;
                CheckToken(TokenType.Comma);
            }

            return json;
        }

        private JsonKeyValue ParseKeyValue()
        {
            CheckToken(TokenType.String);
            var key = (string)_enumerator.Current.Value;
            _path.Push(key);
            TryMoveNext(TokenType.Colon);
            CheckToken(TokenType.Colon);
            TryMoveNext(TokenType.LeftBrace, TokenType.LeftBracket, TokenType.Boolean, TokenType.Null, TokenType.Number, TokenType.String);
            var value = ParseValue();
            _path.Pop();
            return new JsonKeyValue(key, value);
        }

        private JsonArray ParseArray()
        {
            var json = new JsonArray();

            while (true)
            {
                TryMoveNext(TokenType.RightBracket, TokenType.LeftBrace, TokenType.LeftBracket, TokenType.Boolean, TokenType.Null, TokenType.Number, TokenType.String);
                if (_enumerator.Current.Type == TokenType.RightBracket)
                    break;
                _path.Push("_" + json.Count);
                json.AddLast(new JsonArrayValue(ParseValue()));
                _path.Pop();
                TryMoveNext(TokenType.RightBracket, TokenType.Comma);
                if (_enumerator.Current.Type == TokenType.RightBracket)
                    break;
                CheckToken(TokenType.Comma);
            }

            return json;
        }

        private void TryMoveNext(params TokenType[] expectedTokenTypes)
        {
            bool success;
            try
            {
                success = _enumerator.MoveNext();
            }
            catch(JsonLexerException ex)
            {
                throw new JsonParserException("See inner exception", _path, ex);
            }
            if (!success)
                throw new UnexpectedEndOfStreamException(expectedTokenTypes, _path);
        }

        private void CheckToken(params TokenType[] expectedTokenTypes)
        {
            if (!expectedTokenTypes.Contains(_enumerator.Current.Type))
                throw new UnexpectedTokenException(_enumerator.Current, expectedTokenTypes, _path);
        }
    }
}
