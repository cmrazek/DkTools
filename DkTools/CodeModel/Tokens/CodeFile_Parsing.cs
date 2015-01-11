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
	internal sealed partial class CodeFile : GroupToken
	{
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
			_pos = 0;
			_length = _src.Length;
			_fileName = fileName;
			_parentFiles = parentFiles.ToArray();

			FunctionFileScanning.FFUtil.FileNameIsClass(_fileName, out _className);

			var scope = new Scope(this, 0, ScopeHint.None, visible, _model.DefinitionProvider);
			scope.ClassName = _className;
			Scope = scope;

			ParseScope(scope, t => ParseScopeResult.Continue);

			Span = new Span(0, Position);
		}

		public Token ParseComplexToken(GroupToken parent, Scope scope)
		{
			if (!SkipWhiteSpaceAndComments(scope)) return null;
			var ch = _src[_pos];

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

		private Token ParseTokenFromDefinitions(GroupToken parent, Scope scope, Span span, string word, Definition[] defs)
		{
			// Tokens that shouldn't be mixed up with other types of definitions.
			foreach (var def in defs)
			{
				if (def is VariableDefinition)
				{
					var varDef = def as VariableDefinition;
					if (varDef.DataType.HasMethodsOrProperties)
					{
						SkipWhiteSpaceAndComments(scope);
						if (PeekChar() == '.')
						{
							// This will be handled below when processing the combination tokens.
							continue;
						}
					}

					return new VariableToken(parent, scope, span, word, varDef);
				}
				if (def is FunctionDefinition)
				{
					var argsToken = BracketsToken.TryParse(parent, scope);
					if (argsToken != null)
					{
						var funcDef = def as FunctionDefinition;
						if (string.Equals(funcDef.SourceFileName, _fileName, StringComparison.OrdinalIgnoreCase) &&
							funcDef.ArgsStartPosition == argsToken.Span.Start)
						{
							var nameToken = new IdentifierToken(parent, scope, span, word);
							return new FunctionPlaceholderToken(parent, scope, span, word, nameToken, argsToken, funcDef);
						}
						else
						{
							var nameToken = new IdentifierToken(parent, scope, span, word);
							return new FunctionCallToken(parent, scope, null, null, nameToken, argsToken, funcDef);
						}
					}
				}
				if (def is ConstantDefinition)
				{
					return new ConstantToken(parent, scope, span, word, def as ConstantDefinition);
				}
				if (def is DataTypeDefinition)
				{
					return new DataTypeToken(parent, scope, new IdentifierToken(parent, scope, span, word), (def as DataTypeDefinition).DataType, def as DataTypeDefinition);
				}
				if (def is MacroDefinition)
				{
					return new MacroCallToken(parent, scope, new IdentifierToken(parent, scope, span, word), def as MacroDefinition);
				}
				if (def is InterfaceTypeDefinition)
				{
					return new InterfaceTypeToken(parent, scope, span, def as InterfaceTypeDefinition);
				}
			}

			// Tables/classes/extracts may be mixed up due to the ability to use the same name1.
			var tokens = new List<Token>();
			SkipWhiteSpaceAndComments(scope);
			if (PeekChar() == '.')
			{
				var resetPos = Position;
				var dotSpan = MoveNextSpan(1);
				SkipWhiteSpaceAndComments(scope);

				var word2 = PeekWord();
				if (!string.IsNullOrEmpty(word2))
				{
					var word2Span = MoveNextSpan(word2.Length);

					if (defs.Any(d => d is TableDefinition))
					{
						var table = ProbeEnvironment.GetTable(word);
						if (table != null)
						{
							var field = table.GetField(word2);
							if (field != null)
							{
								var tableToken = new TableToken(parent, scope, span, word, table.BaseDefinition);
								var dotToken = new DotToken(parent, scope, dotSpan);
								var fieldToken = new TableFieldToken(parent, scope, word2Span, word2, field);
								return new TableAndFieldToken(parent, scope, tableToken, dotToken, fieldToken);
							}
						}
					}

					if (defs.Any(d => d is RelIndDefinition))
					{
						var relInd = ProbeEnvironment.GetRelInd(word);
						if (relInd != null)
						{
							var field = relInd.GetField(word2);
							if (field != null)
							{
								var relIndToken = new RelIndToken(parent, scope, span, word, relInd.Definition);
								var dotToken = new DotToken(parent, scope, dotSpan);
								var fieldToken = new RelIndFieldToken(parent, scope, word2Span, word2, field);
								return new RelIndAndFieldToken(parent, scope, relIndToken, dotToken, fieldToken);
							}
						}
					}

					if (defs.Any(d => d is ClassDefinition))
					{
						var ffClass = ProbeToolsPackage.Instance.FunctionFileScanner.GetClass(word);
						if (ffClass != null)
						{
							var funcDef = ffClass.GetFunctionDefinition(word2);
							if (funcDef != null)
							{
								SkipWhiteSpaceAndComments(scope);
								if (PeekChar() == '(')
								{
									var classToken = new ClassToken(parent, scope, span, word, ffClass.ClassDefinition);
									var dotToken = new DotToken(parent, scope, dotSpan);
									var nameToken = new IdentifierToken(parent, scope, word2Span, word2);
									var argsToken = BracketsToken.Parse(parent, scope);
									var funcToken = new FunctionCallToken(parent, scope, classToken, dotToken, nameToken, argsToken, funcDef);
									return new ClassAndFunctionToken(parent, scope, classToken, dotToken, funcToken, funcDef);
								}
							}
						}
					}

					if (defs.Any(d => d is ExtractTableDefinition))
					{
						var exDef = defs.FirstOrDefault(d => d is ExtractTableDefinition) as ExtractTableDefinition;
						if (exDef != null)
						{
							var fieldDef = exDef.GetField(word2);
							if (fieldDef != null)
							{
								var exToken = new ExtractTableToken(parent, scope, span, word, exDef);
								var dotToken = new DotToken(parent, scope, dotSpan);
								var fieldToken = new ExtractFieldToken(parent, scope, word2Span, word2, fieldDef);
								return new ExtractTableAndFieldToken(parent, scope, exToken, dotToken, fieldToken);
							}
						}
					}

					if (defs.Any(d => d is VariableDefinition))
					{
						var varDef = defs.FirstOrDefault(d => d is VariableDefinition) as VariableDefinition;
						if (varDef != null)
						{
							var dataType = varDef.DataType;
							var methodDef = dataType.GetMethods(word2).FirstOrDefault() as InterfaceMethodDefinition;
							if (methodDef != null)
							{
								SkipWhiteSpaceAndComments(scope);
								if (PeekChar() == '(')
								{
									var varToken = new VariableToken(parent, scope, span, word, varDef);
									var dotToken = new DotToken(parent, scope, dotSpan);
									var nameToken = new IdentifierToken(parent, scope, word2Span, word2);
									var argsToken = BracketsToken.Parse(parent, scope);
									return new InterfaceMethodCallToken(parent, scope, varToken, dotToken, nameToken, argsToken, methodDef);

									// TODO: There should be some attempt to match the proper method signature to this call.
									// but this only affects interfaces.
								}
							}

							var propDef = dataType.GetProperties(word2).FirstOrDefault() as InterfacePropertyDefinition;
							if (propDef != null)
							{
								var varToken = new VariableToken(parent, scope, span, word, varDef);
								var dotToken = new DotToken(parent, scope, dotSpan);
								var nameToken = new IdentifierToken(parent, scope, word2Span, word2);
								return new InterfacePropertyToken(parent, scope, varToken, dotToken, nameToken, propDef);
							}
						}
					}
				}

				Position = resetPos;
			}

			// If we got here, then there is no 'dot' or field name following the next word, so it will be just name1 by itself.

			// Table definition takes higher precedence, since it can contain prompt/comment info.
			var tableDef = defs.FirstOrDefault(d => d is TableDefinition);
			if (tableDef != null)
			{
				return new TableToken(parent, scope, span, word, tableDef as TableDefinition);
			}

			foreach (var def in defs)
			{
				if (def is ClassDefinition)
				{
					return new ClassToken(parent, scope, span, word, def as ClassDefinition);
				}
				if (def is ExtractTableDefinition)
				{
					return new ExtractTableToken(parent, scope, span, word, def as ExtractTableDefinition);
				}
				if (def is RelIndDefinition)
				{
					return new RelIndToken(parent, scope, span, word, def as RelIndDefinition);
				}
			}

			return null;
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
			if (word == "extract") return ExtractStatement.Parse(parent, scope, new KeywordToken(parent, scope, wordSpan, "extract"));
			if (word == "tag") return TagToken.Parse(parent, scope, new KeywordToken(parent, scope, wordSpan, word));
			if (word == "alter" || word == "ALTER") return AlterToken.Parse(parent, scope, new KeywordToken(parent, scope, wordSpan, word));

			if (Constants.Keywords.Contains(word))
			{
				return new KeywordToken(parent, scope, wordSpan, word);
			}

			if (Constants.DataTypeKeywords.Contains(word))
			{
				return new DataTypeKeywordToken(parent, scope, wordSpan, word);
			}

			if ((scope.Hint & ScopeHint.InsideAlter) != 0)
			{
				if (Constants.AlterKeywords.Contains(word)) return new KeywordToken(parent, scope, wordSpan, word);
			}

			var defs = scope.DefinitionProvider.GetAny(wordSpan.Start, word).ToArray();
			if (defs.Length > 0)
			{
				var token = ParseTokenFromDefinitions(parent, scope, wordSpan, word, defs);
				if (token != null) return token;
			}

			return new IdentifierToken(parent, scope, wordSpan, word);
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
			var endLineCount = 0;

			while (_pos < _length)
			{
				var ch = _src[_pos];

				if (Char.IsWhiteSpace(ch))
				{
					// WhiteSpace
					endLineCount = 0;
					while (_pos < _length && Char.IsWhiteSpace(ch = _src[_pos]))
					{
						if (ch == '\n') endLineCount++;
						MoveNext();
					}
					if (_pos >= _length) break;
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
								// First comment found
								commentStartPos = Position;
								SeekEndOfLine();
								commentEndPos = Position;
								gotComment = true;
							}
							else if (endLineCount > 1)
							{
								// Blank lines between comments; don't combine those into a single region.
								AddCommentRegion(scope, new Span(commentStartPos, commentEndPos));
								commentStartPos = Position;
								SeekEndOfLine();
								commentEndPos = Position;
							}
							else
							{
								// Nth comment found
								SeekEndOfLine();
								commentEndPos = Position;
							}
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
						else if (endLineCount > 1)
						{
							// Blank lines between comments; don't combine those into a single region.
							AddCommentRegion(scope, new Span(commentStartPos, commentEndPos));
							commentStartPos = commentEndPos = Position;
						}

						MoveNext(2);
						var level = 1;

						while (_pos < _length)
						{
							ch = _src[_pos];
							if (ch == '*')
							{
								if (_pos + 1 < _length && _src[_pos + 1] == '/')
								{
									MoveNext(2);
									if (--level == 0) break;
								}
								else
								{
									MoveNext();
								}
							}
							else if (ch == '/')
							{
								if (_pos + 1 < _length && _src[_pos + 1] == '*')
								{
									MoveNext(2);
									level++;
								}
								else
								{
									MoveNext();
								}
							}
							else
							{
								MoveNext();
							}
						}

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
				if (ch == '\\')
				{
					ch = _src[_pos];
					MoveNext();	// Move past escaped char

					if (ch == '\r')
					{
						if (_pos < _length && _src[_pos] == '\n')
						{
							MoveNext();
						}
					}
				}
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

		public string PeekTagName()
		{
			var sb = new StringBuilder();

			var pos = _pos;
			var first = true;
			while (pos < _length && _src[pos].IsTagNameChar(first))
			{
				sb.Append(_src[pos++]);
				first = false;
			}

			return sb.ToString();
		}
	}
}
