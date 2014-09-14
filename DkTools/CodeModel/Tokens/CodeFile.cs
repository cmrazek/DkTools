using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using VsText = Microsoft.VisualStudio.Text;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Tokens
{
	internal class CodeFile : GroupToken
	{
		#region Variables
		private CodeModel _model;
		private string[] _parentFiles;
		private Dictionary<string, DataType> _definedDataTypes = new Dictionary<string, DataType>();
		private string _className;
		#endregion

		#region Construction
		public CodeFile(CodeModel model)
			: base(null, new Scope(model), 0)
		{
			if (model == null) throw new ArgumentNullException("model");
			_model = model;
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public CodeSource CodeSource
		{
			get { return _source; }
		}

		public CodeModel Model
		{
			get { return _model; }
		}

		public IEnumerable<string> ParentFiles
		{
			get { return _parentFiles; }
		}

		public void AddImplicitInclude(CodeFile file)
		{
			file.CopyDefinitionsToToken(this, false);
		}

		public string ClassName
		{
			get { return _className; }
		}
		#endregion

		#region Parsing
		private CodeSource _source;
		private string _src;
		private string _fileName;
		private int _pos = 0;
		private int _length = 0;

		public void Parse(CodeSource source, string fileName, IEnumerable<string> parentFiles, bool visible)
		{
			if (source == null) throw new ArgumentNullException("source");
			_source = source;
			_src = _source.Text;
			_fileName = fileName;
			_parentFiles = parentFiles.ToArray();

			_pos = 0;
			_length = _src.Length;

			FunctionFileScanning.FFUtil.FileNameIsClass(_fileName, out _className);

			var scope = new Scope(this, 0, ScopeHint.None, visible, _model.DefinitionProvider);
			Scope = scope;

			ParseScope(scope, t => ParseScopeResult.Continue);

			Span = new Span(0, Position);
		}

		public Token TryParseComplexToken(GroupToken parent, Scope scope)
		{
			if (!SkipWhiteSpaceAndComments(scope)) return null;
		    var ch = _src[_pos];

			Token token;

			if (scope.Preprocessor)
			{
				if (!scope.Hint.HasFlag(ScopeHint.SuppressFunctionDefinition))
				{
					if ((token = FunctionToken.TryParse(parent, scope)) != null) return token;
				}
				if (!scope.Hint.HasFlag(ScopeHint.SuppressVarDecl) && scope.Preprocessor)
				{
					if ((token = VariableDeclarationToken.TryParse(parent, scope)) != null) return token;
				}
			}

			if ((token = DataTypeToken.TryParse(parent, scope)) != null) return token;
			if ((token = DefineToken.TryParse(parent, scope)) != null) return token;

		    if (ch.IsWordChar(true))
		    {
		        var word = PeekWord();
				return ParseWord(parent, scope, MoveNextSpan(word.Length), word);
			}

			#region Literals
			if (char.IsDigit(ch))
			{
				return NumberToken.Parse(parent, scope);
			}

			if (ch == '\"' || ch == '\'')
			{
				var startPos = Position;
				ParseStringLiteral();
				var span = new Span(startPos, Position);
				return new StringLiteralToken(parent, scope, span, GetText(span));
			}
			#endregion

			#region Operators
			if (ch == '-')
			{
				if (_pos + 1 < _length && Char.IsDigit(_src[_pos + 1]))
				{
					// Number with leading minus sign
					var startPos = Position;
					ParseNumber();
					var span = new Span(startPos, Position);
					return new NumberToken(parent, scope, span, GetText(span));
				}
				if (_pos + 1 < _length && _src[_pos + 1] == '=') return new OperatorToken(parent, scope, MoveNextSpan(2), "-=");
				return new OperatorToken(parent, scope, MoveNextSpan(), "-");
			}

			if (ch == '+')
			{
				if (_pos + 1 < _length && _src[_pos + 1] == '=') return new OperatorToken(parent, scope, MoveNextSpan(2), "+=");
				else return new OperatorToken(parent, scope, MoveNextSpan(), "+");
			}

			if (ch == '*')
			{
				if (_pos + 1 < _length && _src[_pos + 1] == '=') return new OperatorToken(parent, scope, MoveNextSpan(2), "*=");
				else return new OperatorToken(parent, scope, MoveNextSpan(), "*");
			}

			if (ch == '/')
			{
				if (_pos + 1 < _length && _src[_pos + 1] == '=') return new OperatorToken(parent, scope, MoveNextSpan(2), "/=");
				else return new OperatorToken(parent, scope, MoveNextSpan(), "/");
			}

			if (ch == '%')
			{
				if (_pos + 1 < _length && _src[_pos + 1] == '=') return new OperatorToken(parent, scope, MoveNextSpan(2), "%=");
				else return new OperatorToken(parent, scope, MoveNextSpan(), "%");
			}

			if (ch == '=')
			{
				if (_pos + 1 < _length && _src[_pos + 1] == '=') return new OperatorToken(parent, scope, MoveNextSpan(2), "==");
				else return new OperatorToken(parent, scope, MoveNextSpan(), "=");
			}

			if (ch == '!')
			{
				if (_pos + 1 < _length && _src[_pos + 1] == '=') return new OperatorToken(parent, scope, MoveNextSpan(2), "!=");
				else return new OperatorToken(parent, scope, MoveNextSpan(), "!");	// Not technically a probe operator...
			}
			#endregion

			#region Nestable
			if (ch == '(')
			{
				return BracketsToken.Parse(parent, scope);
			}

			if (ch == ')')
			{
				return new CloseBracketToken(parent, scope, MoveNextSpan(), null);
			}

			if (ch == '{')
			{
				return BracesToken.Parse(parent, scope);
			}

			if (ch == '}')
			{
				return new BraceToken(parent, scope, MoveNextSpan(), null, false);
			}

			if (ch == '[')
			{
				return ArrayBracesToken.Parse(parent, scope);
			}

			if (ch == ']')
			{
				return new ArrayBraceToken(parent, scope, MoveNextSpan(), null, false);
			}
			#endregion

			#region Special Operators
			if (ch == ',')
			{
				return new DelimiterToken(parent, scope, MoveNextSpan());
			}

			if (ch == '.')
			{
				return new DotToken(parent, scope, MoveNextSpan());
			}

			if (ch == ';')
			{
				return new StatementEndToken(parent, scope, MoveNextSpan());
			}

			if (ch == ':')
			{
				return new OperatorToken(parent, scope, MoveNextSpan(), ":");
			}

			if (ch == '&')
			{
				return new ReferenceToken(parent, scope, MoveNextSpan());
			}
			#endregion

			#region Preprocessor
			if (ch == '#')
			{
				var startPos = Position;
				MoveNext();	// Skip #
				SeekNonWordChar();
				var wordSpan = new Span(startPos, Position);
				var word = GetText(wordSpan);

				switch (word)
				{
					case "#insert":
						return InsertToken.Parse(parent, scope, new InsertStartToken(parent, scope, wordSpan));
					case "#endinsert":
						return new InsertEndToken(parent, scope, wordSpan);
					case "#define":
						return DefineToken.Parse(parent, scope, new PreprocessorToken(parent, scope, wordSpan, word));
					case "#replace":
						return ReplaceToken.Parse(parent, scope, new ReplaceStartToken(parent, scope, wordSpan));
					case "#with":
						return new ReplaceWithToken(parent, scope, wordSpan);
					case "#endreplace":
						return new ReplaceEndToken(parent, scope, wordSpan);
					case "#include":
						return IncludeToken.Parse(parent, scope, new PreprocessorToken(parent, scope, wordSpan, word));
					default:
						return new PreprocessorToken(parent, scope, wordSpan, word);
				}
			}
			#endregion

			return new UnknownToken(parent, scope, MoveNextSpan(), ch.ToString());
		}

		private Token ParseExtern(GroupToken parent, Scope scope, KeywordToken externToken)
		{
			var externScope = scope.Clone();
			externScope.Hint |= ScopeHint.SuppressFunctionCall | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;
			
			var resetPos = Position;
			Token token, token1, token2, token3;

			var tokens = new List<Token>();
			tokens.Add(externToken);

			if ((token1 = DataTypeToken.TryParse(parent, scope)) != null)
			{
				tokens.Add(token1);
				if ((token2 = IdentifierToken.TryParse(parent, scope)) != null)
				{
					tokens.Add(token2);

					var bracketsScope = scope;
					bracketsScope.Hint |= ScopeHint.FunctionArgs;

					if ((token3 = BracketsToken.TryParse(parent, bracketsScope)) != null)
					{
						// extern dataType name();

						tokens.Add(token3);
						if ((token = StatementEndToken.TryParse(parent, scope)) != null) tokens.Add(token);

						return new ExternFunctionToken(parent, scope, tokens, token1, token2 as IdentifierToken, token3 as BracketsToken);
					}
				}
			}
			else if ((token1 = IdentifierToken.TryParse(parent, scope)) != null)
			{
				tokens.Add(token1);

				var bracketsScope = scope;
				bracketsScope.Hint |= ScopeHint.FunctionArgs;

				if ((token2 = BracketsToken.TryParse(parent, bracketsScope)) != null)
				{
					// extern name();

					tokens.Add(token2);
					if ((token = StatementEndToken.TryParse(parent, scope)) != null) tokens.Add(token);

					return new ExternFunctionToken(parent, scope, tokens, null, token1 as IdentifierToken, token2 as BracketsToken);
				}
			}

			// If we got here, then there's an invalid token after 'extern'.  Return just the extern token by itself.
			Position = resetPos;
			return externToken;
		}

		/// <summary>
		/// Handles parsing a complex word token from the file.
		/// </summary>
		/// <param name="parent">Parent token</param>
		/// <param name="scope">Current scope</param>
		/// <param name="wordSpan">Span of the parsed word</param>
		/// <param name="word">The word found by the parser function</param>
		/// <returns>Always returns a new token for the word.</returns>
		/// <remarks>This function assumes that the current position is after the word.</remarks>
		private Token ParseWord(GroupToken parent, Scope scope, Span wordSpan, string word)
		{
			SkipWhiteSpaceAndComments(scope);

			if (!scope.Hint.HasFlag(ScopeHint.SuppressControlStatements))
			{
				if (word == "if") return IfStatementToken.Parse(parent, scope, new KeywordToken(parent, scope, wordSpan, word));
				if (word == "select") return SelectToken.Parse(parent, scope, new KeywordToken(parent, scope, wordSpan, word));
				if (word == "switch") return SwitchToken.Parse(parent, scope, new KeywordToken(parent, scope, wordSpan, word));
				if (word == "while") return WhileStatementToken.Parse(parent, scope, new KeywordToken(parent, scope, wordSpan, word));
			}

			if (word == "extern")
			{
				return ParseExtern(parent, scope, new KeywordToken(parent, scope, wordSpan, word));
			}

			if (Constants.GlobalKeywords.Contains(word) ||
				(Constants.FunctionKeywords.Contains(word) && parent.FindUpward(t => t is FunctionCallToken) != null) ||
				(Constants.SwitchKeywords.Contains(word) && parent.FindUpward(t => t is SwitchToken) != null) ||
				(scope.Hint.HasFlag(ScopeHint.SelectFrom) && Constants.SelectFromKeywords.Contains(word)) ||
				(scope.Hint.HasFlag(ScopeHint.SelectOrderBy) && Constants.SelectOrderByKeywords.Contains(word)))
			{
				return new KeywordToken(parent, scope, wordSpan, word);
			}

			Definition[] defs = null;

			var ch = PeekChar();
			if (ch == '(')
			{
				// Could be function call/def or macro call

				if (!scope.Hint.HasFlag(ScopeHint.SuppressFunctionDefinition) && scope.Preprocessor)
				{
					Position = wordSpan.Start;
					var funcToken = FunctionToken.TryParse(parent, scope);
					if (funcToken != null) return funcToken;
					Position = wordSpan.End;
				}

				defs = parent.GetDefinitions(word).ToArray();
				Definition bestDef = null;

				if (!scope.Preprocessor)
				{
					// Check if the preprocessor already found the macro or function definition at this position for us.

					foreach (var def in defs)
					{
						if (def.SourceSpan.Start == wordSpan.Start)
						{
							if (def is MacroDefinition)
							{
								var nameToken = new IdentifierToken(parent, scope, wordSpan, word);
								var brackets = BracketsToken.Parse(parent, scope);
								return new MacroCallToken(parent, scope, nameToken, brackets, def as MacroDefinition);
							}

							if (def is FunctionDefinition)
							{
								return new FunctionPlaceholderToken(parent, scope, wordSpan, word, def as FunctionDefinition);
							}

							bestDef = def;
							break;
						}
					}
				}

				foreach (var def in defs)
				{
					if (def is MacroDefinition)
					{
						var nameToken = new IdentifierToken(parent, scope, wordSpan, word);
						var brackets = BracketsToken.Parse(parent, scope);
						return new MacroCallToken(parent, scope, nameToken, brackets, def as MacroDefinition);
					}

					if (def is FunctionDefinition)
					{
						if (!scope.Hint.HasFlag(ScopeHint.SuppressFunctionCall))
						{
							var nameToken = new IdentifierToken(parent, scope, wordSpan, word);
							var brackets = BracketsToken.Parse(parent, scope);
							return new FunctionCallToken(parent, scope, null, null, nameToken, brackets, def as FunctionDefinition);
						}
					}
				}

				// Function call to an unknown function.
				if (!scope.Hint.HasFlag(ScopeHint.SuppressFunctionCall))
				{
					var nameToken = new IdentifierToken(parent, scope, wordSpan, word);
					var brackets = BracketsToken.Parse(parent, scope);
					return new FunctionCallToken(parent, scope, null, null, nameToken, brackets, null);
				}

				Position = wordSpan.End;
				SkipWhiteSpaceAndComments(scope);
			}
			else if (ch == '.')
			{
				var beforeDotPos = Position;
				var dotSpan = MoveNextSpan();
				var tok = TryParsePossibleTableOrClassReference(parent, scope, word, wordSpan, dotSpan);
				if (tok != null) return tok;
				else Position = beforeDotPos;
			}

			// If we got here, then the word is not followed by a '('.
			// This could be a variable, constants or some other type of stand-alone token.

			if (defs == null) defs = parent.GetDefinitions(word).ToArray();
			foreach (var def in defs)
			{
				if (def is VariableDefinition && !scope.Hint.HasFlag(ScopeHint.SuppressVars))
				{
					return new VariableToken(parent, scope, wordSpan, word, def as VariableDefinition);
				}

				if (def is DataTypeDefinition && !scope.Hint.HasFlag(ScopeHint.SuppressDataType))
				{
					return new DataTypeToken(parent, scope, new IdentifierToken(parent, scope, wordSpan, word), (def as DataTypeDefinition).DataType, def);
				}

				if (def is ConstantDefinition || def is StringDefDefinition)
				{
					return new ConstantToken(parent, scope, wordSpan, word, def);
				}

				if (def is TableDefinition)
				{
					var tableToken = new TableToken(parent, scope, wordSpan, word, def);
					if (SkipWhiteSpaceAndComments(scope))
					{
						var tableAndFieldToken = TableAndFieldToken.TryParse(parent, scope, tableToken);
						if (tableAndFieldToken != null) return tableAndFieldToken;
					}

					return tableToken;
				}

				if (def is RelIndDefinition)
				{
					return new RelIndToken(parent, scope, wordSpan, word, def);
				}

				if (def is ClassDefinition)
				{
					return new ClassToken(parent, scope, wordSpan, word, def as ClassDefinition);
				}
			}

			return new IdentifierToken(parent, scope, wordSpan, word);
		}

		private Token TryParsePossibleTableOrClassReference(GroupToken parent, Scope scope, string word1, Span word1Span, Span dotSpan)
		{
			// This function is called when a word has been parsed, and the next character is a '.'.
			// This could be a table.field combo or a class.method() combo.

			// Check that another word follows the dot.
			SkipWhiteSpaceAndComments(scope);
			var word2 = PeekWord();
			if (string.IsNullOrEmpty(word2)) return null;

			var tableDict = ProbeEnvironment.GetTable(word1);
			if (tableDict != null && tableDict.IsField(word2))
			{
				var word2Span = MoveNextSpan(word2.Length);
				var tableToken = new TableToken(parent, scope, word1Span, word1, tableDict.Definition);
				var dotToken = new DotToken(parent, scope, dotSpan);
				var fieldToken = new TableFieldToken(parent, scope, word2Span, word2, tableToken);
				return new TableAndFieldToken(parent, scope, tableToken, dotToken, fieldToken, tableDict.GetField(word2));
			}

			var classFF = ProbeToolsPackage.Instance.FunctionFileScanner.GetClass(word1);
			if (classFF != null && classFF.IsFunction(word2))
			{
				var word2Span = MoveNextSpan(word2.Length);

				SkipWhiteSpaceAndComments(scope);
				if (PeekChar() == '(')
				{
					var classToken = new ClassToken(parent, scope, word1Span, word1, classFF);
					var dotToken = new DotToken(parent, scope, dotSpan);
					var nameToken = new IdentifierToken(parent, scope, word2Span, word2);
					var argsToken = BracketsToken.Parse(parent, scope);
					return new FunctionCallToken(parent, scope, classToken, dotToken, nameToken, argsToken, classFF.GetFunctionDefinition(word2));
				}
			}

			return null;
		}

		private Regex _rxRegionStart = new Regex(@"\G//\s*#region\b\s*(.*)\s*$", RegexOptions.Multiline);
		private Regex _rxRegionEnd = new Regex(@"\G//\s*#endregion\b");

		public bool SkipWhiteSpaceAndComments(Scope scope)
		{
			if (_pos >= _length) return false;

			var gotComment = false;
			var commentStartPos = 0;
			var commentEndPos = 0;
			Match match;

			while (_pos < _length)
			{
				var ch = _src[_pos];

				if (Char.IsWhiteSpace(ch))
				{
					// WhiteSpace
					MoveNext();
					while (_pos < _length && Char.IsWhiteSpace(_src[_pos])) MoveNext();
					if (_pos >= _length) return false;
					ch = _src[_pos];
				}

				if (ch == '/')
				{
					if (_pos + 1 < _length && _src[_pos + 1] == '/')
					{
						// Single-line comment

						if ((match = _rxRegionStart.Match(_src, _pos)).Success)	// Region is starting.
						{
							// If there is a previously started comment, then add the comment region now.
							if (gotComment)
							{
								AddCommentRegion(scope, new Span(commentStartPos, commentEndPos));
								gotComment = false;
							}

							StartUserRegion(scope, Position, match.Groups[1].Value);
							SeekEndOfLine();
						}
						else if ((match = _rxRegionEnd.Match(_src, _pos)).Success)	// Region is ending.
						{
							// If there is a previously started comment, then add the comment region now.
							if (gotComment)
							{
								AddCommentRegion(scope, new Span(commentStartPos, commentEndPos));
								gotComment = false;
							}

							SeekEndOfLine();
							EndUserRegion(Position);
						}
						else
						{
							if (!gotComment)
							{
								commentStartPos = commentEndPos = Position;
								gotComment = true;
							}
							SeekEndOfLine();
							commentEndPos = Position;
						}
						continue;
					}
					else if (_pos + 1 < _length && _src[_pos + 1] == '*')
					{
						// Multi-line comment
						if (!gotComment)
						{
							commentStartPos = commentEndPos = Position;
							gotComment = true;
						}
						SeekMatch("*/");
						MoveNext(2);    // Skip past comment end
						commentEndPos = Position;
						continue;
					}
				}

				if (gotComment) AddCommentRegion(scope, new Span(commentStartPos, commentEndPos));

				return true;
			}

			if (gotComment) AddCommentRegion(scope, new Span(commentStartPos, commentEndPos));
			return false;
		}

		public void MoveNext()
		{
			if (_pos < _length) _pos++;
		}

		public void MoveNext(int numChars)
		{
			_pos += numChars;
			if (_pos > _length) _pos = _length;
		}

		public Span MoveNextSpan(int length = 1)
		{
			var startPos = Position;
			MoveNext(length);
			return new Span(startPos, Position);
		}

		public void SeekEndOfLine()
		{
			while (_pos < _length && _src[_pos] != '\r' && _src[_pos] != '\n') MoveNext();
		}

		public bool SeekMatch(string str)
		{
			if (string.IsNullOrEmpty(str)) throw new ArgumentException("Search term cannot be blank.");

			var maxPos = _length - str.Length;
			var startCh = str[0];
			while (_pos <= maxPos)
			{
				if (_src[_pos] == startCh && _src.Substring(_pos, str.Length) == str) return true;
				MoveNext();
			}

			return false;
		}

		public void SeekNonWordChar()
		{
			char ch;
			while (_pos < _length)
			{
				ch = _src[_pos];
				if (!Char.IsLetterOrDigit(ch) && ch != '_') return;
				MoveNext();
			}
		}

		public string GetText(Span span)
		{
			var startOffset = span.Start;
			if (startOffset < 0 || startOffset > _length) throw new ArgumentOutOfRangeException("span");

			var endOffset = span.End;
			if (endOffset < 0 || endOffset > _length || endOffset < startOffset) throw new ArgumentOutOfRangeException("span");

			return _src.Substring(startOffset, endOffset - startOffset);
		}

		public string GetText(int start, int length)
		{
			if (start < 0 || start > _length) throw new ArgumentOutOfRangeException("start");
			if (length < 0 || start + length > _length) throw new ArgumentOutOfRangeException("length");

			return _src.Substring(start, length);
		}

		public string GetRegionText(Span span)
		{
			var length = span.End - span.Start;
			if (length > Constants.OutliningMaxContextChars)
			{
				return GetText(span.Start, Constants.OutliningMaxContextChars) + "...";
			}
			else
			{
				return GetText(span);
			}
		}

		public void ParseNumber()
		{
			int startPos = _pos;
			bool gotDot = false;
			char ch;

			while (_pos < _length)
			{
				ch = _src[_pos];
				if (Char.IsDigit(ch))
				{
					MoveNext();
				}
				else if (ch == '.')
				{
					if (gotDot) return;
					gotDot = true;
					MoveNext();
				}
				else if (ch == '-' && _pos == startPos)
				{
					// Leading minus sign
					MoveNext();
				}
				else return;
			}
		}

		public void ParseStringLiteral()
		{
			var startCh = _src[_pos];
			var ch = '\0';
			var lastCh = '\0';

			MoveNext(); // Move past starting char.

			while (_pos < _length)
			{
				ch = _src[_pos];
				MoveNext();
				if (ch == '\\') MoveNext();	// Move past escaped char
				else if (ch == startCh || ch == '\n') return;
				lastCh = ch;
			}
		}

		public bool EndOfFile
		{
			get { return _pos >= _length; }
		}

		public bool IsMatch(string text)
		{
			return _pos + text.Length <= _src.Length && _src.Substring(_pos, text.Length) == text;
		}

		public bool SkipMatch(string text)
		{
			if (IsMatch(text))
			{
				MoveNext(text.Length);
				return true;
			}
			return false;
		}

		public bool SkipMatch(Regex regex)
		{
			var match = regex.Match(_src, _pos);
			if (match.Success)
			{
				var seekPos = match.Index + match.Length;
				while (_pos < seekPos) MoveNext();
				return true;
			}

			return false;
		}

		public bool SeekMatch(Regex regex)
		{
			var match = regex.Match(_src, _pos);
			if (match.Success)
			{
				var seekPos = match.Index;
				while (_pos < seekPos) MoveNext();
				return true;
			}

			return false;
		}

		public char PeekChar(int offset)
		{
			if (offset >= 0 && offset < _length) return _src[offset];
			return '\0';
		}

		public char PeekChar()
		{
			if (_pos < _length) return _src[_pos];
			return '\0';
		}

		public string PeekWord()
		{
			var sb = new StringBuilder();

			var pos = _pos;
			var first = true;
			while (pos < _length && _src[pos].IsWordChar(first))
			{
				sb.Append(_src[pos++]);
				first = false;
			}

			return sb.ToString();
		}
		#endregion

		#region Position calculations
		public int Position
		{
			get { return _pos; }
			set
			{
				if (value < 0 || value > _length) throw new ArgumentOutOfRangeException();
				_pos = value;
			}
		}

		// TODO: remove
		//public int FindPosition(int lineNum, int linePos)
		//{
		//	int pos = 0;
		//	int seekLineNum = 0;
		//	int seekLinePos = 0;

		//	while (pos < _length)
		//	{
		//		if (_src[pos] == '\n')
		//		{
		//			if (seekLineNum == lineNum)
		//			{
		//				return new Position(pos, seekLineNum, seekLinePos + 1);
		//			}

		//			seekLineNum++;
		//			seekLinePos = 0;
		//		}
		//		else
		//		{
		//			seekLinePos++;
		//		}
		//		pos++;

		//		if (seekLineNum == lineNum && seekLinePos == linePos)
		//		{
		//			return new Position(pos, lineNum, linePos);
		//		}
		//	}

		//	return new Position(pos, seekLineNum, seekLinePos);
		//}

		//public Position FindPosition(int offset)
		//{
		//	int pos = 0;
		//	int lineNum = 0;
		//	int linePos = 0;

		//	if (offset > _length) offset = _length;
		//	while (pos < offset)
		//	{
		//		if (_src[pos] == '\n')
		//		{
		//			lineNum++;
		//			linePos = 0;
		//		}
		//		else
		//		{
		//			linePos++;
		//		}
		//		pos++;
		//	}

		//	return new Position(pos, lineNum, linePos);
		//}

		public int FindStartOfLine(int pos)
		{
			if (pos > _length) pos = _length;
			while (pos > 0 && _src[pos - 1] != '\n') pos--;
			return pos;
		}

		public int FindEndOfPreviousLine(int pos)
		{
			var offset = FindStartOfLine(pos);
			if (offset <= 0) return 0;

			offset--;
			if (offset > 0 && _src[offset] == '\n' && _src[offset - 1] == '\r') offset--;
			return offset;
		}

		public int FindEndOfLine(int pos)
		{
			while (pos < _length && !_src[pos].IsEndOfLineChar()) pos++;
			return pos;
		}

		public int FindStartOfNextLine(int pos)
		{
			pos = FindEndOfLine(pos);
			if (pos < _length && _src[pos] == '\r') pos++;
			if (pos < _length && _src[pos] == '\n') pos++;
			return pos;
		}
		#endregion

		#region Regions
		private Dictionary<int, Region> _regions = new Dictionary<int, Region>();

		private enum RegionType
		{
			Comment,
			User
		}

		private class Region
		{
			public Scope scope;
			public Span span;
			public RegionType type;
			public string title;
		}

		private void AddCommentRegion(Scope scope, Span span)
		{
			if (scope.Visible)
			{
				// Start and end must be on separate lines.
				var startLineEnd = FindEndOfLine(span.Start);
				if (span.End > startLineEnd)
				{
					_regions[span.Start] = new Region
					{
						scope = scope,
						span = span,
						type = RegionType.Comment,
						title = GetText(span).GetFirstLine().Trim()
					};
				}
			}
		}

		public void StartUserRegion(Scope scope, int pos, string title)
		{
			_regions[pos] = new Region
			{
				scope = scope,
				span = new Span(pos, pos),
				type = RegionType.User,
				title = title.Trim()
			};
		}

		private void EndUserRegion(int pos)
		{
			// Find the region with the highest start, where the end has not been found yet.
			// - same as the start (uninitialized)
			// - equal to pos (already found by a previous call)

			var maxStart = int.MinValue;
			int start;

			foreach (var reg in _regions.Values)
			{
				if (reg.type != RegionType.User) continue;
				if (reg.span.End == pos) return;

				start = reg.span.Start;
				if (reg.span.End == start && start < pos && start > maxStart)
				{
					maxStart = reg.span.Start;
				}
			}

			if (maxStart != int.MinValue)
			{
				// Update the end position of the found region.
				var reg = _regions[maxStart];
				reg.span = new Span(reg.span.Start, pos);
			}
		}

		public override IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				foreach (var reg in base.OutliningRegions) yield return reg;

				foreach (var reg in _regions.Values)
				{
					switch (reg.type)
					{
						case RegionType.Comment:
							yield return new OutliningRegion
							{
								Span = reg.span,
								CollapseToDefinition = !reg.scope.Hint.HasFlag(ScopeHint.NotOnRoot),	// Auto-hide comments on the root
								Text = reg.title,
								TooltipText = GetRegionText(reg.span)
							};
							break;

						case RegionType.User:
							if (reg.span.End > reg.span.Start)
							{
								yield return new OutliningRegion
								{
									Span = reg.span,
									CollapseToDefinition = true,	// Auto-hide all regions
									Text = reg.title,
									TooltipText = GetRegionText(reg.span)
								};
							}
							break;
					}
				}

				var disabledSections = _model.DisabledSections;
				if (disabledSections != null)
				{
					foreach (var section in disabledSections)
					{
						var span = new Span(section.Start, section.End);
						yield return new OutliningRegion
						{
							Span = span,
							CollapseToDefinition = true,
							Text = Constants.DefaultOutliningText,
							TooltipText = GetRegionText(span)
						};
					}
				}
			}
		}
		#endregion
	}
}
