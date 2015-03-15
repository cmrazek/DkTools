using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DkTools.TokenParser
{
	internal sealed class Parser : IEnumerable<Token>
	{
		private int _pos;
		private string _source;
		private int _length;
		private StringBuilder _tokenText = new StringBuilder();
		private string _tokenTextStr;
		private TokenType _tokenType = TokenType.Unknown;
		private bool _returnWhiteSpace = false;
		private bool _returnComments = false;
		private int _tokenStartPos;
		private bool _tokenTerminated = true;
		private bool _enumNestable = false;
		private int _documentOffset;
		private StringBuilder _peekSB = new StringBuilder();

		public Parser(string source)
		{
			SetSource(source);
		}

		public Parser(string source, bool enumerateNestable)
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
					_tokenText.Append(ch);
					_pos++;
					while (_pos < _length && char.IsWhiteSpace(_source[_pos]))
					{
						_tokenText.Append(_source[_pos++]);
					}

					if (_returnWhiteSpace)
					{
						_tokenType = TokenType.WhiteSpace;
						return true;
					}
					else continue;
				}

				if (char.IsLetter(ch) || ch == '_')
				{
					while (_pos < _length && (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '_'))
					{
						_tokenText.Append(_source[_pos++]);
					}

					if ((_tokenText.Length == 3 && _tokenText.ToString() == "and") ||
						(_tokenText.Length == 2 && _tokenText.ToString() == "or"))
					{
						_tokenType = TokenParser.TokenType.Operator;
					}
					else
					{
						_tokenType = TokenType.Word;
					}
					return true;
				}

				if (char.IsDigit(ch))
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

					_tokenType = TokenType.Number;
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
							_tokenText.Append(ch);
							_tokenTerminated = true;
							_pos++;
							break;
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

					_tokenType = TokenType.StringLiteral;
					return true;
				}

				if (ch == '/')
				{
					if (_pos + 1 < _length && _source[_pos + 1] == '/')
					{
						var index = _source.IndexOf('\n', _pos);
						if (index < 0)
						{
							index = _length;
							_tokenTerminated = false;
						}
						else
						{
							index++;
							_tokenTerminated = true;
						}

						if (_returnComments)
						{
							while (_pos < index)
							{
								_tokenText.Append(_source[_pos++]);
							}

							_tokenType = TokenType.Comment;
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
							_tokenType = TokenType.Comment;
							return true;
						}
						else continue;
					}

					if (_pos + 1 < _length && _source[_pos + 1] == '=')
					{
						_tokenText.Append("/=");
						_pos += 2;
						_tokenType = TokenParser.TokenType.Operator;
						return true;
					}

					_tokenText.Append("/");
					_pos++;
					_tokenType = TokenParser.TokenType.Operator;
					return true;
				}

				if (ch == '+' || ch == '-' || ch == '*' || ch == '%' || ch == '=' || ch == '!' || ch == '<' || ch == '>')
				{
					_tokenText.Append(ch);
					_pos++;
					_tokenType = TokenParser.TokenType.Operator;

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
					_tokenType = TokenParser.TokenType.Operator;
					return true;
				}

				if (ch == '&')
				{
					_tokenText.Append('&');
					_pos++;
					_tokenType = TokenParser.TokenType.Operator;

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
					_tokenType = TokenParser.TokenType.Operator;

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

						_tokenType = TokenType.Preprocessor;
						return true;
					}
					else
					{
						_tokenType = TokenType.Operator;
						return true;
					}
				}

				_tokenText.Append(ch);
				_pos++;
				_tokenType = TokenType.Unknown;
				return true;
			}

			// End of file
			_tokenType = TokenType.WhiteSpace;
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

		public CodeModel.Span TokenSpan
		{
			get { return new CodeModel.Span(_tokenStartPos, _tokenStartPos + _tokenText.Length); }
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

		public Token Token
		{
			get
			{
				return new Token(_tokenType, _tokenText.ToString(), _tokenStartPos, Position);
			}
		}

		public TokenType TokenType
		{
			get { return _tokenType; }
		}

		public string TokenText
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

		public bool ReadNestable()
		{
			var startPos = Position;

			if (!Read()) return false;

			var firstTokenType = _tokenType;

			if (_tokenType == TokenParser.TokenType.Operator)
			{
				switch (_tokenText.ToString())
				{
					case "(":
						if (ReadNestable_Inner(")"))
						{
							_tokenStartPos = startPos;
							_tokenText.Clear();
							_tokenText.Append(_source.Substring(_tokenStartPos, _pos - _tokenStartPos));
							_tokenType = TokenParser.TokenType.Nested;
							return true;
						}
						break;
					case "{":
						if (ReadNestable_Inner("}"))
						{
							_tokenStartPos = startPos;
							_tokenText.Clear();
							_tokenText.Append(_source.Substring(_tokenStartPos, _pos - _tokenStartPos));
							_tokenType = TokenParser.TokenType.Nested;
							return true;
						}
						break;
					case "[":
						if (ReadNestable_Inner("]"))
						{
							_tokenStartPos = startPos;
							_tokenText.Clear();
							_tokenText.Append(_source.Substring(_tokenStartPos, _pos - _tokenStartPos));
							_tokenType = TokenParser.TokenType.Nested;
							return true;
						}
						break;
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
				if (_tokenType == TokenParser.TokenType.Operator)
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

		public string Text
		{
			get { return _source; }
		}

		public bool Peek()
		{
			var pos = Position;
			if (!Read()) return false;
			Position = pos;
			return true;
		}

		public static string StringLiteralToString(string str)
		{
			if (str.StartsWith("\"") || str.StartsWith("\'")) str = str.Substring(1);
			if (str.EndsWith("\"") || str.EndsWith("\'")) str = str.Substring(0, str.Length - 1);

			var sb = new StringBuilder(str.Length);
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == '\\' && i + 1 < str.Length)
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
				else sb.Append(str[i]);
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

		public IEnumerator<Token> GetEnumerator()
		{
			return new ParserEnumerator(this, _enumNestable);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ParserEnumerator(this, _enumNestable);
		}

		public bool ReadWord()
		{
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return false;

			if (_pos >= _length || !_source[_pos].IsWordChar(true)) return false;

			_tokenText.Clear();
			_tokenTextStr = null;
			_tokenType = TokenParser.TokenType.Word;
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

		public string PeekWord()
		{
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return null;

			if (_pos >= _length || !_source[_pos].IsWordChar(true)) return null;

			_peekSB.Clear();

			var pos = _pos;
			char ch;
			while (pos < _length)
			{
				ch = _source[pos];
				if (!ch.IsWordChar(false)) return _peekSB.ToString();
				_peekSB.Append(ch);
				pos++;
			}

			return _peekSB.ToString();
		}

		public bool ReadTagName()
		{
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return false;

			if (_pos >= _length || !_source[_pos].IsWordChar(true)) return false;

			_tokenText.Clear();
			_tokenTextStr = null;
			_tokenType = TokenParser.TokenType.Word;
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
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return false;

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
			_tokenType = TokenParser.TokenType.Unknown;
			return true;
		}

		public bool ReadExact(char expecting)
		{
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return false;

			if (_pos >= _length || _source[_pos] != expecting) return false;

			_tokenStartPos = _pos;
			_pos++;

			_tokenText.Clear();
			_tokenText.Append(expecting);
			_tokenTextStr = null;
			_tokenType = TokenParser.TokenType.Unknown;
			return true;
		}

		public bool ReadPattern(Regex rx)
		{
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return false;
			if (_pos >= _length) return false;

			var match = rx.Match(_source, _pos);
			if (!match.Success) return false;
			if (match.Index != _pos) throw new InvalidOperationException("Regular expression read must begin with \\G in order to properly parse.");

			_tokenStartPos = _pos;
			_pos += match.Length;

			_tokenText.Clear();
			_tokenText.Append(match.Value);
			_tokenTextStr = null;
			_tokenType = TokenParser.TokenType.Unknown;
			return true;
		}

		public bool SkipWhiteSpaceAndCommentsIfAllowed()
		{
			char ch;
			while (_pos < _length)
			{
				ch = _source[_pos];
				if (char.IsWhiteSpace(ch))
				{
					if (_returnWhiteSpace) return true;
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
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return false;

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
			_tokenType = TokenParser.TokenType.Number;

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
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return false;

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
					_tokenText.Append(ch);
					_tokenTerminated = true;
					_pos++;
					break;
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

			_tokenType = TokenType.StringLiteral;
			return true;
		}

		public bool PeekExact(string expecting)
		{
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return false;

			var expLength = expecting.Length;
			if (_pos + expLength > _length) return false;

			for (int i = 0; i < expLength; i++)
			{
				if (_source[_pos + i] != expecting[i]) return false;
			}

			return true;
		}

		public bool PeekExact(char expecting)
		{
			if (!SkipWhiteSpaceAndCommentsIfAllowed()) return false;

			if (_pos >= _length || _source[_pos] != expecting) return false;

			return true;
		}

		public int DocumentOffset
		{
			get { return _documentOffset; }
			set { _documentOffset = value; }
		}

		public static string NormalizeText(string text)
		{
			var code = new Parser(text);
			var needSpace = false;
			TokenType lastTokenType = TokenType.Unknown;
			string lastTokenText = null;
			TokenType tokenType;
			string tokenText;
			var sb = new StringBuilder(text.Length);

			while (code.Read())
			{
				tokenType = code.TokenType;
				tokenText = code.TokenText;

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
				else if (tokenType == TokenType.Operator)
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
							if (lastTokenText == "(" || lastTokenText == "[" || lastTokenType == TokenType.Word) needSpace = false;
							else needSpace = true;
							break;
						case ")":
							if (lastTokenText == "(" || lastTokenText == ")" || lastTokenText == "]" ||
								lastTokenType == TokenType.Word || lastTokenType == TokenType.Number || lastTokenType == TokenType.StringLiteral)
							{
								needSpace = false;
							}
							else needSpace = true;
							break;
						case "[":
							if (lastTokenText == "(" || lastTokenText == "[" || lastTokenType == TokenType.Word) needSpace = false;
							else needSpace = true;
							break;
						case "]":
							if (lastTokenText == ")" || lastTokenText == "]" || lastTokenType == TokenType.Word || lastTokenType == TokenType.Number ||
								lastTokenType == TokenType.StringLiteral)
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
				else if (tokenType == TokenType.Word)
				{
					if (lastTokenText == "&") needSpace = false;
					else needSpace = true;
				}
				else if (tokenType == TokenType.Number || tokenType == TokenType.StringLiteral)
				{
					if (lastTokenText == "(" || lastTokenText == "[") needSpace = false;
					else needSpace = true;
				}
				else if (tokenType == TokenType.Preprocessor)
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
	}
}
