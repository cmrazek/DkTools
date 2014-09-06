using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VsText = Microsoft.VisualStudio.Text;
using DkTools.CodeModel.Tokens;

namespace DkTools.Classifier
{
	class ProbeClassifierScanner
	{
		private CodeModel.CodeModel _model;
		private string _source;
		private VsText.ITextSnapshot _snapshot;
		private int _pos;
		private int _posOffset;
		private int _length;
		private FunctionFileScanning.FFScanner _functionScanner;
		private Dictionary<int, Token> _tokenMap;

		public ProbeClassifierScanner()
		{
			_functionScanner = ProbeToolsPackage.Instance.FunctionFileScanner;
		}

		public void SetSource(string source, int offset, VsText.ITextSnapshot snapshot, CodeModel.CodeModel model)
		{
			_source = source;
			_pos = 0;
			_posOffset = offset;
			_length = _source.Length;
			_snapshot = snapshot;
			_model = model;

			var transStart = snapshot.TranslateOffsetToSnapshot(offset, _model.Snapshot);
			var transEnd = snapshot.TranslateOffsetToSnapshot(offset + source.Length, _model.Snapshot);

			_tokenMap = new Dictionary<int, Token>();
			foreach (var token in _model.File.FindDownward(transStart, transEnd - transStart))
			{
				var snapStart = _model.Snapshot.TranslateOffsetToSnapshot(token.Span.Start.Offset, snapshot);
				_tokenMap[snapStart] = token;
			}
		}

		public class TokenInfo
		{
			public int StartIndex;
			public int Length;
			public ProbeClassifierType Type;
		}

		private static Regex _rxWord = new Regex(@"\G[a-zA-Z_]\w*");
		private static Regex _rxNumber = new Regex(@"\G\d+(?:\.\d+)?");
		private static Regex _rxCharLiteral = new Regex(@"\G'(?:\\'|[^'])*'");
		private static Regex _rxPreprocessor = new Regex(@"\G\#\w+");
		private static Regex _rxIncludeString = new Regex(@"\G(?:<[^>]+>|""[^""]+"")");

		private const int k_state_comment = 0x01;
		//private const int k_state_replace = 0x02;
		public const int k_state_disabled = 0x40;
		private const int k_state_afterInclude = 0x10;

		public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state)
		{
			if (_pos >= _length) return false;

			tokenInfo.StartIndex = _pos;

			Match match;
			char ch = _source[_pos];

			if ((state & k_state_comment) != 0)
			{
				// Inside a multi-line comment.

				if ((state & k_state_disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Comment;

				var index = _source.IndexOf("*/", _pos);
				if (index >= 0)
				{
					// Comment end occurs on same line
					_pos = index + 2;
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
				if ((state & k_state_disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Normal;

				_pos++;
				while (_pos < _length && Char.IsWhiteSpace(_source[_pos])) _pos++;
			}
			else if ((state & k_state_afterInclude) != 0 && (match = _rxIncludeString.Match(_source, _pos)).Success)
			{
				if ((state & k_state_disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.StringLiteral;
				_pos = match.Index + match.Length;
				state &= ~k_state_afterInclude;
			}
			else if (ch == '/' && _pos + 1 < _length && _source[_pos + 1] == '/')
			{
				if ((state & k_state_disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Comment;

				var index = _source.IndexOf('\n', _pos);
				if (index >= 0) _pos = index + 1;
				else _pos = _length;
			}
			else if (ch == '/' && _pos + 1 < _length && _source[_pos + 1] == '*')
			{
				if ((state & k_state_disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Comment;

				var index = _source.IndexOf("*/", _pos);
				if (index >= 0)
				{
					// Comment end occurs on same line
					_pos = index + 2;
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
				var word = match.Value;

				if ((state & k_state_disabled) != 0)
				{
					tokenInfo.Type = ProbeClassifierType.Inactive;
				}
				else
				{
					CodeModel.Tokens.Token token;
					if (_tokenMap.TryGetValue(_pos + _posOffset, out token) && token.Text == word)
					{
						var def = token.SourceDefinition;
						if (def != null) tokenInfo.Type = def.ClassifierType;
						else tokenInfo.Type = token.ClassifierType;
					}
					else if (Constants.Keywords.Contains(word)) tokenInfo.Type = ProbeClassifierType.Keyword;
					else if (Constants.DataTypeKeywords.Contains(word)) tokenInfo.Type = ProbeClassifierType.DataType;
					else tokenInfo.Type = ProbeClassifierType.Normal;
				}

				_pos = match.Index + match.Length;
			}
			else if ((match = _rxNumber.Match(_source, _pos)).Success)
			{
				if ((state & k_state_disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Number;
				_pos = match.Index + match.Length;
			}
			else if ((ch == '\"' || ch == '\'') && MatchStringLiteral())
			{
				if ((state & k_state_disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.StringLiteral;
				//_pos = match.Index + match.Length;
			}
			else if ((match = _rxPreprocessor.Match(_source, _pos)).Success)
			{
				if ((state & k_state_disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Preprocessor;
				_pos = match.Index + match.Length;

				if (match.Value == "#include") state |= k_state_afterInclude;
			}
			else if ((state & k_state_disabled) != 0)	// Everything after this point is assumed to be single character
			{
				tokenInfo.Type = ProbeClassifierType.Inactive;
				_pos++;
			}
			else if (ch == '.' || ch == ',')
			{
				tokenInfo.Type = ProbeClassifierType.Delimiter;
				_pos++;
			}
			else if (ch == '(' || ch == ')' || ch == '{' || ch == '}' || ch == '[' || ch == ']')
			{
				tokenInfo.Type = ProbeClassifierType.Operator;
				_pos++;
			}
			else
			{
				if (Constants.OperatorChars.Contains(ch))
				{
					tokenInfo.Type = ProbeClassifierType.Operator;
				}
				else
				{
					// Unknown character
					tokenInfo.Type = ProbeClassifierType.Normal;
				}

				_pos++;
			}

			tokenInfo.Length = _pos - tokenInfo.StartIndex;
			return true;
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

		public int Position
		{
			get { return _pos; }
			set
			{
				if (value < 0 || value > _source.Length) throw new ArgumentOutOfRangeException();
				_pos = value;
			}
		}

		public int Length
		{
			get { return _length; }
			set
			{
				if (value < 0 || value > _source.Length) throw new ArgumentOutOfRangeException();
				_length = value;
			}
		}

		public int PositionOffset
		{
			get { return _posOffset; }
		}

		public IEnumerable<CodeModel.Tokens.Token> TokensAtPos()
		{
			var modelPos = _pos + _posOffset;
			if (_snapshot != null && _model.Snapshot != null) modelPos = _snapshot.TranslateOffsetToSnapshot(modelPos, _model.Snapshot);
			return _model.FindTokens(modelPos);
		}

		public CodeModel.CodeModel CodeModel
		{
			get { return _model; }
		}
	}
}
