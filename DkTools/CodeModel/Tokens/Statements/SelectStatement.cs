using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class SelectStatement : GroupToken, IBreakOwner, IContinueOwner
	{
		private SelectStatement(Scope scope, KeywordToken selectToken)
			: base(scope)
		{
			AddToken(selectToken);
		}

		private static readonly string[] _whereEndTokens = new string[] { "order" };

		public static SelectStatement Parse(Scope parentScope, KeywordToken selectToken)
		{
			var ret = new SelectStatement(parentScope, selectToken);
			var scope = parentScope.Clone();
			scope.BreakOwner = ret;
			scope.ContinueOwner = ret;

			var code = scope.Code;

			if (code.ReadStringLiteral()) ret.AddToken(new StringLiteralToken(scope, code.Span, code.Text));
			if (code.ReadExact('*')) ret.AddToken(new OperatorToken(scope, code.Span, "*"));

			if (!code.ReadExactWholeWord("from")) return ret;
			ret.AddToken(new KeywordToken(scope, code.Span, "from"));

			if (code.ReadWord())
			{
				var table = ProbeEnvironment.GetTable(code.Text);
				if (table != null) ret.AddToken(new TableToken(scope, code.Span, code.Text, table.BaseDefinition));
				else ret.AddToken(new UnknownToken(scope, code.Span, code.Text));
			}

			if (code.ReadExactWholeWord("of"))
			{
				ret.AddToken(new KeywordToken(scope, code.Span, "of"));

				if (code.ReadWord())
				{
					var table = ProbeEnvironment.GetTable(code.Text);
					if (table != null) ret.AddToken(new TableToken(scope, code.Span, code.Text, table.BaseDefinition));
					else ret.AddToken(new UnknownToken(scope, code.Span, code.Text));
				}
			}
			else if (code.ReadExact(','))
			{
				ret.AddToken(new DelimiterToken(scope, code.Span));

				var expectingComma = false;

				while (!code.EndOfFile)
				{
					if (code.PeekExact('{')) break;
					if (expectingComma)
					{
						if (code.ReadExact(','))
						{
							ret.AddToken(new DelimiterToken(scope, code.Span));
							expectingComma = false;
						}
						else break;
					}
					else if (code.ReadWord())
					{
						var table = ProbeEnvironment.GetTable(code.Text);
						if (table != null) ret.AddToken(new TableToken(scope, code.Span, code.Text, table.BaseDefinition));
						else ret.AddToken(new UnknownToken(scope, code.Span, code.Text));
						expectingComma = true;
					}
					else break;
				}
			}

			// WHILE and ORDER BY
			var gotWhile = false;
			var gotOrderBy = false;
			while (!code.EndOfFile)
			{
				if (code.PeekExact('{')) break;
				if (!gotWhile && code.ReadExactWholeWord("while"))
				{
					ret.AddToken(new KeywordToken(scope, code.Span, code.Text));
					gotWhile = true;

					var exp = ExpressionToken.TryParse(scope, _whereEndTokens);
					if (exp != null) ret.AddToken(exp);
					else break;
				}
				else if (!gotOrderBy && code.ReadExactWholeWord("order"))
				{
					ret.AddToken(new KeywordToken(scope, code.Span, code.Text));
					gotOrderBy = true;

					if (!code.ReadExactWholeWord("by")) break;
					ret.AddToken(new KeywordToken(scope, code.Span, code.Text));

					var expectingComma = false;

					while (!code.EndOfFile)
					{
						if (code.PeekExact('{')) break;
						if (expectingComma)
						{
							if (code.ReadExact(','))
							{
								ret.AddToken(new DelimiterToken(scope, code.Span));
								expectingComma = false;
							}
							else break;
						}
						else if (code.ReadWord())
						{
							var table = ProbeEnvironment.GetTable(code.Text);
							if (table != null)
							{
								var tableToken = new TableToken(scope, code.Span, code.Text, table.BaseDefinition);
								if (code.ReadExact('.'))
								{
									var dotToken = new DotToken(scope, code.Span);
									if (code.ReadWord())
									{
										var field = table.GetField(code.Text);
										if (field != null)
										{
											var fieldName = code.Text;
											var fieldSpan = code.Span;

											var fieldToken = new TableFieldToken(scope, fieldSpan, fieldName, field);
											ret.AddToken(new TableAndFieldToken(scope, tableToken, dotToken, fieldToken));
										}
										else ret.AddToken(new CompositeToken(scope, null, tableToken, dotToken, new UnknownToken(scope, code.Span, code.Text)));
									}
									else ret.AddToken(new CompositeToken(scope, null, tableToken, dotToken));
								}
								else ret.AddToken(tableToken);
							}
							else ret.AddToken(new UnknownToken(scope, code.Span, code.Text));

							if (code.ReadExactWholeWord("asc") || code.ReadExactWholeWord("desc")) ret.AddToken(new KeywordToken(scope, code.Span, code.Text));

							expectingComma = true;
						}
						else break;
					}
				}
				else break;
			}

			// Body
			if (code.ReadExact('{'))
			{
				var braces = new BracesToken(scope);
				braces.AddOpenBrace(code.Span);
				ret.AddToken(braces);

				while (!code.EndOfFile)
				{
					if (code.ReadExact('}'))
					{
						braces.AddCloseBrace(code.Span);
						break;
					}

					if (code.ReadExactWholeWord("for"))
					{
						braces.AddToken(new KeywordToken(scope, code.Span, "for"));

						if (!code.ReadExactWholeWord("each")) continue;
						braces.AddToken(new KeywordToken(scope, code.Span, "each"));

						if (!code.ReadExact(':')) continue;
						braces.AddToken(new OperatorToken(scope, code.Span, ":"));
					}
					else if (code.ReadExactWholeWord("before") || code.ReadExactWholeWord("after"))
					{
						braces.AddToken(new KeywordToken(scope, code.Span, "before"));

						if (!code.ReadExactWholeWord("group")) continue;
						braces.AddToken(new KeywordToken(scope, code.Span, "group"));

						if (code.ReadExactWholeWord("all"))
						{
							braces.AddToken(new KeywordToken(scope, code.Span, "all"));

							if (!code.ReadExact(':')) continue;
							braces.AddToken(new OperatorToken(scope, code.Span, ":"));
						}
						else if (code.ReadWord())
						{
							var table = ProbeEnvironment.GetTable(code.Text);
							if (table != null)
							{
								var tableToken = new TableToken(scope, code.Span, code.Text, table.BaseDefinition);
								if (code.ReadExact('.'))
								{
									var dotToken = new DotToken(scope, code.Span);
									if (code.ReadWord())
									{
										var field = table.GetField(code.Text);
										if (field != null)
										{
											var fieldToken = new TableFieldToken(scope, code.Span, code.Text, field);
											braces.AddToken(new TableAndFieldToken(scope, tableToken, dotToken, fieldToken));
										}
										else braces.AddToken(new CompositeToken(scope, null, tableToken, dotToken, new UnknownToken(scope, code.Span, code.Text)));
									}
									else braces.AddToken(new CompositeToken(scope, null, tableToken, dotToken));
								}
								else braces.AddToken(tableToken);
							}
							else braces.AddToken(new UnknownToken(scope, code.Span, code.Text));
						}
					}
					else if (code.ReadExactWholeWord("default"))
					{
						braces.AddToken(new KeywordToken(scope, code.Span, "default"));

						if (!code.ReadExact(':')) continue;
						braces.AddToken(new OperatorToken(scope, code.Span, ":"));
					}
					else
					{
						var stmt = StatementToken.TryParse(scope);
						if (stmt != null) braces.AddToken(stmt);
					}
				}
			}

			return ret;
		}

		public void OnBreakAttached(BreakStatement breakToken)
		{
		}

		public void OnContinueAttached(ContinueStatement continueToken)
		{
		}
	}
}
