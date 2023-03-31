using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DK.Code
{
	public class CodeParser
	{
		private int _pos;
		private string _source;
		private int _length;
		private StringBuilder _tokenText = new StringBuilder();
		private string _tokenTextStr;
		private CodeType _tokenType = CodeType.Unknown;
		private bool _returnWhiteSpace = false;
		private bool _stopAtLineEnd = false;
		private bool _returnComments = false;
		private int _tokenStartPos;
		private bool _tokenTerminated = true;
		private bool _enumNestable = false;
		private int _documentOffset;

		private static readonly char[] _lineEndChars = new char[] { '\r', '\n' };

		public CodeParser(string source)
		{
			SetSource(source);
		}

		public CodeParser(string source, bool enumerateNestable)
		{
			SetSource(source);
			_enumNestable = enumerateNestable;
		}

		public void SetSource(string source)
		{
			_source = source;
			_length = _source.Length;
			_pos = 0;
		}

		public bool Read()
		{
			while (_pos < _length)
			{
				_tokenStartPos = Position;
				_tokenText.Clear();
				_tokenTextStr = null;
				_tokenTerminated = true;

				var ch = _source[_pos];

				if (char.IsWhiteSpace(ch))
				{
					if (_stopAtLineEnd && (ch == '\r' || ch == '\n'))
					{
						_tokenType = CodeType.WhiteSpace;
						return false;
					}

					_tokenText.Append(ch);
					_pos++;

					while (_pos < _length)
					{
						ch = _source[_pos];
						if (_stopAtLineEnd && (ch == '\r' || ch == '\n')) break;
						else if (char.IsWhiteSpace(ch))
						{
							_tokenText.Append(ch);
							_pos++;
						}
						else break;
					}

					if (_returnWhiteSpace)
					{
						_tokenType = CodeType.WhiteSpace;
						return true;
					}
					else if (_stopAtLineEnd && _tokenText.Length == 0)
					{
						_tokenType = CodeType.WhiteSpace;
						return false;
					}
					else continue;
				}

				if (char.IsLetter(ch) || ch == '_')
				{
					while (_pos < _length && (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '_'))
					{
						_tokenText.Append(_source[_pos++]);
					}

					switch (_tokenText.ToString())
					{
						case "and":
						case "or":
						case "in":
							_tokenType = CodeType.Operator;
							break;
						default:
							_tokenType = CodeType.Word;
							break;
					}

					return true;
				}

				if (char.IsDigit(ch) || (ch == '.' && _pos + 1 < _length && char.IsDigit(_source[_pos + 1])))
				{
					var gotDot = false;
					while (_pos < _length)
					{
						ch = _source[_pos];
						if (char.IsDigit(ch))
						{
							_tokenText.Append(ch);
							_pos++;
						}
						else if (ch == '.' && !gotDot)
						{
							_tokenText.Append('.');
							_pos++;
							gotDot = true;
						}
						else break;
					}

					_tokenType = CodeType.Number;
					return true;
				}

				if (ch == '\"' || ch == '\'')
				{
					var startCh = ch;
					_tokenText.Append(ch);
					_tokenTerminated = false;
					_pos++;

					while (_pos < _length)
					{
						ch = _source[_pos];
						if (ch == '\\' && _pos + 1 < _length)
						{
							ch = _source[_pos + 1];
							// If the '\' is the last char on the line, then the string decends down to the next line.
							if (ch == '\r')
							{
								_pos += 2;
								if (_pos < _length && _source[_pos] == '\n') _pos++;
							}
							else if (ch == '\n')
							{
								_pos += 2;
							}
							else
							{
								_tokenText.Append(_source[_pos]);
								_tokenText.Append(_source[_pos + 1]);
								_pos += 2;
							}
						}
						else if (ch == startCh)
						{
							if (_pos + 1 < _length && _source[_pos + 1] == startCh)
							{
								// 2 strings side-by-side make a single concatenated string.
								_tokenText.Append(startCh);
								_tokenText.Append(startCh);
								_pos += 2;
							}
							else
							{
								_tokenText.Append(ch);
								_tokenTerminated = true;
								_pos++;
								break;
							}
						}
						else if (ch == '\r' || ch == '\n')
						{
							// String literal breaks before end of line.
							break;
						}
						else
						{
							_tokenText.Append(ch);
							_pos++;
						}
					}

					_tokenType = CodeType.StringLiteral;
					return true;
				}

				if (ch == '/')
				{
					if (_pos + 1 < _length && _source[_pos + 1] == '/')
					{
						var index = _source.IndexOfAny(_lineEndChars, _pos);
						if (index < 0)
						{
							index = _length;
							_tokenTerminated = false;
						}
						else
						{
							//index++;
							_tokenTerminated = true;
						}

						if (_returnComments)
						{
							while (_pos < index)
							{
								_tokenText.Append(_source[_pos++]);
							}

							_tokenType = CodeType.Comment;
							return true;
						}
						else
						{
							_pos = index;
							continue;
						}
					}

					if (_pos + 1 < _length && _source[_pos + 1] == '*')
					{
						if (_returnComments) _tokenText.Append("/*");
						_pos += 2;
						var level = 1;
						while (_pos < _length)
						{
							ch = _source[_pos];
							if (ch == '/' && _pos + 1 < _length && _source[_pos + 1] == '*')
							{
								if (_returnComments) _tokenText.Append("/*");
								_pos += 2;
								level++;
							}
							else if (ch == '*' && _pos + 1 < _length && _source[_pos + 1] == '/')
							{
								if (_returnComments) _tokenText.Append("*/");
								_pos += 2;
								if (--level == 0) break;
							}
							else
							{
								if (_returnComments) _tokenText.Append(ch);
								_pos++;
							}
						}

						if (_returnComments)
						{
							_tokenTerminated = level == 0;
							_tokenType = CodeType.Comment;
							return true;
						}
						else continue;
					}

					if (_pos + 1 < _length && _source[_pos + 1] == '=')
					{
						_tokenText.Append("/=");
						_pos += 2;
						_tokenType = CodeType.Operator;
						return true;
					}

					_tokenText.Append("/");
					_pos++;
					_tokenType = CodeType.Operator;
					return true;
				}

				if (ch == '+' || ch == '-' || ch == '*' || ch == '%' || ch == '=' || ch == '!' || ch == '<' || ch == '>')
				{
					_tokenText.Append(ch);
					_pos++;
					_tokenType = CodeType.Operator;

					if (_pos < _length && _source[_pos] == '=')
					{
						_tokenText.Append("=");
						_pos++;
					}

					return true;
				}

				if (ch == '?' || ch == ':' || ch == ',' || ch == '.' || ch == '(' || ch == ')' || ch == '{' || ch == '}' || ch == '[' || ch == ']' || ch == '^' || ch == '!' || ch == ';')
				{
					_tokenText.Append(ch);
					_pos++;
					_tokenType = CodeType.Operator;
					return true;
				}

				if (ch == '&')
				{
					_tokenText.Append('&');
					_pos++;
					_tokenType = CodeType.Operator;

					if (_pos < _length)
					{
						if (_source[_pos] == '+')
						{
							_tokenText.Append('+');
							_pos++;
						}
						else if (_source[_pos] == '&')
						{
							_tokenText.Append('&');
							_pos++;
						}
					}

					return true;
				}

				if (ch == '|')
				{
					_tokenText.Append('|');
					_pos++;
					_tokenType = CodeType.Operator;

					if (_pos < _length && _source[_pos] == '|')
					{
						_tokenText.Append('|');
						_pos++;
					}

					return true;
				}

				if (ch == '#')
				{
					_tokenText.Append(ch);
					_pos++;
					if (_pos < _length && char.IsLetter(_source[_pos]))
					{
						while (_pos < _length && char.IsLetter(_source[_pos]))
						{
							_tokenText.Append(_source[_pos++]);
						}

						_tokenType = CodeType.Preprocessor;
						return true;
					}
					else
					{
						_tokenType = CodeType.Operator;
						return true;
					}
				}

				_tokenText.Append(ch);
				_pos++;
				_tokenType = CodeType.Unknown;
				return true;
			}

			// End of file
			_tokenType = CodeType.WhiteSpace;
			_tokenText.Clear();
			return false;
		}

		public int Position
		{
			get { return _pos; }
			set
			{
				if (value < 0 || value > _length) throw new ArgumentOutOfRangeException();
				_pos = value;
			}
		}

		public int TokenStartPostion
		{
			get { return _tokenStartPos; }
		}

		public CodeSpan Span
		{
			get { return new CodeSpan(_tokenStartPos, _tokenStartPos + _tokenText.Length); }
		}

		public int Length
		{
			get { return _length; }
		}

		public bool ReturnWhiteSpace
		{
			get { return _returnWhiteSpace; }
			set { _returnWhiteSpace = value; }
		}

		public bool StopAtLineEnd
		{
			get { return _stopAtLineEnd; }
			set { _stopAtLineEnd = value; }
		}

		public bool ReturnComments
		{
			get { return _returnComments; }
			set { _returnComments = value; }
		}

		public void CalcLineAndPosFromOffset(int offset, out int lineNumOut, out int linePosOut)
		{
			if (offset < 0 || offset > _length) throw new ArgumentOutOfRangeException("offset");

			int pos = 0;
			int lineNum = 0;
			int linePos = 0;

			while (pos < _length)
			{
				if (_source[pos] == '\n')
				{
					lineNum++;
					linePos = 0;
				}
				else
				{
					linePos++;
				}
				pos++;
			}

			lineNumOut = lineNum;
			linePosOut = linePos;
		}

		public CodeType Type
		{
			get { return _tokenType; }
		}

		public string Text
		{
			get
			{
				if (_tokenTextStr == null) _tokenTextStr = _tokenText.ToString();
				return _tokenTextStr;
			}
		}

		public bool TokenTerminated
		{
			get { return _tokenTerminated; }
		}

		public bool EndOfFile
		{
			get { return _pos >= _length; }
		}

		public string RemainingText => _pos < _length ? _source.Substring(_pos) : string.Empty;

		public bool ReadNestable()
		{
			if (!Read()) return false;

			var startPos = _tokenStartPos;
			var firstTokenType = _tokenType;

			if (_tokenType == CodeType.Operator)
			{
				switch (_tokenText.ToString())
				{
					case "(":
						if (ReadNestable_Inner(")"))
						{
							_tokenStartPos = startPos;
							_tokenText.Clear();
							_tokenText.Append(_source.Substring(_tokenStartPos, _pos - _tokenStartPos));
							_tokenType = CodeType.Nested;
						}
						else
                        {
							_tokenStartPos = startPos;
							_tokenText.Clear();
							_tokenText.Append('(');
							_tokenType = CodeType.Operator;
							_pos = _tokenStartPos + 1;
                        }
						return true;
					case "{":
						if (ReadNestable_Inner("}"))
						{
							_tokenStartPos = startPos;
							_tokenText.Clear();
							_tokenText.Append(_source.Substring(_tokenStartPos, _pos - _tokenStartPos));
							_tokenType = CodeType.Nested;
						}
						else
                        {
							_tokenStartPos = startPos;
							_tokenText.Clear();
							_tokenText.Append('{');
							_tokenType = CodeType.Operator;
							_pos = _tokenStartPos + 1;
                        }
						return true;
					case "[":
						if (ReadNestable_Inner("]"))
						{
							_tokenStartPos = startPos;
							_tokenText.Clear();
							_tokenText.Append(_source.Substring(_tokenStartPos, _pos - _tokenStartPos));
							_tokenType = CodeType.Nested;
						}
						else
                        {
							_tokenStartPos = startPos;
							_tokenText.Clear();
							_tokenText.Append('[');
							_tokenType = CodeType.Operator;
							_pos = _tokenStartPos + 1;
                        }
						return true;
				}
			}

			_tokenStartPos = startPos;
			_tokenType = firstTokenType;
			return true;
		}

		private bool ReadNestable_Inner(string endText)
		{
			var startPos = Position;

			while (Read())
			{
				if (_tokenType == CodeType.Operator)
				{
					if (_tokenText.ToString() == endText) return true;
					switch (_tokenText.ToString())
					{
						case "(":
							if (!ReadNestable_Inner(")")) return false;
							break;
						case "{":
							if (!ReadNestable_Inner("}")) return false;
							break;
						case "[":
							if (!ReadNestable_Inner("]")) return false;
							break;
					}
				}
			}

			Position = startPos;
			return false;
		}

		public string GetText(int startIndex, int length)
		{
			if (startIndex < 0 || startIndex + length > _length) throw new ArgumentOutOfRangeException();
			return _source.Substring(startIndex, length);
		}

		public string GetText(CodeSpan span)
		{
			return GetText(span.Start, span.Length);
		}

		public string DocumentText
		{
			get { return _source; }
		}

		public bool Peek()
		{
			var pos = _pos;
			if (!Read()) return false;
			_pos = pos;
			return true;
		}

		public static string StringLiteralToString(string str)
		{
			if (str.StartsWith("\"") || str.StartsWith("\'")) str = str.Substring(1);
			if (str.EndsWith("\"") || str.EndsWith("\'")) str = str.Substring(0, str.Length - 1);

			var sb = new StringBuilder(str.Length);
			char ch;
			for (int i = 0; i < str.Length; i++)
			{
				ch = str[i];

				if (ch == '\\' && i + 1 < str.Length)
				{
					i++;
					switch (str[i])
					{
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						default:
							sb.Append(str[i]);
							break;
					}
				}
				else if ((ch == '\"' || ch == '\'') && i + 1 < str.Length && str[i + 1] == ch)
				{
					// Concatenated strings
					i++;
				}
				else sb.Append(ch);
			}

			return sb.ToString();
		}

		public static string StringToStringLiteral(string str)
		{
			var sb = new StringBuilder();
			sb.Append('\"');

			foreach (var ch in str)
			{
				switch (ch)
				{
					case '\t':
						sb.Append("\\t");
						break;
					case '\r':
						sb.Append("\\r");
						break;
					case '\n':
						sb.Append("\\n");
						break;
					case '\"':
						sb.Append("\\\"");
						break;
					case '\'':
						sb.Append("\\'");
						break;
					default:
						if (ch >= ' ' && ch <= 0x7f)
						{
							sb.Append(ch);
						}
						else
						{
							sb.AppendFormat("\\x{0:X4}", (int)ch);
						}
						break;
				}
			}

			sb.Append('\"');
			return sb.ToString();
		}

		public string Source
		{
			get { return _source; }
			set { SetSource(value); }
		}

		public bool ReadWord()
		{
			if (!SkipWhiteSpace()) return false;

			if (_pos >= _length || !_source[_pos].IsWordChar(true)) return false;

			_tokenText.Clear();
			_tokenTextStr = null;
			_tokenType = CodeType.Word;
			_tokenStartPos = _pos;
			_tokenTerminated = true;

			char ch;
			while (_pos < _length)
			{
				ch = _source[_pos];
				if (!ch.IsWordChar(false)) return true;
				_tokenText.Append(ch);
				_pos++;
			}

			return true;
		}

		public string ReadWordR()
		{
			if (!ReadWord()) return string.Empty;
			return Text;
		}

		public string PeekWordR()
		{
			var pos = _pos;
			if (ReadWord())
			{
				_pos = pos;
				return _tokenText.ToString();
			}
			else
			{
				_pos = pos;
				return string.Empty;
			}
		}

		public bool ReadTagName()
		{
			if (!SkipWhiteSpace()) return false;

			if (_pos >= _length || !_source[_pos].IsWordChar(true)) return false;

			_tokenText.Clear();
			_tokenTextStr = null;
			_tokenType = CodeType.Word;
			_tokenStartPos = _pos;
			_tokenTerminated = true;

			char ch;
			var gotColon = false;
			while (_pos < _length)
			{
				ch = _source[_pos];
				if (!ch.IsWordChar(false))
				{
					if (ch == ':')
					{
						if (gotColon) return true;
						gotColon = true;
					}
					else
					{
						return true;
					}
				}
				_tokenText.Append(ch);
				_pos++;
			}

			return true;
		}

		public bool ReadExact(string expecting)
		{
			if (!SkipWhiteSpace()) return false;

			var expLength = expecting.Length;
			if (_pos + expLength > _length) return false;

			for (int i = 0; i < expLength; i++)
			{
				if (_source[_pos + i] != expecting[i]) return false;
			}

			_tokenStartPos = _pos;
			_pos += expLength;

			_tokenText.Clear();
			_tokenText.Append(expecting);
			_tokenTextStr = null;
			_tokenType = CodeType.Unknown;
			return true;
		}

		public bool ReadExactWholeWord(string expecting)
		{
			if (!SkipWhiteSpace()) return false;

			var expLength = expecting.Length;
			if (_pos + expLength > _length) return false;

			for (int i = 0; i < expLength; i++)
			{
				if (_source[_pos + i] != expecting[i]) return false;
			}

			if (_pos + expLength < _length && _source[_pos + expLength].IsWordChar(false))
			{
				return false;
			}

			_tokenStartPos = _pos;
			_pos += expLength;

			_tokenText.Clear();
			_tokenText.Append(expecting);
			_tokenTextStr = null;
			_tokenType = CodeType.Unknown;
			return true;
		}

		public bool ReadExactWholeWordI(string expecting)
		{
			if (!SkipWhiteSpace()) return false;

			var expLength = expecting.Length;
			if (_pos + expLength > _length) return false;

			for (int i = 0; i < expLength; i++)
			{
				if (char.ToLower(_source[_pos + i]) != char.ToLower(expecting[i])) return false;
			}

			if (_pos + expLength < _length && _source[_pos + expLength].IsWordChar(false))
			{
				return false;
			}

			_tokenStartPos = _pos;
			_pos += expLength;

			_tokenText.Clear();
			_tokenText.Append(expecting);
			_tokenTextStr = null;
			_tokenType = CodeType.Unknown;
			return true;
		}

		public bool ReadExactWholeWord(IEnumerable<string> expectingList)
		{
			foreach (var expecting in expectingList)
			{
				if (ReadExactWholeWord(expecting)) return true;
			}

			return false;
		}

		public bool ReadExact(char expecting)
		{
			if (!SkipWhiteSpace()) return false;

			if (_pos >= _length || _source[_pos] != expecting) return false;

			_tokenStartPos = _pos;
			_pos++;

			_tokenText.Clear();
			_tokenText.Append(expecting);
			_tokenTextStr = null;
			_tokenType = CodeType.Unknown;
			return true;
		}

		public bool ReadPattern(Regex rx)
		{
			if (!SkipWhiteSpace()) return false;
			if (_pos >= _length) return false;

			var match = rx.Match(_source, _pos);
			if (!match.Success) return false;
			if (match.Index != _pos) throw new InvalidOperationException("Regular expression read must begin with \\G in order to properly parse.");

			_tokenStartPos = _pos;
			_pos += match.Length;

			_tokenText.Clear();
			_tokenText.Append(match.Value);
			_tokenTextStr = null;
			_tokenType = CodeType.Unknown;
			return true;
		}

		public bool SkipWhiteSpace()
		{
			char ch;
			while (_pos < _length)
			{
				ch = _source[_pos];
				if (char.IsWhiteSpace(ch))
				{
					if (_stopAtLineEnd && (ch == '\r' || ch == '\n')) return true;
					else if (_returnWhiteSpace) return true;
					_pos++;
				}
				else if (ch == '/' && !_returnComments && _pos + 1 < _length)
				{
					ch = _source[_pos + 1];
					if (ch == '/')
					{
						// single line comment
						var index = _source.IndexOf('\n', _pos);
						_pos = index < 0 ? _length : index;
					}
					else if (ch == '*')
					{
						// multi line comment
						_pos += 2;
						var level = 1;
						while (_pos < _length)
						{
							ch = _source[_pos];
							if (ch == '/' && _pos + 1 < _length && _source[_pos + 1] == '*')
							{
								_pos += 2;
								level++;
							}
							else if (ch == '*' && _pos + 1 < _length && _source[_pos + 1] == '/')
							{
								_pos += 2;
								if (--level == 0) break;
							}
							else _pos++;
						}
					}
					else return true;	// '/' not part of comment
				}
				else return true;	// other char
			}

			return false;
		}

		public bool ReadNumber()
		{
			if (!SkipWhiteSpace()) return false;

			if (_pos >= _length) return false;

			var ch = _source[_pos];
			if (ch == '-')
			{
				if (_pos + 1 >= _length || (!char.IsDigit(_source[_pos + 1]) && ch != '.')) return false;
			}
			else if (ch == '.')
			{
				if (_pos + 1 >= _length || !char.IsDigit(_source[_pos + 1])) return false;
			}
			else if (char.IsDigit(ch))
			{
				_tokenText.Append(ch);
			}
			else return false;

			_tokenText.Clear();
			_tokenTextStr = null;
			_tokenStartPos = Position;
			_tokenType = CodeType.Number;

			var gotDot = false;
			while (_pos < _length)
			{
				ch = _source[_pos];
				if (char.IsDigit(ch))
				{
					_tokenText.Append(ch);
					_pos++;
				}
				else if (ch == '.' && !gotDot)
				{
					_tokenText.Append('.');
					_pos++;
					gotDot = true;
				}
				else break;
			}

			return true;
		}

		public bool ReadStringLiteral()
		{
			if (!SkipWhiteSpace()) return false;

			if (_pos >= _length) return false;

			var ch = _source[_pos];
			if (ch != '\"' && ch != '\'') return false;

			var startCh = ch;
			_tokenStartPos = Position;
			_tokenText.Clear();
			_tokenText.Append(ch);
			_tokenTextStr = null;
			_tokenTerminated = false;
			_pos++;

			while (_pos < _length)
			{
				ch = _source[_pos];
				if (ch == '\\' && _pos + 1 < _length)
				{
					ch = _source[_pos + 1];
					// If the '\' is the last char on the line, then the string decends down to the next line.
					if (ch == '\r')
					{
						_pos += 2;
						if (_pos < _length && _source[_pos] == '\n') _pos++;
					}
					else if (ch == '\n')
					{
						_pos += 2;
					}
					else
					{
						_tokenText.Append(_source[_pos]);
						_tokenText.Append(_source[_pos + 1]);
						_pos += 2;
					}
				}
				else if (ch == startCh)
				{
					if (_pos + 1 < _length && _source[_pos + 1] == startCh)
					{
						// 2 strings side-by-side make a single concatenated string.
						_tokenText.Append(startCh);
						_tokenText.Append(startCh);
						_pos += 2;
					}
					else
					{
						_tokenText.Append(ch);
						_tokenTerminated = true;
						_pos++;
						break;
					}
				}
				else if (ch == '\r' || ch == '\n')
				{
					// String literal breaks before end of line.
					break;
				}
				else
				{
					_tokenText.Append(ch);
					_pos++;
				}
			}

			_tokenType = CodeType.StringLiteral;
			return true;
		}

		public bool ReadIncludeStringLiteral()
		{
			if (!SkipWhiteSpace()) return false;

			var ch = _source[_pos];
			var endCh = ch;

			_tokenText.Clear();
			_tokenTextStr = null;
			_tokenStartPos = _pos;
			_tokenTerminated = false;
			
			switch (endCh)
			{
				case '\"':
					_tokenText.Append(ch);
					endCh = '\"';
					break;
				case '<':
					_tokenText.Append(ch);
					endCh = '>';
					break;
				default:
					return false;
			}
			_pos++;

			while (_pos < _length)
			{
				ch = _source[_pos];
				if (ch == '\r' || ch == '\n') break;
				_tokenText.Append(ch);
				_pos++;
				if (ch == endCh)
				{
					_tokenTerminated = true;
					break;
				}
			}

			return true;
		}

		public bool PeekExact(string expecting)
		{
			if (!SkipWhiteSpace()) return false;

			var expLength = expecting.Length;
			if (_pos + expLength > _length) return false;

			for (int i = 0; i < expLength; i++)
			{
				if (_source[_pos + i] != expecting[i]) return false;
			}

			_tokenStartPos = _pos;

			_tokenText.Clear();
			_tokenText.Append(expecting);
			_tokenTextStr = null;
			_tokenType = CodeType.Unknown;
			return true;
		}

		public bool PeekExactWholeWord(string expecting)
		{
			if (!SkipWhiteSpace()) return false;

			var expLength = expecting.Length;
			if (_pos + expLength > _length) return false;

			for (int i = 0; i < expLength; i++)
			{
				if (_source[_pos + i] != expecting[i]) return false;
			}

			if (_pos + expLength < _length && _source[_pos + expLength].IsWordChar(false))
			{
				return false;
			}

			_tokenStartPos = _pos;

			_tokenText.Clear();
			_tokenText.Append(expecting);
			_tokenTextStr = null;
			_tokenType = CodeType.Unknown;
			return true;
		}

		public bool PeekExactWholeWordI(string expecting)
		{
			if (!SkipWhiteSpace()) return false;

			var expLength = expecting.Length;
			if (_pos + expLength > _length) return false;

			for (int i = 0; i < expLength; i++)
			{
				if (char.ToLower(_source[_pos + i]) != char.ToLower(expecting[i])) return false;
			}

			if (_pos + expLength < _length && _source[_pos + expLength].IsWordChar(false))
			{
				return false;
			}

			_tokenStartPos = _pos;

			_tokenText.Clear();
			_tokenText.Append(expecting);
			_tokenTextStr = null;
			_tokenType = CodeType.Unknown;
			return true;
		}

		public bool PeekExactWholeWord(IEnumerable<string> expectingList)
		{
			var pos = _pos;
			var ret = ReadExactWholeWord(expectingList);
			_pos = pos;
			return ret;
		}

		public bool PeekExact(char expecting)
		{
			if (!SkipWhiteSpace()) return false;

			if (_pos >= _length || _source[_pos] != expecting) return false;

			_tokenStartPos = _pos;

			_tokenText.Clear();
			_tokenText.Append(expecting);
			_tokenTextStr = null;
			_tokenType = CodeType.Unknown;
			return true;
		}

		public char PeekChar()
		{
			if (!SkipWhiteSpace()) return '\0';
			return _source[_pos];
		}

		public int DocumentOffset
		{
			get { return _documentOffset; }
			set { _documentOffset = value; }
		}

		public static string NormalizeText(string text)
		{
			var code = new CodeParser(text);
			var needSpace = false;
			CodeType lastTokenType = CodeType.Unknown;
			string lastTokenText = null;
			CodeType tokenType;
			string tokenText;
			var sb = new StringBuilder(text.Length);

			while (code.Read())
			{
				tokenType = code.Type;
				tokenText = code.Text;

				if (sb.Length == 0)
				{
					needSpace = false;
				}
				if (lastTokenText == "," || lastTokenText == ";")
				{
					needSpace = true;
				}
				else if (lastTokenText == "." || lastTokenText == "(" || lastTokenText == "[")
				{
					needSpace = false;
				}
				else if (tokenType == CodeType.Operator)
				{
					switch (tokenText)
					{
						case ",":
						case ".":
						case ";":
							needSpace = false;
							break;
						case "{":
						case "}":
							needSpace = false;
							break;
						case "(":
							if (lastTokenText == "(" || lastTokenText == "[" || lastTokenType == CodeType.Word) needSpace = false;
							else needSpace = true;
							break;
						case ")":
							if (lastTokenText == "(" || lastTokenText == ")" || lastTokenText == "]" ||
								lastTokenType == CodeType.Word || lastTokenType == CodeType.Number || lastTokenType == CodeType.StringLiteral)
							{
								needSpace = false;
							}
							else needSpace = true;
							break;
						case "[":
							if (lastTokenText == "(" || lastTokenText == "[" || lastTokenType == CodeType.Word) needSpace = false;
							else needSpace = true;
							break;
						case "]":
							if (lastTokenText == ")" || lastTokenText == "]" || lastTokenType == CodeType.Word || lastTokenType == CodeType.Number ||
								lastTokenType == CodeType.StringLiteral)
							{
								needSpace = false;
							}
							else needSpace = true;
							break;
						default:
							needSpace = true;
							break;
					}
				}
				else if (tokenType == CodeType.Word)
				{
					if (lastTokenText == "&") needSpace = false;
					else needSpace = true;
				}
				else if (tokenType == CodeType.Number || tokenType == CodeType.StringLiteral)
				{
					if (lastTokenText == "(" || lastTokenText == "[") needSpace = false;
					else needSpace = true;
				}
				else if (tokenType == CodeType.Preprocessor)
				{
					needSpace = true;
				}
				else
				{
					needSpace = true;
				}

				if (needSpace) sb.Append(' ');
				sb.Append(tokenText);

				lastTokenType = tokenType;
				lastTokenText = tokenText;
			}

			return sb.ToString();
		}

		public bool MoveNext()
		{
			if (_pos < _length) _pos++;
			return _pos < _length;
		}

		public bool MoveNext(int length)
		{
			_pos += length;
			if (_pos > _length) _pos = _length;
			return _pos < _length;
		}

		public bool MovePeeked()
		{
			_pos = _tokenStartPos + _tokenText.Length;
			return _pos < _length;
		}

		public CodeSpan MovePeekedSpan()
		{
			var span = Span;
			_pos = span.End;
			return span;
		}

		public int FindStartOfLine(int pos)
		{
			if (pos > _length) pos = _length;
			while (pos > 0 && _source[pos - 1] != '\n') pos--;
			return pos;
		}

		public int FindEndOfPreviousLine(int pos)
		{
			var offset = FindStartOfLine(pos);
			if (offset <= 0) return 0;

			offset--;
			if (offset > 0 && _source[offset] == '\n' && _source[offset - 1] == '\r') offset--;
			return offset;
		}

		public int FindEndOfLine(int pos)
		{
			while (pos < _length && !_source[pos].IsEndOfLineChar()) pos++;
			return pos;
		}

		public int FindStartOfNextLine(int pos)
		{
			pos = FindEndOfLine(pos);
			if (pos < _length && _source[pos] == '\r') pos++;
			if (pos < _length && _source[pos] == '\n') pos++;
			return pos;
		}

		public int FindPreviousNonWhiteSpace(int pos)
		{
			if (pos < 0 || pos > _length) throw new ArgumentOutOfRangeException();

			while (pos > 0)
			{
				pos--;
				if (!char.IsWhiteSpace(_source[pos])) break;
			}

			return pos;
		}

		/// <summary>
		/// Moves to the end of the line, to just before the line-end chars.
		/// </summary>
		public void SeekEndOfLine()
		{
			while (_pos < _length)
			{
				switch (_source[_pos])
				{
					case '\r':
					case '\n':
						return;
					default:
						_pos++;
						break;
				}
			}
		}

		public bool PositionsAreOnDifferentLines(int pos1, int pos2)
		{
			if (pos1 < 0 || pos1 > _length) throw new ArgumentOutOfRangeException("pos1");
			if (pos2 < 0 || pos2 > _length) throw new ArgumentOutOfRangeException("pos2");

			if (pos1 > pos2)
			{
				var tmp = pos1;
				pos1 = pos2;
				pos2 = tmp;
			}

			for (int i = pos1; i < pos2; i++)
			{
				if (_source[i] == '\n') return true;
			}

			return false;
		}

        #region Code Items
        public CodeItem? ReadItem()
        {
			if (Read())
            {
				return new CodeItem(_tokenType, new CodeSpan(_tokenStartPos, _tokenStartPos + _tokenText.Length), _tokenText.ToString());
            }

			return null;
		}

		public CodeItem? ReadItemNestable()
        {
			if (ReadNestable())
            {
				return new CodeItem(_tokenType, new CodeSpan(_tokenStartPos, _tokenStartPos + _tokenText.Length), _tokenText.ToString());
            }

			return null;
        }
		#endregion
	}
}
