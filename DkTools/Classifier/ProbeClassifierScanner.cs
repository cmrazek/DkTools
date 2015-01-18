using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VsText = Microsoft.VisualStudio.Text;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;
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
				var snapStart = _model.Snapshot.TranslateOffsetToSnapshot(token.Span.Start, snapshot);
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

		private static readonly char[] k_commentEndKickOffChars = new char[] { '*', '/' };

		public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state)
		{
			if (_pos >= _length) return false;

			tokenInfo.StartIndex = _pos;

			Match match;
			char ch = _source[_pos];
			Token token;

			if ((state & State.MultiLineCommentMask) != 0)
			{
				// Inside a multi-line comment.

				if ((state & State.Disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Comment;

				SkipComments(ref state);
			}
			else if ((state & State.StringLiteral_Mask) != 0)
			{
				if ((state & State.Disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.StringLiteral;

				if ((state & State.StringLiteral_Single) != 0)
				{
					MatchStringLiteral(false, '\'', ref state);
				}
				else if ((state & State.StringLiteral_Double) != 0)
				{
					MatchStringLiteral(false, '\"', ref state);
				}
				else
				{
					// This should never happen.
					_pos++;
				}
			}
			else if (Char.IsWhiteSpace(ch))
			{
				if ((state & State.Disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Normal;

				_pos++;
				while (_pos < _length && Char.IsWhiteSpace(_source[_pos])) _pos++;

				if (ch == '\r' || ch == '\n') state &= ~State.SingleLineComment;
			}
			else if ((state & State.AfterInclude) != 0 && (match = _rxIncludeString.Match(_source, _pos)).Success)
			{
				if ((state & State.Disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.StringLiteral;
				_pos = match.Index + match.Length;
				state &= ~State.AfterInclude;
			}
			else if (ch == '/' && _pos + 1 < _length && _source[_pos + 1] == '/')
			{
				if ((state & State.Disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Comment;

				var index = _source.IndexOf('\n', _pos);
				if (index >= 0)
				{
					_pos = index + 1;
				}
				else
				{
					_pos = _length;
					state |= State.SingleLineComment;
				}
			}
			else if (ch == '/' && _pos + 1 < _length && _source[_pos + 1] == '*')
			{
				if ((state & State.Disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Comment;

				_pos += 2;
				state = (state & ~State.MultiLineCommentMask) | 1;	// Set comment level to 1.
				SkipComments(ref state);
			}
			else if ((match = _rxWord.Match(_source, _pos)).Success)
			{
				var word = match.Value;

				if ((state & State.Disabled) != 0)
				{
					tokenInfo.Type = ProbeClassifierType.Inactive;
				}
				else
				{
					StatementCompletion.StatementState stmt = (StatementCompletion.StatementState)(state >> 16);

					var gotKeyword = false;
					foreach (var keyword in StatementCompletion.StatementLayout.GetNextPossibleKeywords(stmt))
					{
						if (keyword == word)
						{
							tokenInfo.Type = ProbeClassifierType.Keyword;
							gotKeyword = true;
							break;
						}
					}

					if (!gotKeyword)
					{
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

					stmt = StatementCompletion.StatementLayout.ProcessWord(word, stmt);
					state = (((int)stmt) << 16) | (state & ~State.StatementMask);
				}

				_pos = match.Index + match.Length;
			}
			else if ((match = _rxNumber.Match(_source, _pos)).Success)
			{
				if ((state & State.Disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Number;
				_pos = match.Index + match.Length;

				StatementCompletion.StatementState stmt = (StatementCompletion.StatementState)(state >> 16);
				stmt = StatementCompletion.StatementLayout.ProcessNumber(stmt);
				state = (((int)stmt) << 16) | (state & ~State.StatementMask);
			}
			else if ((ch == '\"' || ch == '\'') && MatchStringLiteral(true, ch, ref state))
			{
				if ((state & State.Disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.StringLiteral;
				//_pos = match.Index + match.Length;

				StatementCompletion.StatementState stmt = (StatementCompletion.StatementState)(state >> 16);
				stmt = StatementCompletion.StatementLayout.ProcessStringLiteral(stmt);
				state = (((int)stmt) << 16) | (state & ~State.StatementMask);
			}
			else if ((match = _rxPreprocessor.Match(_source, _pos)).Success)
			{
				if ((state & State.Disabled) != 0) tokenInfo.Type = ProbeClassifierType.Inactive;
				else tokenInfo.Type = ProbeClassifierType.Preprocessor;
				_pos = match.Index + match.Length;

				if (match.Value == "#include") state |= State.AfterInclude;
			}
			else if ((state & State.Disabled) != 0)	// Everything after this point is assumed to be single character
			{
				tokenInfo.Type = ProbeClassifierType.Inactive;
				_pos++;
			}
			else if (ch == '.' || ch == ',')
			{
				tokenInfo.Type = ProbeClassifierType.Delimiter;
				_pos++;

				StatementCompletion.StatementState stmt = (StatementCompletion.StatementState)(state >> 16);
				stmt = StatementCompletion.StatementLayout.ProcessSymbol(ch, stmt);
				state = (((int)stmt) << 16) | (state & ~State.StatementMask);
			}
			else if (ch == '(' || ch == ')' || ch == '{' || ch == '}' || ch == '[' || ch == ']')
			{
				tokenInfo.Type = ProbeClassifierType.Operator;
				_pos++;

				StatementCompletion.StatementState stmt = (StatementCompletion.StatementState)(state >> 16);
				stmt = StatementCompletion.StatementLayout.ProcessSymbol(ch, stmt);
				state = (((int)stmt) << 16) | (state & ~State.StatementMask);
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

				StatementCompletion.StatementState stmt = (StatementCompletion.StatementState)(state >> 16);
				stmt = StatementCompletion.StatementLayout.ProcessSymbol(ch, stmt);
				state = (((int)stmt) << 16) | (state & ~State.StatementMask);
			}

			tokenInfo.Length = _pos - tokenInfo.StartIndex;
			return true;
		}

		private bool MatchStringLiteral(bool starting, char startCh, ref int state)
		{
			char ch;

			if (starting)
			{
				ch = _source[_pos];
#if DEBUG
				if (ch != startCh) throw new InvalidOperationException("First char read does not match specified start char.");
#endif
				if (ch == '\'')
				{
					state |= State.StringLiteral_Single;
				}
				else if (ch == '\"')
				{
					state |= State.StringLiteral_Double;
				}
#if DEBUG
				else
				{
					throw new InvalidOperationException("First char of string literal must be a quote.");
				}
#endif
				_pos++;
			}
			
			while (_pos < _length)
			{
				ch = _source[_pos];

				if ((state & State.StringLiteral_AfterBackslash) != 0)
				{
					if (ch == '\r')
					{
						// Skip over \r because it is always followed by \n on Windows
						_pos++;
					}
					else
					{
						// Normal char or \n
						_pos++;
						state &= ~State.StringLiteral_AfterBackslash;
					}
				}
				else if (ch == startCh)
				{
					_pos++;
					state &= ~State.StringLiteral_Mask;
					break;
				}
				else if (ch == '\r' || ch == '\n')
				{
					// String literal stops at the end of the line when there isn't '\' as the last char
					_pos++;
					state &= ~State.StringLiteral_Mask;
					break;
				}
				else if (ch == '\\')
				{
					state |= State.StringLiteral_AfterBackslash;
					_pos++;
				}
				else
				{
					// Normal char
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

		private void SkipComments(ref int state)
		{
			char ch;

			while (_pos < _length & (state & State.MultiLineCommentMask) != 0)
			{
				var index = _source.IndexOfAny(k_commentEndKickOffChars, _pos);
				if (index < 0)
				{
					// No comment start or ends on this line.
					_pos = _length;
				}
				else
				{
					ch = _source[index];
					if (ch == '*')
					{
						if (index + 1 < _length && _source[index + 1] == '/')
						{
							var level = state & State.MultiLineCommentMask;
							level--;
							if (level <= 0)
							{
								// End of outer comment
								state &= ~State.MultiLineCommentMask;
							}
							else
							{
								// End of inner comment. Not out of the woods yet...
								state = (state & ~State.MultiLineCommentMask) | (level & State.MultiLineCommentMask);
							}
							_pos = index + 2;
						}
						else
						{
							// False positive
							_pos = index + 1;
						}
					}
					else if (ch == '/')
					{
						if (index + 1 < _length && _source[index + 1] == '*')
						{
							// Start of a nested comment
							var level = state & State.MultiLineCommentMask;
							if (level < State.MultiLineCommentMask) level++;
							state = (state & ~State.MultiLineCommentMask) | (level & State.MultiLineCommentMask);
							_pos = index + 2;
						}
						else
						{
							// False positive
							_pos = index + 1;
						}
					}
					else
					{
						// This shouldn't happen
						_pos = index + 1;
					}
				}
			}
		}
	}
}
