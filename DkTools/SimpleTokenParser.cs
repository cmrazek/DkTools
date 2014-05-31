using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools
{
    internal class SimpleTokenParser
    {
        private string _source;
        private int _length;
        private int _pos;
        private StringBuilder _sb = new StringBuilder();
        private int _tokenStart = 0;
        private int _tokenLength = 0;

        public SimpleTokenParser(string source)
        {
            _source = source;
            _length = _source.Length;
            _pos = 0;
        }

        public void SkipWhiteSpaceAndComments()
        {
            char ch;
            while (_pos < _length)
            {
                ch = _source[_pos];

                if (char.IsWhiteSpace(ch))
                {
                    _pos++;
                    continue;
                }

                if (ch == '/' && _pos + 1 < _length)
                {
                    if (_source[_pos + 1] == '/')
                    {
                        var index = _source.IndexOf('\n', _pos);
                        _pos = index >= 0 ? index : _length;
                        continue;
                    }

                    if (_source[_pos + 1] == '*')
                    {
                        var index = _source.IndexOf("*/", _pos);
                        _pos = index >= 0 ? index : _length;
                        continue;
                    }
                }

                break;
            }
        }

        public bool EndOfFile
        {
            get { return _pos >= _length; }
        }

        public string ParseToken()
        {
            SkipWhiteSpaceAndComments();

            _tokenStart = _pos;
            _tokenLength = 0;

            if (_pos >= _length) return null;

            var ch = _source[_pos];
            if (ch.IsWordChar(true))
            {
                _sb.Clear();
                _sb.Append(ch);
                _pos++;
                while (_pos < _length && _source[_pos].IsWordChar(false)) _sb.Append(_source[_pos++]);

                SkipWhiteSpaceAndComments();
                _tokenLength = _sb.Length;
                return _sb.ToString();
            }

            if (char.IsDigit(ch))
            {
                _sb.Clear();
                _sb.Append(ch);

                var gotDot = false;
                while (_pos < _length)
                {
                    ch = _source[_pos];
                    if (char.IsDigit(ch))
                    {
                        _sb.Append(ch);
                        _pos++;
                    }
                    else if (ch == '.' && gotDot == false)
                    {
                        _sb.Append(ch);
                        _pos++;
                        gotDot = true;
                    }
                    else break;
                }

                SkipWhiteSpaceAndComments();
                _tokenLength = _sb.Length;
                return _sb.ToString();
            }

            if (ch == '\"' || ch == '\'')
            {
                _sb.Clear();
                _sb.Append(ch);

                var startCh = ch;

                while (_pos < _length)
                {
                    ch = _source[_pos];
                    if (ch == '\\' && _pos + 1 < _length)
                    {
                        _sb.Append(ch);
                        _sb.Append(_source[_pos + 1]);
                        _pos += 2;
                    }
                    else if (ch == startCh)
                    {
                        _sb.Append(ch);
                        _pos++;
                        break;
                    }
                    else
                    {
                        _sb.Append(ch);
                        _pos++;
                    }
                }

                SkipWhiteSpaceAndComments();
                _tokenLength = _sb.Length;
                return _sb.ToString();
            }

            _pos++;
            SkipWhiteSpaceAndComments();
            _tokenLength = 1;
            return ch.ToString();
        }

        public IEnumerable<string> Tokens
        {
            get
            {
                var savePos = _pos;
                string token;
                _pos = 0;

                while (_pos < _length)
                {
                    if ((token = ParseToken()) != null) yield return token;
                }

                _pos = savePos;
                yield break;
            }
        }

        public int Position
        {
            get { return _pos; }
            set
            {
                if (value < 0 || value > _length) throw new ArgumentException("Position is out of bounds.");
                _pos = value;
            }
        }

        public int TokenStart
        {
            get { return _tokenStart; }
        }

        public int TokenLength
        {
            get { return _tokenLength; }
        }

        public string GetText(int startPos, int length)
        {
            return _source.Substring(startPos, length);
        }

        public int Length
        {
            get { return _length; }
        }
    }
}
