﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class StatementToken : GroupToken
	{
		private StatementToken(Scope scope)
			: base(scope)
		{
		}

		public delegate void StatementParseCallback(Token token);

		public static StatementToken TryParse(Scope scope, StatementParseCallback callback = null)
		{
			var code = scope.Code;
			if (!code.SkipWhiteSpace()) return null;

			var ret = new StatementToken(scope);

			if (!code.Peek()) return null;

			switch (code.Type)
			{
				case TokenParser.TokenType.Word:
					switch (code.Text)
					{
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
						case "if":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = IfStatementToken.Parse(scope, keywordToken);
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
								var token = SwitchToken.Parse(scope, keywordToken);
								ret.AddToken(token);
								if (callback != null) callback(token);
								return ret;
							}
						case "while":
							{
								var keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
								var token = WhileStatementToken.Parse(scope, keywordToken);
								if (callback != null) callback(token);
								return ret;
							}
					}
					break;

				case TokenParser.TokenType.Preprocessor:
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

				case TokenParser.TokenType.Operator:
					switch (code.Text)
					{
						case "{":
							{
								// Start of a 'scope'. This is not allowed in PROBE/WBDK but allow it here anyway.
								var token = BracesToken.Parse(scope);
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
