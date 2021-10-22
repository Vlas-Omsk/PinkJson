﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PinkJson2
{
    public sealed class JsonLexer : IEnumerable<Token>, IDisposable
    {
        public StreamReader Stream { get; }

        private int _position;
        private int _startPosition;
        private Token _token;
        private string _buffer;
        private bool _useBuffer = false;
        private char? _current;
        private readonly char[] numberChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', '.', 'x', 'o', 'b' };
        private readonly char[] hexadecimalChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        public JsonLexer(StreamReader stream)
        {
            Stream = stream;
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
                var next = Stream.Peek();
                return next == -1 ? null : (char?)next;
            }
        }

        public IEnumerator<Token> GetEnumerator()
        {
            Stream.BaseStream.Position = 0;
            Stream.DiscardBufferedData();
            _position = -1;
            _startPosition = 0;
            while (!Stream.EndOfStream)
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

        public void Dispose()
        {
            Stream.Dispose();
        }

        private void ReadNext()
        {
            _position++;
            var current = Stream.Read();
            _current = current == -1 ? null : (char?)current;
            if (_useBuffer && _current.HasValue)
                _buffer += _current.Value;
        }

        private void EnableBuffer()
        {
            _useBuffer = true;
            _buffer += _current.Value;
        }

        private void Get()
        {
            if (_useBuffer)
            {
                _buffer = string.Empty;
                _useBuffer = false;
            }
            _token = new Token();
            ReadNext();
            _token.Position = _startPosition = _position;

            switch (_current)
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
                    if (_current == '\0' || char.IsWhiteSpace(_current.Value))
                        ReadWhiteSpace();
                    else if (char.IsLetterOrDigit(_current.Value))
                        ReadOther();
                    break;
            }

            _token.Length = _position - _startPosition;
        }

        private void ReadWhiteSpace()
        {
            while (Next.HasValue && (Next == '\0' || char.IsWhiteSpace(Next.Value)))
                ReadNext();
            _token.Type = TokenType.Invisible;
        }

        private void ReadNumber()
        {
            EnableBuffer();

            bool isDouble = false, isEnumber = false;
            var valueBase = 10;
            while (Next.HasValue && (numberChars.Contains(char.ToLowerInvariant(Next.Value)) ||
                (char.ToLowerInvariant(_current.Value) == 'e' && ((isEnumber && Next == '-') || (isEnumber && Next == '+')))))
            {
                char? previous = _current;
                ReadNext();

                var lowerCurrent = char.ToLowerInvariant(_current.Value);
                if (lowerCurrent == '.' && valueBase == 10)
                {
                    if (isDouble)
                        throw new JsonException("Invalid double number", _position, Stream);
                    else
                        isDouble = true;
                }
                if (lowerCurrent == 'e' && valueBase == 10)
                {
                    if (isEnumber)
                        throw new JsonException("Invalid e number", _position, Stream);
                    else
                        isEnumber = true;
                }
                else if (lowerCurrent == 'x')
                {
                    if (valueBase == 16 || previous != '0' || _buffer.Length > 2)
                        throw new JsonException("Invalid hexadecimal number", _position, Stream);
                    else
                        valueBase = 16;
                }
                else if (lowerCurrent == 'o')
                {
                    if (valueBase == 8 || previous != '0' || _buffer.Length > 2)
                        throw new JsonException("Invalid octal number", _position, Stream);
                    else
                        valueBase = 8;
                }
                else if (lowerCurrent == 'b' && previous == '0' && _buffer.Length == 2)
                {
                    if (valueBase == 2 || previous != '0' || _buffer.Length > 2)
                        throw new JsonException("Invalid binary number", _position, Stream);
                    else
                        valueBase = 2;
                }
                else
                {
                    var index = Array.IndexOf(hexadecimalChars, lowerCurrent);
                    if (index >= valueBase)
                        throw new JsonException($"Invalid character '{_current}' for {valueBase}-based number", _position, Stream);
                }
            }

            if (isEnumber || isDouble)
            {
                if (!double.TryParse(_buffer.Replace('.', ','), out double value))
                    throw new JsonException($"Invalid double number {_buffer}", _startPosition, Stream);
                _token.Value = value;
            }
            else if (valueBase != 10)
            {
                var prefix = _buffer.Substring(0, 2);
                _buffer = _buffer.Substring(2);
                try
                {
                    _token.Value = Convert.ToInt32(_buffer, valueBase);
                }
                catch
                {
                    try
                    {
                        _token.Value = Convert.ToInt64(_buffer, valueBase);
                    }
                    catch (Exception ex)
                    {
                        throw new JsonException($"Invalid number {prefix}{_buffer}", _startPosition, Stream, ex);
                    }
                }
            }
            else
            {
                if (int.TryParse(_buffer, out int intvalue))
                    _token.Value = intvalue;
                else if (long.TryParse(_buffer, out long longvalue))
                    _token.Value = longvalue;
                //else if (BigInteger.TryParse(str, out BigInteger bigintvalue))
                //    _token.Value = bigintvalue;
                else
                {
                    if (_buffer.Length < 40)
                        throw new JsonException($"Invalid or too big number {_buffer}", _startPosition, Stream);
                    else
                        throw new JsonException($"Well.. It's seriously big number {_buffer}. I.. I even can't imagine how to handle it. No, seriously. Maybe you know how?", _startPosition, Stream);
                }
            }

            _token.Type = TokenType.Number;
        }

        private void ReadString()
        {
            var escape = false;
            var value = string.Empty;

            while (Next.HasValue && (escape || Next != '"'))
            {
                ReadNext();

                if (escape)
                {
                    escape = false;
                    switch (_current)
                    {
                        case 'b':
                            value += '\b';
                            break;
                        case 'a':
                            value += '\a';
                            break;
                        case 'f':
                            value += '\f';
                            break;
                        case 'n':
                            value += '\n';
                            break;
                        case 'r':
                            value += '\r';
                            break;
                        case 't':
                            value += '\t';
                            break;
                        case '0':
                            value += '\0';
                            break;
                        case 'u':
                            string unicode_value = "";
                            for (var i = 0; i < 4; i++)
                            {
                                if (!Next.HasValue || !hexadecimalChars.Contains(char.ToLowerInvariant(Next.Value)))
                                    throw new JsonException($"The Unicode value must be hexadecimal and 4 characters long", _position - i - 1, Stream);
                                ReadNext();
                                unicode_value += _current;
                            }
                            value += (char)Convert.ToInt32(unicode_value, 16);
                            break;
                        case '"':
                            value += '\"';
                            break;
                        case '\\':
                            value += '\\';
                            break;
                        case '/':
                            value += '/';
                            break;
                        default:
                            throw new JsonException($"Unidentified escape sequence \\{_current}", _position - 1, Stream);
                    }
                }
                else if (_current == '\\')
                {
                    escape = true;
                }
                else
                {
                    value += _current;
                }
            }

            ReadNext();

            _token.Type = TokenType.String;
            _token.Value = value;
        }

        private void ReadOther()
        {
            EnableBuffer();

            while (Next.HasValue && char.IsLetterOrDigit(Next.Value))
                ReadNext();

            if (_buffer == "null")
            {
                _token.Type = TokenType.Null;
            }
            else if (bool.TryParse(_buffer, out bool value))
            {
                _token.Value = value;
                _token.Type = TokenType.Boolean;
            }
        }
    }
}
