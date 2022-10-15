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
        private static readonly FieldInfo _streamReaderDetectEncodingField = typeof(StreamReader).GetField("_detectEncoding", BindingFlags.NonPublic | BindingFlags.Instance);
        private JsonLexerEnumerator _enumerator;
        private readonly object _enumeratorLock = new object();
        private readonly bool _detectEncoding;

        private sealed class JsonLexerEnumerator : IEnumerator<Token>
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
            private readonly StringBuilder _stringBuffer = new StringBuilder();

            public JsonLexerEnumerator(JsonLexer lexer)
            {
                _lexer = lexer;
            }

            public Token Current { get; private set; }
            public bool Disposed { get; private set; }

            private StreamReader Stream => _lexer.Stream;
            private int BufferLength => _buffer.Length;
            object IEnumerator.Current => Current;

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

            public bool MoveNext()
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(JsonLexerEnumerator));

                while (Next.HasValue)
                {
                    Current = Get();

                    if (Current.Type == TokenType.Invalid)
                        throw new InvalidTokenException(_startPosition, Stream);
                    if (Current.Type != TokenType.Invisible)
                        return true;
                }

                return false;
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
                            throw new Exception();
                        }
                        break;
                }

                return new Token(tokenType, tokenPosition, _position - _startPosition, tokenValue);
            }

            private void ReadWhiteSpace()
            {
                while (Next.HasValue && (Next.Value == '\0' || char.IsWhiteSpace(Next.Value)))
                    ReadNext();
            }

            private object ReadNumber()
            {
                EnableBuffer();

                var isDouble = false;
                var isEnumber = false;
                var valueBase = 10;
                var signed = _current.Value == '-' || _current.Value == '+';
                var prefixLength = signed ? 3 : 2;

                while (
                    Next.HasValue &&
                    (
                        _numberChars.Contains(char.ToLowerInvariant(Next.Value)) || 
                        (
                            char.ToLowerInvariant(_current.Value) == 'e' &&
                            isEnumber &&
                            (
                                Next.Value == '-' || 
                                Next.Value == '+'
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

                var buffer = DisableBuffer();

                if (isEnumber || isDouble)
                {
                    if (!double.TryParse(buffer.Replace('.', ','), out double value))
                        throw new JsonLexerException($"Invalid double number {_buffer}", _startPosition, Stream);
                    return value;
                }
                else if (valueBase != 10)
                {
                    var number = buffer.Substring(prefixLength);
                    try
                    {
                        var i = Convert.ToInt32(number, valueBase);
                        if (signed && buffer[0] == '-')
                            i = -i;
                        return i;
                    }
                    catch
                    {
                        try
                        {
                            var i = Convert.ToInt64(number, valueBase);
                            if (signed && buffer[0] == '-')
                                i = -i;
                            return i;
                        }
                        catch (Exception ex)
                        {
                            throw new JsonLexerException($"Invalid number {buffer.Substring(0, 2)}{_buffer}", _startPosition, Stream, ex);
                        }
                    }
                }
                else
                {
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

                _stringBuffer.Clear();

                while (Next.HasValue && (escape || Next.Value != stringBeginChar))
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
                                    if (!Next.HasValue || !_hexadecimalChars.Contains(char.ToLowerInvariant(Next.Value)))
                                        throw new JsonLexerException($"The Unicode value must be hexadecimal and 4 characters long", _position - i - 1, Stream);
                                    ReadNext();
                                    unicode_value += _current.Value;
                                }
                                _stringBuffer.Append((char)Convert.ToInt32(unicode_value, 16));
                                break;
                            case '"':
                                _stringBuffer.Append('\"');
                                break;
                            case '\'':
                                _stringBuffer.Append('\'');
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

                return _stringBuffer.ToString();
            }

            private (TokenType type, object value) ReadOther()
            {
                EnableBuffer();

                while (Next.HasValue && char.IsLetterOrDigit(Next.Value))
                    ReadNext();

                var buffer = DisableBuffer();

                if (buffer == "null")
                    return (TokenType.Null, null);
                else if (bool.TryParse(buffer, out bool value))
                    return (TokenType.Boolean, value);

                throw new Exception();
            }

            public void Reset()
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(JsonLexerEnumerator));

                _position = -1;
                _startPosition = 0;
                _next = null;
                _lexer.ResetStream();
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        public JsonLexer(Stream stream) : this(new StreamReader(stream))
        {
        }

        public JsonLexer(Stream stream, Encoding encoding) : this(new StreamReader(stream, encoding))
        {
        }

        public JsonLexer(string source) : this(new StreamReader(new MemoryStream(Encoding.Default.GetBytes(source))))
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
                        throw new Exception();

                    ResetStream();
                }

                return _enumerator = new JsonLexerEnumerator(this);
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
            _streamReaderDetectEncodingField.SetValue(Stream, _detectEncoding);
        }

        public void Dispose()
        {
            Stream.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
