using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

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

			ExtractTableDefinition extractDef = null;
			DkDict.Table table = null;

			if (code.ReadWord())
			{
				if ((table = DkDict.Dict.GetTable(code.Text)) != null) ret.AddToken(new TableToken(scope, code.Span, code.Text, table.Definition));
				else if ((extractDef = scope.DefinitionProvider.GetAny<ExtractTableDefinition>(code.TokenStartPostion, code.Text).FirstOrDefault()) != null)
					ret.AddToken(new IdentifierToken(scope, code.Span, code.Text, extractDef));
				else ret.AddToken(new UnknownToken(scope, code.Span, code.Text));
			}

			if (code.ReadExactWholeWord("of"))
			{
				ret.AddToken(new KeywordToken(scope, code.Span, "of"));

				if (code.ReadWord())
				{
					if ((table = DkDict.Dict.GetTable(code.Text)) != null) ret.AddToken(new TableToken(scope, code.Span, code.Text, table.Definition));
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
						if ((table = DkDict.Dict.GetTable(code.Text)) != null) ret.AddToken(new TableToken(scope, code.Span, code.Text, table.Definition));
						else ret.AddToken(new UnknownToken(scope, code.Span, code.Text));
						expectingComma = true;
					}
					else break;
				}
			}

			// WHERE and ORDER BY
			var gotWhere = false;
			var gotOrderBy = false;

			while (!code.EndOfFile)
			{
				if (code.PeekExact('{')) break;
				if (!gotWhere && code.ReadExactWholeWord("where"))
				{
					ret.AddToken(new KeywordToken(scope, code.Span, code.Text));
					gotWhere = true;

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

					while (!code.EndOfFile)
					{
						if (code.PeekExact('{')) break;

						if (code.ReadExact(','))
						{
							ret.AddToken(new DelimiterToken(scope, code.Span));
							continue;
						}

						if (code.ReadExactWholeWord("asc") || code.ReadExactWholeWord("desc"))
						{
							ret.AddToken(new KeywordToken(scope, code.Span, code.Text));
							continue;
						}

						if (TryParseColumn(scope, ret, true, extractDef))
						{
							continue;
						}

						break;
					}
				}
				else break;
			}

			// Body
			if (code.ReadExact('{'))
			{
				var braces = new BracesToken(scope);
				braces.AddOpen(code.Span);
				ret.AddToken(braces);

				while (!code.EndOfFile)
				{
					if (code.ReadExact('}'))
					{
						braces.AddClose(code.Span);
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
						braces.AddToken(new KeywordToken(scope, code.Span, code.Text));

						if (!code.ReadExactWholeWord("group")) continue;
						braces.AddToken(new KeywordToken(scope, code.Span, "group"));

						if (code.ReadExactWholeWord("all"))
						{
							braces.AddToken(new KeywordToken(scope, code.Span, "all"));

							if (!code.ReadExact(':')) continue;
							braces.AddToken(new OperatorToken(scope, code.Span, ":"));
						}
						else if (TryParseColumn(scope, braces, false, extractDef))
						{
							if (!code.ReadExact(':')) continue;
							braces.AddToken(new OperatorToken(scope, code.Span, ":"));
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

		private static bool TryParseColumn(Scope scope, GroupToken parent, bool allowRelInd, ExtractTableDefinition extractDef)
		{
			var code = scope.Code;
			if (code.ReadWord())
			{
				var wordPos = code.TokenStartPostion;

				var table = DkDict.Dict.GetTable(code.Text);
				if (table != null)
				{
					var tableToken = new TableToken(scope, code.Span, code.Text, table.Definition);
					if (code.ReadExact('.'))
					{
						var dotToken = new DotToken(scope, code.Span);
						if (code.ReadWord())
						{
							var field = table.GetColumn(code.Text);
							if (field != null)
							{
								var fieldName = code.Text;
								var fieldSpan = code.Span;

								var fieldToken = new TableFieldToken(scope, fieldSpan, fieldName, field);
								parent.AddToken(new TableAndFieldToken(scope, tableToken, dotToken, fieldToken));
							}
							else parent.AddToken(new CompositeToken(scope, null, tableToken, dotToken, new UnknownToken(scope, code.Span, code.Text)));
						}
						else parent.AddToken(new CompositeToken(scope, null, tableToken, dotToken));
					}
					else parent.AddToken(tableToken);
					return true;
				}

				if (allowRelInd)
				{
					var relind = DkDict.Dict.GetRelInd(code.Text);
					if (relind != null)
					{
						parent.AddToken(new RelIndToken(scope, code.Span, code.Text, relind.Definition));
						return true;
					}
				}

				if (extractDef != null && code.Text == extractDef.Name)
				{
					parent.AddToken(new IdentifierToken(scope, code.Span, code.Text, extractDef));
					if (code.ReadExact('.'))
					{
						parent.AddToken(new DotToken(scope, code.Span));
						var word = code.PeekWordR();
						if (!string.IsNullOrEmpty(word))
						{
							var childDef = extractDef.GetChildDefinition(word);
							if (childDef != null)
							{
								parent.AddToken(new IdentifierToken(scope, code.MovePeekedSpan(), word, childDef));
							}
						}
					}
					return true;
				}

				// Word was not recognized, so set parser back to before the word so it can get picked up later.
				code.Position = wordPos;
			}

			return false;
		}

		public void OnBreakAttached(BreakStatement breakToken)
		{
		}

		public void OnContinueAttached(ContinueStatement continueToken)
		{
		}
	}
}
