using System;
using System.Collections.Generic;
using System.Linq;

namespace PinkJson2
{
    public sealed class JsonParser
    {
        private JsonLexer _lexer;
        private IEnumerator<Token> _enumerator;
        private Stack<string> _path = new Stack<string>();

        private JsonParser(JsonLexer lexer)
        {
            _lexer = lexer;
            _enumerator = lexer.GetEnumerator();
        }

        public static IJson Parse(JsonLexer lexer)
        {
            return new JsonParser(lexer).Parse();
        }

        private IJson Parse()
        {
            TryMoveNext();
            _path.Push("root");
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
                    throw new InvalidJsonFormatException();
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
                TryMoveNext();
                if (_enumerator.Current.Type == TokenType.RightBrace)
                    break;
                json.AddLast(ParseKeyValue());
                TryMoveNext();
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
            TryMoveNext();
            CheckToken(TokenType.Colon);
            TryMoveNext();
            var value = ParseValue();
            _path.Pop();
            return new JsonKeyValue(key, value);
        }

        private JsonArray ParseArray()
        {
            var json = new JsonArray();

            while (true)
            {
                TryMoveNext();
                if (_enumerator.Current.Type == TokenType.RightBracket)
                    break;
                _path.Push("_" + json.Count);
                json.AddLast(new JsonArrayValue(ParseValue()));
                _path.Pop();
                TryMoveNext();
                if (_enumerator.Current.Type == TokenType.RightBracket)
                    break;
                CheckToken(TokenType.Comma);
            }

            return json;
        }

        private void TryMoveNext()
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
                throw new UnexpectedEndOfStreamException();
        }

        private void CheckToken(params TokenType[] tokenTypes)
        {
            if (!tokenTypes.Contains(_enumerator.Current.Type))
                throw new UnexpectedTokenException(_enumerator.Current, tokenTypes, _path);
        }
    }
}
