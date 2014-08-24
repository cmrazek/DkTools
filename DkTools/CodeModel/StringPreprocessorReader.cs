using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal class StringPreprocessorReader : IPreprocessorReader
	{
		private string _str;
		private int _pos;
		private int _len;
		private StringBuilder _sb = new StringBuilder();

		public StringPreprocessorReader(string str)
		{
			_str = str;
			_pos = 0;
			_len = _str.Length;
		}

		public bool EOF
		{
			get { return _pos >= _len; }
		}

		public char ReadChar()
		{
			if (_pos >= _len) return '\0';
			return _str[_pos++];
		}

		public char ReadChar(out CodeAttributes att)
		{
			att = CodeAttributes.Empty;
			if (_pos >= _len) return '\0';
			return _str[_pos++];
		}

		public char PeekChar()
		{
			if (_pos >= _len) return '\0';
			return _str[_pos];
		}

		public char PeekChar(out CodeAttributes att)
		{
			att = CodeAttributes.Empty;
			if (_pos >= _len) return '\0';
			return _str[_pos];
		}

		public string Peek(int numChars)
		{
			if (_pos + numChars > _len) numChars = _len - _pos;
			return _str.Substring(_pos, numChars);
		}

		public bool MoveNext()
		{
			if (_pos < _len) _pos++;
			return _pos < _len;
		}

		public bool MoveNext(int length)
		{
			_pos += length;
			if (_pos > _len) _pos = _len;
			return _pos < _len;
		}

		public string ReadSegmentUntil(Func<char, bool> callback)
		{
			char ch;

			_sb.Clear();

			while (_pos < _len)
			{
				ch = _str[_pos];
				if (callback(ch))
				{
					_sb.Append(ch);
					_pos++;
				}
				else break;
			}

			return _sb.ToString();
		}

		public string ReadSegmentUntil(Func<char, bool> callback, out CodeAttributes att)
		{
			att = CodeAttributes.Empty;
			return ReadSegmentUntil(callback);
		}

		public string ReadAllUntil(Func<char, bool> callback)
		{
			return ReadSegmentUntil(callback);
		}

		public string ReadIdentifier()
		{
			_sb.Clear();

			var first = true;
			char ch;

			while (_pos < _len)
			{
				ch = _str[_pos];
				if (!ch.IsWordChar(first)) break;
				_sb.Append(ch);
				_pos++;
				first = false;
			}

			return _sb.ToString();
		}
	}
}
