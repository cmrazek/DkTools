using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens.Operators;
using DkTools.CodeModel.Tokens.Statements;

namespace DkTools.CodeModel.Tokens
{
	class StatementToken : GroupToken
	{
		private StatementToken(Scope scope)
			: base(scope)
		{
		}

		public delegate void StatementParseCallback(Token token);

		public static bool IsStatementBreakingWord(Scope scope, string word)
		{
			if ((scope.Hint & ScopeHint.SuppressStatementStarts) != 0) return false;

			switch (word)
			{
				case "alter":
				case "create":
				case "col":
				case "colff":
				case "extern":
				case "extract":
				case "for":
				case "format":
				case "header":
				case "if":
				case "page":
				case "return":
				case "row":
				case "select":
				case "switch":
				case "while":
					return true;
				case "continue":
					return scope.ContinueOwner != null;
				case "break":
					return scope.BreakOwner != null;
				case "private":
				case "protected":
				case "public":
					return scope.Model.FileContext.IsClass();
				default:
					return false;
			}
		}

		public static StatementToken TryParse(Scope scope, StatementParseCallback callback = null)
		{
			var code = scope.Code;
			if (!code.SkipWhiteSpace()) return null;

			var ret = new StatementToken(scope);

			if (!code.Peek()) return null;

			switch (code.Type)
			{
				case CodeType.Word:
					switch (code.Text)
					{
						case "alter":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = CreateStatement.ParseAlter(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "break":
							if (scope.BreakOwner != null)
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var breakToken = BreakStatement.Parse(scope, keywordToken);
								ret.AddToken(breakToken);
								if (callback != null) callback(breakToken);
								scope.BreakOwner.OnBreakAttached(breakToken);
								return ret;
							}
							break;
						case "create":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = CreateStatement.ParseCreate(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "center":
						case "col":
						case "row":
						case "colff":
						case "page":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = RowColStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "continue":
							if (scope.ContinueOwner != null)
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var continueToken = ContinueStatement.Parse(scope, keywordToken);
								ret.AddToken(continueToken);
								if (callback != null) callback(continueToken);
								scope.ContinueOwner.OnContinueAttached(continueToken);
								return ret;
							}
							break;
						case "extern":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = ExternStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "extract":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = ExtractStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "footer":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = FooterStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "for":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = ForStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "format":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = FormatStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "header":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = HeaderStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "if":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = IfStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "public":
						case "private":
						case "protected":
							if (scope.Model.FileContext.IsClass())
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								ret.AddToken(keywordToken);
								if (callback != null) callback(keywordToken);
								return ret;
							}
							else break;
						case "return":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = ReturnStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "select":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = SelectStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "switch":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = SwitchStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "while":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = WhileStatement.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
					}
					break;

				case CodeType.Preprocessor:
					switch (code.Text)
					{
						case "#insert":
							{
								var token = InsertToken.Parse(scope, new InsertStartToken(scope, code.MovePeekedSpan()));
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "#endinsert":
							{
								var token = new InsertEndToken(scope, code.MovePeekedSpan());
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "#replace":
							{
								var token = ReplaceToken.Parse(scope, new ReplaceStartToken(scope, code.MovePeekedSpan()));
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "#with":
							{
								var token = new ReplaceWithToken(scope, code.MovePeekedSpan());
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "#endreplace":
							{
								var token = new ReplaceEndToken(scope, code.MovePeekedSpan());
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "#include":
							{
								var token = IncludeToken.Parse(scope, new PreprocessorToken(scope, code.MovePeekedSpan(), code.Text));
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						default:
							{
								var token = new PreprocessorToken(scope, code.MovePeekedSpan(), code.Text);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
					}

				case CodeType.Operator:
					switch (code.Text)
					{
						case "{":
							{
								// Start of a 'scope'. This is not allowed in PROBE/WBDK but allow it here anyway.
								var token = BracesToken.Parse(scope, null);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "}":
							{
								// Unmatched '}'. This is a syntax error, but since it's a statement breaking token, add it here and end the statement.
								var token = new OperatorToken(scope, code.MovePeekedSpan(), "}");
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
					}
					break;
			}

			var exp = ExpressionToken.TryParse(scope, null);
			if (exp != null)
			{
				ret.AddToken(exp);
				if (callback != null) callback(exp);

				code.SkipWhiteSpace();
			}

			if (code.ReadExact(';'))
			{
				// Empty statement. This is not allowed in PROBE/WBDK, but allow it here anyway.
				var token = new StatementEndToken(scope, code.Span);
				ret.AddToken(token);
				if (callback != null) callback(token);
				return ret;
			}

			return ret;
		}
	}
}
