using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal class StringPreprocessorReader : IPreprocessorReader
	{
		private string _str;
		private int _pos;
		private int _len;
		private StringBuilder _sb = new StringBuilder();
		private IPreprocessorWriter _writer;
		private bool _suppress;

		public StringPreprocessorReader(string str)
		{
			_str = str;
			_pos = 0;
			_len = _str.Length;
		}

		public void SetWriter(IPreprocessorWriter writer)
		{
			_writer = writer;
		}

		public bool EOF
		{
			get { return _pos >= _len; }
		}

		public char Peek()
		{
			if (_pos >= _len) return '\0';
			return _str[_pos];
		}

		public string Peek(int numChars)
		{
			if (_pos + numChars > _len) numChars = _len - _pos;
			return _str.Substring(_pos, numChars);
		}

		public string PeekUntil(Func<char, bool> callback)
		{
			var pos = _pos;
			char ch;
			_sb.Clear();

			while (pos < _len)
			{
				if (callback(ch = _str[pos]))
				{
					_sb.Append(ch);
					pos++;
				}
				else break;
			}

			return _sb.ToString();
		}

		public void Use(int numChars)
		{
			if (_pos + numChars > _len) numChars = _len - _pos;
			if (!_suppress) _writer.Append(_str.Substring(_pos, numChars), CodeAttributes.Empty);
			_pos += numChars;
		}

		public void UseUntil(Func<char, bool> callback)
		{
			char ch;
			_sb.Clear();

			while (_pos < _len)
			{
				if (callback(ch = _str[_pos]))
				{
					_sb.Append(ch);
					_pos++;
				}
				else break;
			}

			if (_sb.Length > 0 && !_suppress)
			{
				_writer.Append(_sb.ToString(), CodeAttributes.Empty);
			}
		}

		public void Ignore(int numChars)
		{
			_pos += numChars;
			if (_pos > _len) _pos = _len;
		}

		public void IgnoreUntil(Func<char, bool> callback)
		{
			char ch;

			while (_pos < _len)
			{
				if (callback(ch = _str[_pos])) _pos++;
				else break;
			}
		}

		public void Insert(string text)
		{
			_writer.Append(text, CodeAttributes.Empty);
		}

		public bool Suppress
		{
			get { return _suppress; }
			set { _suppress = value; }
		}

		public string FileName
		{
			get { return string.Empty; }
		}

		public int Position
		{
			get { return _pos; }
		}

		public Match Match(Regex rx)
		{
			var match = rx.Match(_str, _pos);
			if (match.Success && match.Index == _pos) return match;
			return System.Text.RegularExpressions.Match.Empty;
		}
	}
}
