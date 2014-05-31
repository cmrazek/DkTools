using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text;

namespace DkTools.LanguageSvc
{
	class ProbeScanner : IScanner
	{
		private IVsTextBuffer _buf = null;
		private string _source = "";
		private int _pos = 0;
		private int _length = 0;

		public ProbeScanner(IVsTextBuffer buf)
		{
			_buf = buf;
		}

		private static Regex _rxWord = new Regex(@"\G[a-zA-Z_]\w*");
		private static Regex _rxNumber = new Regex(@"\G\d+(?:\.\d+)?");
		private static Regex _rxCharLiteral = new Regex(@"\G'(?:\\'|[^'])*'");
		private static Regex _rxLineComment = new Regex(@"\G//.*$");
		private static Regex _rxCommentStart = new Regex(@"\G/\*");
		private static Regex _rxCommentEnd = new Regex(@"\*/");
		private static Regex _rxPreprocessor = new Regex(@"\G\#\w+");
		private static Regex _rxReplaceStart = new Regex(@"\G\#replace");
		private static Regex _rxReplaceWith = new Regex(@"\G\#with");

		private const int k_state_comment = 0x01;

		bool IScanner.ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state)
		{
			if (_pos >= _length) return false;

			tokenInfo.StartIndex = _pos;

			Match match;
			char ch = _source[_pos];

			if ((state & k_state_comment) != 0)
			{
				// Inside a multi-line comment.

				tokenInfo.Type = TokenType.Comment;
				tokenInfo.Color = TokenColor.Text;

				if ((match = _rxCommentEnd.Match(_source, _pos)).Success)
				{
					// Comment end occurs on same line
					_pos = match.Index + match.Length;
					state &= ~k_state_comment;
				}
				else
				{
					// No comment end on this line.
					_pos = _length;
				}
			}
			else if (Char.IsWhiteSpace(ch))
			{
				tokenInfo.Type = TokenType.WhiteSpace;
				tokenInfo.Color = TokenColor.Text;

				_pos++;
				while (_pos < _length && Char.IsWhiteSpace(_source[_pos])) _pos++;
			}
			else if ((match = _rxLineComment.Match(_source, _pos)).Success)
			{
				tokenInfo.Type = TokenType.Comment;
				tokenInfo.Color = TokenColor.Text;
				_pos = match.Index + match.Length;
			}
			else if ((match = _rxCommentStart.Match(_source, _pos)).Success)
			{
				tokenInfo.Type = TokenType.Comment;
				tokenInfo.Color = TokenColor.Text;

				if ((match = _rxCommentEnd.Match(_source, _pos)).Success)
				{
					// Comment end occurs on same line
					_pos = match.Index + match.Length;
					state &= ~k_state_comment;
				}
				else
				{
					// No comment end on this line.
					_pos = _length;
					state |= k_state_comment;
				}
			}
			else if ((match = _rxWord.Match(_source, _pos)).Success)
			{
				tokenInfo.Type = TokenType.Identifier;
				tokenInfo.Color = TokenColor.Text;
				_pos = match.Index + match.Length;
			}
			else if ((match = _rxNumber.Match(_source, _pos)).Success)
			{
				tokenInfo.Type = TokenType.Literal;
				tokenInfo.Color = TokenColor.Number;
				_pos = match.Index + match.Length;
			}
			else if ((ch == '\"' || ch == '\'') && MatchStringLiteral())
			{
				tokenInfo.Type = TokenType.Literal;
				tokenInfo.Color = TokenColor.Text;
			}
			else if ((match = _rxReplaceStart.Match(_source, _pos)).Success)
			{
				tokenInfo.Type = TokenType.Text;
				tokenInfo.Color = TokenColor.Text;
				_pos = match.Index + match.Length;
			}
			else if ((match = _rxPreprocessor.Match(_source, _pos)).Success)
			{
				tokenInfo.Type = TokenType.Text;
				tokenInfo.Color = TokenColor.Text;
				_pos = match.Index + match.Length;
			}
			else if (ch == '.')
			{
				tokenInfo.Type = TokenType.Delimiter;
				tokenInfo.Color = TokenColor.Text;
				tokenInfo.Trigger |= TokenTriggers.MemberSelect;
				_pos++;
			}
			else if (ch == ',')
			{
				tokenInfo.Type = TokenType.Delimiter;
				tokenInfo.Color = TokenColor.Text;
				_pos++;
			}
			else if (ch == '(' || ch == ')' || ch == '{' || ch == '}' || ch == '[' || ch == ']')
			{
				tokenInfo.Type = TokenType.Operator;
				tokenInfo.Color = TokenColor.Text;
				tokenInfo.Trigger = TokenTriggers.MatchBraces;
				_pos++;
			}
			else
			{
				if (Constants.OperatorChars.Contains(ch))
				{
					tokenInfo.Type = TokenType.Operator;
					tokenInfo.Color = TokenColor.Text;
				}
				else
				{
					tokenInfo.Type = TokenType.Unknown;
					tokenInfo.Color = TokenColor.Text;
				}

				_pos++;
			}

			tokenInfo.EndIndex = _pos - 1;
			return true;
		}

		void IScanner.SetSource(string source, int offset)
		{
			_source = source;
			_pos = offset;
			_length = _source.Length;
		}

		private bool MatchStringLiteral()
		{
			var startCh = _source[_pos];
			if (startCh != '\"' && startCh != '\'') return false;

			char ch;
			for (_pos = _pos + 1; _pos < _length; _pos++)
			{
				ch = _source[_pos];
				if (ch == startCh)
				{
					_pos++;
					break;
				}
				else if (ch == '\\' && _pos + 1 < _source.Length)
				{
					_pos++;
				}
			}

			return true;
		}
	}
}
