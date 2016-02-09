using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens.Statements
{
	class CreateStatement : GroupToken
	{
		private CreateStatement(Scope scope)
			: base(scope)
		{
		}

		public static CreateStatement Parse(Scope scope, KeywordToken createToken)
		{
			scope = scope.Clone();
			scope.Hint |= ScopeHint.SuppressStatementStarts | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressFunctionCall | ScopeHint.SuppressLogic;

			var ret = new CreateStatement(scope);
			ret.AddToken(createToken);

			var code = scope.Code;

			var word = code.PeekWord();
			if (word == "table")
			{
				ret.ParseCreateTable(new KeywordToken(scope, code.MovePeekedSpan(), code.Text));
			}

			return ret;
		}

		private static readonly string[] _createTableEndTokens = new string[] { "(", "updates", "database", "display", "modal",
			"nopick", "pick", "snapshot", "prompt", "comment", "image", "description", "tag" };

		private static readonly string[] _columnEndTokens = new string[] { ")", "}", ",", "prompt", "comment", "group", "endgroup",
			"tag", "form", "formonly", "zoom", "row", "col", "rows", "cols" };

		private void ParseCreateTable(KeywordToken tableToken)
		{
			AddToken(tableToken);

			var code = Code;

			// Table name
			if (!code.ReadWord()) return;
			var table = ProbeEnvironment.GetTable(code.Text);
			if (table != null) AddToken(new IdentifierToken(Scope, code.Span, code.Text, table.BaseDefinition));
			else AddToken(new UnknownToken(Scope, code.Span, code.Text));

			// Table number
			if (!code.ReadNumber()) return;
			AddToken(new NumberToken(Scope, code.Span, code.Text));

			// Table number+1
			if (Code.ReadNumber()) AddToken(new NumberToken(Scope, code.Span, code.Text));

			string word;
			ExpressionToken exp;

			// Attributes
			while (true)
			{
				if (code.PeekExact('(') || code.PeekExact('{')) break;
				if (!code.Peek()) break;

				if (!string.IsNullOrEmpty(word = code.PeekWord()))
				{
					if (word == "updates" || word == "display" || word == "modal" || word == "nopick" || word == "pick")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), code.Text));
						continue;
					}

					if (word == "database" || word == "snapshot" || word == "prompt" || word == "comment" || word == "image" ||
						word == "description" || word == "updates")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), code.Text));
						if ((exp = ExpressionToken.TryParse(Scope, _createTableEndTokens)) != null) AddToken(exp);
						continue;
					}

					if (word == "tag")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), code.Text));

						var resetPos = code.Position;
						if (code.ReadTagName() && ProbeEnvironment.IsValidTagName(code.Text))
						{
							AddToken(new KeywordToken(Scope, code.Span, code.Text));
							if (code.ReadStringLiteral())
							{
								AddToken(new StringLiteralToken(Scope, code.Span, code.Text));
								continue;
							}
						}
						else
						{
							code.Position = resetPos;
						}

						if ((exp = ExpressionToken.TryParse(Scope, _createTableEndTokens)) != null) AddToken(exp);
						continue;
					}

					break;
				}
				else break;
			}

			BracketsToken brackets = null;
			BracesToken braces = null;
			GroupToken parent = null;
			if (code.ReadExact('('))
			{
				brackets = new BracketsToken(Scope);
				brackets.AddOpen(code.Span);
				AddToken(brackets);
				parent = brackets;
			}
			else if (code.ReadExact('{'))
			{
				braces = new BracesToken(Scope);
				braces.AddOpen(code.Span);
				AddToken(braces);
				parent = braces;
			}
			else return;

			// Columns
			while (!code.EndOfFile)
			{
				if (code.ReadExact(')') || code.ReadExact('}'))
				{
					if (brackets != null) brackets.AddClose(code.Span);
					else if (braces != null) braces.AddClose(code.Span);
					return;
				}

				if (code.ReadExact(','))
				{
					parent.AddToken(new DelimiterToken(Scope, code.Span));
					continue;
				}

				// Column name
				if (table != null)
				{
					if (!string.IsNullOrEmpty(word = code.PeekWord()))
					{
						var field = table.GetField(word);
						if (field != null)
						{
							parent.AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), code.Text, field.Definition));
						}
					}
				}

				// Data type
				if ((exp = ExpressionToken.TryParse(Scope, _columnEndTokens)) != null) parent.AddToken(exp);

				// Column attributes
				while (!code.EndOfFile)
				{
					if (code.PeekExact(')') || code.PeekExact('}')) break;

					if (!string.IsNullOrEmpty(word = code.PeekWord()))
					{
						if (word == "prompt" || word == "comment" || word == "group" || word == "row" || word == "col" ||
							word == "rows" || word == "cols")
						{
							parent.AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), code.Text));
							if ((exp = ExpressionToken.TryParse(Scope, _columnEndTokens)) != null) parent.AddToken(exp);
							continue;
						}

						if (word == "endgroup" || word == "form" || word == "formonly" || word == "zoom")
						{
							parent.AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), code.Text));
							continue;
						}

						if (word == "tag")
						{
							parent.AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), code.Text));

							var resetPos = code.Position;
							if (code.ReadTagName() && ProbeEnvironment.IsValidTagName(code.Text))
							{
								parent.AddToken(new KeywordToken(Scope, code.Span, code.Text));
								if (code.ReadStringLiteral())
								{
									parent.AddToken(new StringLiteralToken(Scope, code.Span, code.Text));
									continue;
								}
							}
							else
							{
								code.Position = resetPos;
							}

							if ((exp = ExpressionToken.TryParse(Scope, _columnEndTokens)) != null) parent.AddToken(exp);
							continue;
						}

						break;
					}
					else break;
				}
			}
		}

	}
}
