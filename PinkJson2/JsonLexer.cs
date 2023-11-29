using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PinkJson2
{
    public sealed class JsonLexer : IEnumerable<Token>, IDisposable
    {
        private static readonly FieldInfo _streamReaderDetectEncodingField = 
            typeof(StreamReader).GetField("_detectEncoding", BindingFlags.NonPublic | BindingFlags.Instance);
        private Enumerator _enumerator;
        private readonly object _enumeratorLock = new object();
        private readonly bool _detectEncoding;

        private sealed class Enumerator : IEnumerator<Token>
        {
            private static readonly char[] _numberChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', '.', 'x', 'o', 'b' };
            private static readonly char[] _hexadecimalChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
            private readonly JsonLexer _lexer;
            private int _position = -1;
            private int _startPosition;
            private readonly StringBuilder _buffer = new StringBuilder();
            private bool _useBuffer = false;
            private char? _current;
            private char? _next;
            private int _state = 1;

            public Enumerator(JsonLexer lexer)
            {
                _lexer = lexer;
            }

            public Token Current { get; private set; }
            public bool Disposed => _state == -1;

            private StreamReader Stream => _lexer.Stream;
            private int BufferLength => _buffer.Length;
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_state == -1)
                    throw new ObjectDisposedException(nameof(Enumerator));

                switch (_state)
                {
                    case 1:
                        _next = ReadNextChar();
                        _state = 2;
                        goto case 2;
                    case 2:
                        while (_next.HasValue)
                        {
                            Current = Get();

                            if (Current.Type == TokenType.Invalid)
                                throw new InvalidTokenException(_startPosition, Stream);
                            if (Current.Type != TokenType.Invisible)
                                return true;
                        }
                        break;
                }

                return false;
            }

            private void ReadNext()
            {
                _position++;
                if (_next.HasValue)
                    _current = _next.Value;
                else
                    _current = ReadNextChar();
                // if Stream.BaseStream is ChunkedEncodingReadStream Stream.Peek return -1 if position in end of chunk
                _next = ReadNextChar();

                if (_useBuffer && _current.HasValue)
                    _buffer.Append(_current.Value);
            }

            private char? ReadNextChar()
            {
                var current = Stream.Read();
                return current == -1 ? null : (char?)current;
            }

            private void EnableBuffer()
            {
                _useBuffer = true;
                _buffer.Clear();
                _buffer.Append(_current.Value);
            }

            private void DisableBuffer()
            {
                _useBuffer = false;
            }

            private Token Get()
            {
                ReadNext();

                var tokenPosition = _startPosition = _position;
                TokenType tokenType;
                object tokenValue = null;

                switch (_current.Value)
                {
                    case ':':
                        tokenType = TokenType.Colon;
                        break;
                    case ',':
                        tokenType = TokenType.Comma;
                        break;
                    case '{':
                        tokenType = TokenType.LeftBrace;
                        break;
                    case '}':
                        tokenType = TokenType.RightBrace;
                        break;
                    case '[':
                        tokenType = TokenType.LeftBracket;
                        break;
                    case ']':
                        tokenType = TokenType.RightBracket;
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
                        tokenValue = ReadNumber();
                        tokenType = TokenType.Number;
                        break;
                    case '"':
                    case '\'':
                        tokenValue = ReadString();
                        tokenType = TokenType.String;
                        break;
                    default:
                        if (_current.Value == '\0' || char.IsWhiteSpace(_current.Value))
                        {
                            ReadWhiteSpace();
                            tokenType = TokenType.Invisible;
                        }
                        else if (char.IsLetterOrDigit(_current.Value))
                        {
                            (tokenType, tokenValue) = ReadOther();
                        }
                        else
                        {
                            throw new JsonLexerException("Character not recognized as valid", _position, Stream);
                        }
                        break;
                }

                return new Token(tokenType, tokenPosition, _position - _startPosition, tokenValue);
            }

            private void ReadWhiteSpace()
            {
                while (_next.HasValue && (_next.Value == '\0' || char.IsWhiteSpace(_next.Value)))
                    ReadNext();
            }

            private object ReadNumber()
            {
                EnableBuffer();

                var isDouble = false;
                var isEnumber = false;
                var valueBase = 10;
                var signed = _current.Value == '-' || _current.Value == '+';
                var sign = signed && _current.Value == '-' ? -1 : 1;
                var prefixLength = signed ? 3 : 2;

                while (
                    _next.HasValue &&
                    (
                        _numberChars.Contains(char.ToLowerInvariant(_next.Value)) || 
                        (
                            char.ToLowerInvariant(_current.Value) == 'e' &&
                            isEnumber &&
                            (
                                _next.Value == '-' || 
                                _next.Value == '+'
                            )
                        )
                    )
                )
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
                        if (valueBase == 16 || previous != '0' || BufferLength > prefixLength)
                            throw new JsonLexerException("Invalid hexadecimal number", _position, Stream);
                        else
                            valueBase = 16;
                    }
                    else if (lowerCurrent == 'o')
                    {
                        if (valueBase == 8 || previous != '0' || BufferLength > prefixLength)
                            throw new JsonLexerException("Invalid octal number", _position, Stream);
                        else
                            valueBase = 8;
                    }
                    else if (lowerCurrent == 'b' && previous == '0' && BufferLength == prefixLength)
                    {
                        if (valueBase == 2)
                            throw new JsonLexerException("Invalid binary number", _position, Stream);
                        else
                            valueBase = 2;
                    }
                    else
                    {
                        var index = Array.IndexOf(_hexadecimalChars, lowerCurrent);
                        if (index >= valueBase)
                            throw new JsonLexerException($"Invalid character '{_current}' for {valueBase}-based number", _position, Stream);
                    }
                }

                DisableBuffer();

                if (isEnumber || isDouble)
                {
                    var buffer = _buffer.Replace('.', ',').ToString();

                    if (!double.TryParse(buffer, out double value))
                        throw new JsonLexerException($"Invalid double number {_buffer}", _startPosition, Stream);

                    return value;
                }
                else if (valueBase != 10)
                {
                    var buffer = _buffer.ToString(prefixLength, _buffer.Length - prefixLength);

                    try
                    {
                        return Convert.ToInt32(buffer, valueBase) * sign;
                    }
                    catch
                    {
                        try
                        {
                            return Convert.ToInt64(buffer, valueBase) * sign;
                        }
                        catch (Exception ex)
                        {
                            throw new JsonLexerException($"Invalid number {_buffer}", _startPosition, Stream, ex);
                        }
                    }
                }
                else
                {
                    var buffer = _buffer.ToString();

                    if (int.TryParse(buffer, out int intvalue))
                        return intvalue;
                    else if (long.TryParse(buffer, out long longvalue))
                        return longvalue;
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
            }

            private string ReadString()
            {
                var stringBeginChar = _current.Value;
                var escape = false;

                _buffer.Clear();

                while (_next.HasValue && (escape || _next.Value != stringBeginChar))
                {
                    ReadNext();

                    if (escape)
                    {
                        escape = false;
                        switch (_current.Value)
                        {
                            case 'b':
                                _buffer.Append('\b');
                                break;
                            case 'a':
                                _buffer.Append('\a');
                                break;
                            case 'f':
                                _buffer.Append('\f');
                                break;
                            case 'n':
                                _buffer.Append('\n');
                                break;
                            case 'r':
                                _buffer.Append('\r');
                                break;
                            case 't':
                                _buffer.Append('\t');
                                break;
                            case '0':
                                _buffer.Append('\0');
                                break;
                            case 'u':
                                string unicode_value = "";
                                for (var i = 0; i < 4; i++)
                                {
                                    if (!_next.HasValue || !_hexadecimalChars.Contains(char.ToLowerInvariant(_next.Value)))
                                        throw new JsonLexerException($"The Unicode value must be hexadecimal and 4 characters long", _position - i - 1, Stream);
                                    ReadNext();
                                    unicode_value += _current.Value;
                                }
                                _buffer.Append((char)Convert.ToInt32(unicode_value, 16));
                                break;
                            case '"':
                                _buffer.Append('\"');
                                break;
                            case '\'':
                                _buffer.Append('\'');
                                break;
                            case '\\':
                                _buffer.Append('\\');
                                break;
                            case '/':
                                _buffer.Append('/');
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
                        _buffer.Append(_current.Value);
                    }
                }

                ReadNext();

                return _buffer.ToString();
            }

            private (TokenType type, object value) ReadOther()
            {
                EnableBuffer();

                while (_next.HasValue && char.IsLetterOrDigit(_next.Value))
                    ReadNext();

                DisableBuffer();

                if (_buffer.Equals("null"))
                    return (TokenType.Null, null);
                else if (bool.TryParse(_buffer.ToString(), out bool value))
                    return (TokenType.Boolean, value);

                throw new JsonLexerException($"{_buffer} value is not valid", _position, Stream);
            }

            public void Reset()
            {
                if (_state == -1)
                    throw new ObjectDisposedException(nameof(Enumerator));

                _position = -1;
                _startPosition = 0;
                _next = null;
                _lexer.ResetStream();
            }

            public void Dispose()
            {
                _state = -1;
            }
        }

        public JsonLexer(Stream stream) : this(new StreamReader(stream, null, true, 1))
        {
        }

        public JsonLexer(Stream stream, Encoding encoding) : this(new StreamReader(stream, encoding))
        {
        }

        public JsonLexer(string source) : this(
            new StreamReader(new MemoryStream(Encoding.Default.GetBytes(source)), null, true, 0)
        )
        {
        }

        public JsonLexer(StreamReader stream)
        {
            Stream = stream;
            _detectEncoding = (bool)_streamReaderDetectEncodingField.GetValue(stream);
        }

        ~JsonLexer()
        {
            Dispose();
        }

        public StreamReader Stream { get; }

        public IEnumerator<Token> GetEnumerator()
        {
            lock (_enumeratorLock)
            {
                if (_enumerator != null)
                {
                    if (!_enumerator.Disposed)
                        throw new InvalidOperationException($"There can only be one enumerator for a type {nameof(JsonLexer)}");

                    ResetStream();
                }

                return _enumerator = new Enumerator(this);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void ResetStream()
        {
            Stream.BaseStream.Position = 0;
            Stream.DiscardBufferedData();
            // After the first Read call on the StreamReader, detectEncoding is set to false,
            // and when moving to the beginning, the next Read call may return BOM bytes.
            _streamReaderDetectEncodingField.SetValue(Stream, _detectEncoding);
        }

        public void Dispose()
        {
            Stream.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
