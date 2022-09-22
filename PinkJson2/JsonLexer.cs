using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PinkJson2
{
    public sealed class JsonLexer : IEnumerable<Token>
    {
        public StreamReader Stream { get; }

        private int _position;
        private int _startPosition;
        private Token _token;
        private readonly StringBuilder _buffer = new StringBuilder();
        private bool _useBuffer = false;
        private char? _current;
        private char? _next;
        private readonly StringBuilder _stringBuffer = new StringBuilder();
        private static readonly char[] numberChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', '.', 'x', 'o', 'b' };
        private static readonly char[] hexadecimalChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        public JsonLexer(StreamReader stream)
        {
            Stream = stream;
        }

        public JsonLexer(Stream stream) : this(new StreamReader(stream))
        {
        }

        public JsonLexer(Stream stream, Encoding encoding) : this(new StreamReader(stream, encoding))
        {
        }

        public JsonLexer(string source)
        {
            Stream = new StreamReader(new MemoryStream(Encoding.Default.GetBytes(source)));
        }

        private char? Next
        {
            get
            {
                // if Stream.BaseStream is ChunkedEncodingReadStream Stream.Peek return -1 if position in end of chunk
                if (!_next.HasValue)
                {
                    var next = Stream.Read();
                    _next = next == -1 ? null : (char?)next;
                }
                return _next;
            }
        }

        private int BufferLength
        {
            get => _buffer.Length;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            Stream.DiscardBufferedData();
            _position = -1;
            _startPosition = 0;
            while (Next.HasValue)
            {
                Get();

                if (_token.Type == TokenType.Invalid)
                    throw new InvalidTokenException(_startPosition, Stream);
                if (_token.Type != TokenType.Invisible)
                    yield return _token;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void ReadNext()
        {
            _position++;
            if (_next.HasValue)
            {
                _current = _next;
                _next = null;
            }
            else
            {
                var current = Stream.Read();
                _current = current == -1 ? null : (char?)current;
            }
            if (_useBuffer && _current.HasValue)
                _buffer.Append(_current.Value);
        }

        private void EnableBuffer()
        {
            _useBuffer = true;
            _buffer.Append(_current.Value);
        }

        private string DisableBuffer()
        {
            _useBuffer = false;
            var str = _buffer.ToString();
            _buffer.Clear();
            return str;
        }

        private void Get()
        {
            _token = new Token();
            ReadNext();
            _token.Position = _startPosition = _position;

            switch (_current.Value)
            {
                case ':':
                    _token.Type = TokenType.Colon;
                    break;
                case ',':
                    _token.Type = TokenType.Comma;
                    break;
                case '{':
                    _token.Type = TokenType.LeftBrace;
                    break;
                case '}':
                    _token.Type = TokenType.RightBrace;
                    break;
                case '[':
                    _token.Type = TokenType.LeftBracket;
                    break;
                case ']':
                    _token.Type = TokenType.RightBracket;
                    break;
                case '+':
                case '-':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '0':
                    ReadNumber();
                    break;
                case '"':
                    ReadString();
                    break;
                default:
                    if (_current.Value == '\0' || char.IsWhiteSpace(_current.Value))
                        ReadWhiteSpace();
                    else if (char.IsLetterOrDigit(_current.Value))
                        ReadOther();
                    break;
            }

            _token.Length = _position - _startPosition;
        }

        private void ReadWhiteSpace()
        {
            while (Next.HasValue && (Next.Value == '\0' || char.IsWhiteSpace(Next.Value)))
                ReadNext();
            _token.Type = TokenType.Invisible;
        }

        private void ReadNumber()
        {
            EnableBuffer();

            var isDouble = false;
            var isEnumber = false;
            var valueBase = 10;

            while (Next.HasValue && 
                (numberChars.Contains(char.ToLowerInvariant(Next.Value)) || (char.ToLowerInvariant(_current.Value) == 'e' && ((isEnumber && Next.Value == '-') || (isEnumber && Next.Value == '+')))))
            {
                var previous = _current.Value;
                ReadNext();

                var lowerCurrent = char.ToLowerInvariant(_current.Value);
                if (lowerCurrent == '.' && valueBase == 10)
                {
                    if (isDouble)
                        throw new JsonLexerException("Invalid double number", _position, Stream);
                    else
                        isDouble = true;
                }
                if (lowerCurrent == 'e' && valueBase == 10)
                {
                    if (isEnumber)
                        throw new JsonLexerException("Invalid e number", _position, Stream);
                    else
                        isEnumber = true;
                }
                else if (lowerCurrent == 'x')
                {
                    if (valueBase == 16 || previous != '0' || BufferLength > 2)
                        throw new JsonLexerException("Invalid hexadecimal number", _position, Stream);
                    else
                        valueBase = 16;
                }
                else if (lowerCurrent == 'o')
                {
                    if (valueBase == 8 || previous != '0' || BufferLength > 2)
                        throw new JsonLexerException("Invalid octal number", _position, Stream);
                    else
                        valueBase = 8;
                }
                else if (lowerCurrent == 'b' && previous == '0' && BufferLength == 2)
                {
                    if (valueBase == 2)
                        throw new JsonLexerException("Invalid binary number", _position, Stream);
                    else
                        valueBase = 2;
                }
                else
                {
                    var index = Array.IndexOf(hexadecimalChars, lowerCurrent);
                    if (index >= valueBase)
                        throw new JsonLexerException($"Invalid character '{_current}' for {valueBase}-based number", _position, Stream);
                }
            }

            var buffer = DisableBuffer();

            if (isEnumber || isDouble)
            {
                if (!double.TryParse(buffer.Replace('.', ','), out double value))
                    throw new JsonLexerException($"Invalid double number {_buffer}", _startPosition, Stream);
                _token.Value = value;
            }
            else if (valueBase != 10)
            {
                var prefix = buffer.Substring(0, 2);
                buffer = buffer.Substring(2);
                try
                {
                    _token.Value = Convert.ToInt32(buffer, valueBase);
                }
                catch
                {
                    try
                    {
                        _token.Value = Convert.ToInt64(buffer, valueBase);
                    }
                    catch (Exception ex)
                    {
                        throw new JsonLexerException($"Invalid number {prefix}{_buffer}", _startPosition, Stream, ex);
                    }
                }
            }
            else
            {
                if (int.TryParse(buffer, out int intvalue))
                    _token.Value = intvalue;
                else if (long.TryParse(buffer, out long longvalue))
                    _token.Value = longvalue;
                //else if (BigInteger.TryParse(str, out BigInteger bigintvalue))
                //    _token.Value = bigintvalue;
                else
                {
                    if (BufferLength < 40)
                        throw new JsonLexerException($"Invalid or too big number {_buffer}", _startPosition, Stream);
                    else
                        throw new JsonLexerException($"Well.. It's seriously big number {_buffer}. I.. I even can't imagine how to handle it. No, seriously. Maybe you know how?", _startPosition, Stream);
                }
            }

            _token.Type = TokenType.Number;
        }

        private void ReadString()
        {
            var escape = false;

            _stringBuffer.Clear();

            while (Next.HasValue && (escape || Next.Value != '"'))
            {
                ReadNext();

                if (escape)
                {
                    escape = false;
                    switch (_current.Value)
                    {
                        case 'b':
                            _stringBuffer.Append('\b');
                            break;
                        case 'a':
                            _stringBuffer.Append('\a');
                            break;
                        case 'f':
                            _stringBuffer.Append('\f');
                            break;
                        case 'n':
                            _stringBuffer.Append('\n');
                            break;
                        case 'r':
                            _stringBuffer.Append('\r');
                            break;
                        case 't':
                            _stringBuffer.Append('\t');
                            break;
                        case '0':
                            _stringBuffer.Append('\0');
                            break;
                        case 'u':
                            string unicode_value = "";
                            for (var i = 0; i < 4; i++)
                            {
                                if (!Next.HasValue || !hexadecimalChars.Contains(char.ToLowerInvariant(Next.Value)))
                                    throw new JsonLexerException($"The Unicode value must be hexadecimal and 4 characters long", _position - i - 1, Stream);
                                ReadNext();
                                unicode_value += _current.Value;
                            }
                            _stringBuffer.Append((char)Convert.ToInt32(unicode_value, 16));
                            break;
                        case '"':
                            _stringBuffer.Append('\"');
                            break;
                        case '\\':
                            _stringBuffer.Append('\\');
                            break;
                        case '/':
                            _stringBuffer.Append('/');
                            break;
                        default:
                            throw new JsonLexerException($"Unidentified escape sequence \\{_current}", _position - 1, Stream);
                    }
                }
                else if (_current.Value == '\\')
                {
                    escape = true;
                }
                else
                {
                    _stringBuffer.Append(_current.Value);
                }
            }

            ReadNext();

            _token.Type = TokenType.String;
            _token.Value = _stringBuffer.ToString();
        }

        private void ReadOther()
        {
            EnableBuffer();

            while (Next.HasValue && char.IsLetterOrDigit(Next.Value))
                ReadNext();

            var buffer = DisableBuffer();

            if (buffer == "null")
            {
                _token.Type = TokenType.Null;
            }
            else if (bool.TryParse(buffer, out bool value))
            {
                _token.Value = value;
                _token.Type = TokenType.Boolean;
            }
        }
    }
}
