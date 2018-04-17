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

		public static CreateStatement ParseCreate(Scope scope, KeywordToken createToken)
		{
			scope = scope.Clone();
			scope.Hint |= ScopeHint.SuppressStatementStarts | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressFunctionCall | ScopeHint.SuppressLogic;

			var ret = new CreateStatement(scope);
			ret.AddToken(createToken);

			var code = scope.Code;

			var word = code.PeekWordR();
			if (word == "table")
			{
				ret.ParseCreateTable(new KeywordToken(scope, code.MovePeekedSpan(), "table"));
			}
			else if (word == "relationship")
			{
				ret.ParseCreateRelationship(new KeywordToken(scope, code.MovePeekedSpan(), "relationship"));
			}
			else if (word == "index")
			{
				ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "index"));
				ret.ParseCreateIndex();
			}
			else if (word == "unique")
			{
				ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "unique"));
				switch (code.PeekWordR())
				{
					case "index":
						ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "index"));
						ret.ParseCreateIndex();
						break;

					case "primary":
					case "nopick":
					case "NOPICK":
						ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), word));
						if (code.PeekWordR() == "index")
						{
							ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "index"));
							ret.ParseCreateIndex();
						}
						break;
				}
			}
			else if (word == "primary" || word == "NOPICK" || word == "nopick")
			{
				ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), word));
				if (code.PeekWordR() == "index")
				{
					ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "index"));
					ret.ParseCreateIndex();
				}
			}
			else if (word == "stringdef")
			{
				ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "stringdef"));
				ret.ParseCreateStringdef();
			}
			else if (word == "typedef")
			{
				ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "typedef"));
				ret.ParseCreateTypedef();
			}
			else if (word == "time")
			{
				ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "time"));
				if (code.ReadExactWholeWord("relationship"))
				{
					ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "relationship"));
					ret.ParseCreateTimeRelationship();
				}
			}
			else if (word == "workspace")
			{
				ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "workspace"));
				ret.ParseCreateWorkspace();
			}

			return ret;
		}

		public static CreateStatement ParseAlter(Scope scope, KeywordToken alterToken)
		{
			scope = scope.Clone();
			scope.Hint |= ScopeHint.SuppressStatementStarts | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressFunctionCall | ScopeHint.SuppressLogic;

			var ret = new CreateStatement(scope);
			ret.AddToken(alterToken);

			var code = scope.Code;

			var word = code.PeekWordR();
			if (word == "table")
			{
				ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), "table"));
				ret.ParseAlterTable();
			}

			return ret;
		}

		private static readonly string[] _createTableEndTokens = new string[] { "(", "updates", "database", "display", "modal",
			"nopick", "pick", "snapshot", "prompt", "comment", "image", "description", "tag" };

		private void ParseCreateTable(KeywordToken tableToken)
		{
			AddToken(tableToken);

			var code = Code;

			// Table name
			if (!code.ReadWord()) return;
			var table = DkDict.Dict.GetTable(code.Text);
			if (table != null) AddToken(new IdentifierToken(Scope, code.Span, code.Text, table.Definition));
			else AddToken(new UnknownToken(Scope, code.Span, code.Text));

			// Table number
			if (!code.ReadNumber()) return;
			AddToken(new NumberToken(Scope, code.Span, code.Text));

			// Table number+1
			if (Code.ReadNumber()) AddToken(new NumberToken(Scope, code.Span, code.Text));

			ExpressionToken exp;

			// Attributes
			ParseTableAttributes(_createTableEndTokens);

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

				if (!TryParseColumnDefinition(Scope, parent, table != null ? table.Definition : null, true))
				{
					if ((exp = ExpressionToken.TryParse(Scope, _columnEndTokens)) != null) parent.AddToken(exp);
					else break;
				}
			}
		}

		private void ParseTableAttributes(string[] endTokens)
		{
			var code = Code;
			string word;
			ExpressionToken exp;

			while (true)
			{
				if (code.PeekExact('(') || code.PeekExact('{')) break;
				if (!code.Peek()) break;

				if (!string.IsNullOrEmpty(word = code.PeekWordR()))
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
						if ((exp = ExpressionToken.TryParse(Scope, endTokens)) != null) AddToken(exp);
						continue;
					}

					if (word == "tag")
					{
						ParseTag(Scope, this, new KeywordToken(Scope, code.MovePeekedSpan(), "tag"), endTokens);
						continue;
					}

					if ((exp = ExpressionToken.TryParse(Scope, endTokens)) != null) AddToken(exp);
					else break;
				}
				else
				{
					if ((exp = ExpressionToken.TryParse(Scope, endTokens)) != null) AddToken(exp);
					else break;
				}
			}
		}

		private static readonly string[] _alterTableEndTokens = new string[] { "before", "after", "column",
			"add", "alter", "drop", "move", "samtype", "updates", "database", "display", "modal",
			"nopick", "pick", "snapshot", "prompt", "comment", "image", "description", "tag" };

		private void ParseAlterTable()
		{
			var code = Code;
			DkDict.Table table = null;
			ExpressionToken exp;
			
			var word = code.PeekWordR();
			if (!string.IsNullOrEmpty(word) && (table = DkDict.Dict.GetTable(word)) != null)
			{
				AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, table.Definition));
			}
			else
			{
				if ((exp = ExpressionToken.TryParse(Scope, _alterTableEndTokens)) != null) AddToken(exp);
			}

			ParseTableAttributes(_alterTableEndTokens);

			if (code.ReadExact(';'))
			{
				AddToken(new StatementEndToken(Scope, code.Span));
				return;
			}

			if (code.ReadExactWholeWord("before") ||
				code.ReadExactWholeWord("after"))
			{
				AddToken(new KeywordToken(Scope, code.Span, code.Text));
			}

			if (code.ReadExactWholeWord("column")) AddToken(new KeywordToken(Scope, code.Span, code.Text));

			if (table != null)
			{
				var field = table.GetColumn(code.PeekWordR());
				if (field != null)
				{
					AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), code.Text, field.Definition));
				}
				else
				{
					if ((exp = ExpressionToken.TryParse(Scope, _alterTableEndTokens)) != null) AddToken(exp);
				}
			}

			if (code.ReadExactWholeWord("add") ||
				code.ReadExactWholeWord("alter") ||
				code.ReadExactWholeWord("drop") ||
				code.ReadExactWholeWord("move"))
			{
				AddToken(new KeywordToken(Scope, code.Span, code.Text));
			}

			if (code.ReadExactWholeWord("column")) AddToken(new KeywordToken(Scope, code.Span, code.Text));

			if (code.ReadExactWholeWord("sametype"))
			{
				AddToken(new KeywordToken(Scope, code.Span, code.Text));
			}
			else
			{
				var dataType = DataType.TryParse(new DataType.ParseArgs
				{
					Code = code,
					Scope = Scope,
					TokenCreateCallback = token =>
					{
						AddToken(token);
					},
					VisibleModel = true,
					AllowTags = true
				});
			}

			TryParseColumnDefinition(Scope, this, table != null ? table.Definition : null, false);

			if (code.ReadExact(';')) AddToken(new StatementEndToken(Scope, code.Span));
		}

		private static readonly string[] _columnEndTokens = new string[] { ")", "}", ",", "prompt", "comment", "group", "endgroup",
			"tag", "form", "formonly", "zoom", "row", "col", "rows", "cols", "image", "tool" };

		private static bool TryParseColumnDefinition(Scope scope, GroupToken parent, Definition parentDef, bool includeNameAndDataType)
		{
			string word;
			var code = scope.Code;
			ExpressionToken exp;

			if (includeNameAndDataType)
			{
				// Column name
				if (parentDef != null && parentDef.AllowsChild)
				{
					if (!string.IsNullOrEmpty(word = code.PeekWordR()))
					{
						var childDef = parentDef.GetChildDefinitions(word).FirstOrDefault();
						if (childDef != null)
						{
							parent.AddToken(new IdentifierToken(scope, code.MovePeekedSpan(), code.Text, childDef));
						}
					}
					else return false;
				}
				else return false;

				// Data type
				if ((exp = ExpressionToken.TryParse(scope, _columnEndTokens)) != null) parent.AddToken(exp);
			}

			// Column attributes
			while (!code.EndOfFile)
			{
				if (code.PeekExact(')') || code.PeekExact('}') || code.PeekExact(',')) break;

				if (!string.IsNullOrEmpty(word = code.PeekWordR()))
				{
					if (word == "prompt" || word == "comment" || word == "group" || word == "row" || word == "col" ||
						word == "rows" || word == "cols" || word == "image")
					{
						parent.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), code.Text));
						if ((exp = ExpressionToken.TryParse(scope, _columnEndTokens)) != null) parent.AddToken(exp);
						continue;
					}

					if (word == "endgroup" || word == "form" || word == "formonly" || word == "zoom" || word == "tool")
					{
						parent.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), code.Text));
						continue;
					}

					if (word == "tag")
					{
						ParseTag(scope, parent, new KeywordToken(scope, code.MovePeekedSpan(), "tag"), _columnEndTokens);
						continue;
					}

					if ((exp = ExpressionToken.TryParse(scope, _columnEndTokens)) != null) parent.AddToken(exp);
					else break;
				}
				else
				{
					if ((exp = ExpressionToken.TryParse(scope, _columnEndTokens)) != null) parent.AddToken(exp);
					else break;
				}
			}

			if (code.ReadExact(','))
			{
				parent.AddToken(new DelimiterToken(scope, code.Span));
			}

			return true;
		}

		private static void ParseTag(Scope scope, GroupToken parent, KeywordToken tagToken, string[] endTokens)
		{
			var code = scope.Code;

			parent.AddToken(tagToken);

			var resetPos = code.Position;
			if (code.ReadTagName() && ProbeEnvironment.IsValidTagName(code.Text))
			{
				parent.AddToken(new KeywordToken(scope, code.Span, code.Text));
				if (code.ReadStringLiteral())
				{
					parent.AddToken(new StringLiteralToken(scope, code.Span, code.Text));
					return;
				}
			}
			else
			{
				code.Position = resetPos;
			}

			var exp = ExpressionToken.TryParse(scope, endTokens);
			if (exp != null) parent.AddToken(exp);
		}

		private static readonly string[] _createRelationshipEndTokens = new string[] { "(", ")", "updates", "prompt", "comment",
			"image", "description", "one", "many", "to", "order", "tag" };

		private void ParseCreateRelationship(KeywordToken relationshipToken)
		{
			AddToken(relationshipToken);

			var code = Code;
			var word = code.PeekWordR();
			var relind = DkDict.Dict.GetRelInd(word);
			if (relind != null)
			{
				AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, relind.Definition));

				if (code.ReadNumber()) AddToken(new NumberToken(Scope, code.Span, code.Text));
			}
			else
			{
				var exp = ExpressionToken.TryParse(Scope, _createRelationshipEndTokens);
				if (exp != null) AddToken(exp);
			}

			DkDict.Table table = null;

			var done = false;

			while (!code.EndOfFile && !done)
			{
				if (!string.IsNullOrEmpty(word = code.PeekWordR()))
				{
					if (word == "updates")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), word));
					}
					else if (word == "prompt" || word == "comment" || word == "image" || word == "description")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), word));
						var exp = ExpressionToken.TryParse(Scope, _createRelationshipEndTokens);
						if (exp != null) AddToken(exp);
					}
					else if (word == "one" || word == "many")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), word));
						word = code.PeekWordR();
						if (!string.IsNullOrEmpty(word))
						{
							table = DkDict.Dict.GetTable(word);
							if (table != null)
							{
								AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, table.Definition));
							}
							else
							{
								var exp = ExpressionToken.TryParse(Scope, _createRelationshipEndTokens);
								if (exp != null) AddToken(exp);
							}
						}
						else
						{
							var exp = ExpressionToken.TryParse(Scope, _createRelationshipEndTokens);
							if (exp != null) AddToken(exp);
						}
					}
					else if (word == "to")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), "to"));
						if (code.ReadExactWholeWord("one") || code.ReadExactWholeWord("many"))
						{
							AddToken(new KeywordToken(Scope, code.Span, code.Text));

							word = code.PeekWordR();
							if (!string.IsNullOrEmpty(word))
							{
								table = DkDict.Dict.GetTable(word);
								if (table != null)
								{
									AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, table.Definition));
								}
								else
								{
									var exp = ExpressionToken.TryParse(Scope, _createRelationshipEndTokens);
									if (exp != null) AddToken(exp);
								}
							}
							else
							{
								var exp = ExpressionToken.TryParse(Scope, _createRelationshipEndTokens);
								if (exp != null) AddToken(exp);
							}
						}
					}
					else if (word == "order")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), "order"));

						if (code.ReadExactWholeWord("by"))
						{
							AddToken(new KeywordToken(Scope, code.Span, "by"));

							if (code.ReadExactWholeWord("unique")) AddToken(new KeywordToken(Scope, code.Span, "unique"));

							while (!code.EndOfFile)
							{
								if (code.PeekExact('(') || code.PeekExact('{')) break;

								if (table != null && !string.IsNullOrEmpty(word = code.PeekWordR()))
								{
									var field = table.GetColumn(word);
									if (field != null) AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, field.Definition));
									else break;
								}
								else break;
							}
						}
					}
					else if (word == "tag")
					{
						ParseTag(Scope, this, new KeywordToken(Scope, code.MovePeekedSpan(), "tag"), _createRelationshipEndTokens);
					}
					else
					{
						var exp = ExpressionToken.TryParse(Scope, _createRelationshipEndTokens);
						if (exp != null) AddToken(exp);
						else break;
					}
				}
				else if (code.ReadExact('(') || code.ReadExact('{'))
				{
					var brackets = new BracketsToken(Scope);
					brackets.AddOpen(code.Span);
					AddToken(brackets);

					while (!code.EndOfFile)
					{
						if (code.ReadExact(')') || code.ReadExact('}'))
						{
							brackets.AddClose(code.Span);
							done = true;
							break;
						}

						if (!TryParseColumnDefinition(Scope, brackets, relind != null ? relind.Definition : null, true))
						{
							var exp = ExpressionToken.TryParse(Scope, _createRelationshipEndTokens);
							if (exp != null) AddToken(exp);
							else break;
						}
					}
				}
				else
				{
					var exp = ExpressionToken.TryParse(Scope, _createRelationshipEndTokens);
					if (exp != null) AddToken(exp);
					else break;
				}
			}
		}

		private static readonly string[] _createIndexEndTokens = new string[] { "on", "description", "tag" };

		private static readonly string[] _createIndexColumnEndTokens = new string[] { ")", "}", "," };

		private void ParseCreateIndex()
		{
			var code = Code;
			DkDict.RelInd relind = null;
			DkDict.Table table = null;
			string word;

			if (code.ReadExactWholeWord("primary")) AddToken(new KeywordToken(Scope, code.Span, "primary"));
			if (code.ReadExactWholeWord("nopick")) AddToken(new KeywordToken(Scope, code.Span, "nopick"));
			if (code.ReadExactWholeWord("NOPICK")) AddToken(new KeywordToken(Scope, code.Span, "NOPICK"));

			if (!string.IsNullOrEmpty(word = code.PeekWordR()))
			{
				if ((relind = DkDict.Dict.GetRelInd(word)) != null)
				{
					AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, relind.Definition));
				}
				else
				{
					var exp = ExpressionToken.TryParse(Scope, _createIndexEndTokens);
					if (exp != null) AddToken(exp);
				}
			}
			else
			{
				var exp = ExpressionToken.TryParse(Scope, _createIndexEndTokens);
				if (exp != null) AddToken(exp);
			}

			if (code.ReadExactWholeWord("on"))
			{
				AddToken(new KeywordToken(Scope, code.Span, "on"));

				if (!string.IsNullOrEmpty(word = code.PeekWordR()) &&
					(table = DkDict.Dict.GetTable(word)) != null)
				{
					AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, table.Definition));
				}
				else
				{
					var exp = ExpressionToken.TryParse(Scope, _createIndexEndTokens);
					if (exp != null) AddToken(exp);
				}
			}

			while (!code.EndOfFile)
			{
				if (code.PeekExact('(') || code.PeekExact('{')) break;

				if (!string.IsNullOrEmpty(word = code.PeekWordR()))
				{
					if (word == "description")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), "description"));
						var exp = ExpressionToken.TryParse(Scope, _createIndexEndTokens);
						if (exp != null) AddToken(exp);
					}
					else if (word == "tag")
					{
						ParseTag(Scope, this, new KeywordToken(Scope, code.MovePeekedSpan(), "tag"), _createIndexEndTokens);
					}
					else
					{
						var exp = ExpressionToken.TryParse(Scope, _createIndexEndTokens);
						if (exp != null) AddToken(exp);
						else break;
					}
				}
				else
				{
					var exp = ExpressionToken.TryParse(Scope, _createIndexEndTokens);
					if (exp != null) AddToken(exp);
					else break;
				}
			}

			if (code.ReadExact('(') || code.ReadExact('{'))
			{
				var brackets = new BracketsToken(Scope);
				brackets.AddOpen(code.Span);
				AddToken(brackets);

				DkDict.Column field = null;

				while (!code.EndOfFile)
				{
					if (code.ReadExact(')') || code.ReadExact('}'))
					{
						brackets.AddClose(code.Span);
						break;
					}

					if (code.ReadExact(','))
					{
						brackets.AddToken(new DelimiterToken(Scope, code.Span));
					}

					if (table != null &&
						!string.IsNullOrEmpty(word = code.PeekWordR()) &&
						(field = table.GetColumn(word)) != null)
					{
						brackets.AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, field.Definition));
					}
					else
					{
						var exp = ExpressionToken.TryParse(Scope, _createIndexColumnEndTokens);
						if (exp != null) brackets.AddToken(exp);
						else break;
					}
				}
			}
		}

		private static readonly string[] _createStringdefEndTokens = new string[] { ";", "description" };

		private void ParseCreateStringdef()
		{
			var code = Code;

			var word = code.PeekWordR();
			var sd = DkDict.Dict.GetStringdef(word);
			if (sd != null)
			{
				AddToken(new IdentifierToken(Scope, code.Span, word, sd.Definition));
			}
			else
			{
				var exp = ExpressionToken.TryParse(Scope, _createStringdefEndTokens);
				if (exp != null) AddToken(exp);
			}

			while (!code.EndOfFile)
			{
				if (code.ReadExact(';'))
				{
					AddToken(new StatementEndToken(Scope, code.Span));
					break;
				}

				if (code.ReadStringLiteral())
				{
					AddToken(new StringLiteralToken(Scope, code.Span, code.Text));
					if (code.ReadNumber()) AddToken(new NumberToken(Scope, code.Span, code.Text));
					continue;
				}
				else if (code.ReadExactWholeWord("description"))
				{
					AddToken(new KeywordToken(Scope, code.Span, "description"));
					while (code.ReadStringLiteral()) AddToken(new StringLiteralToken(Scope, code.Span, code.Text));
				}
				else
				{
					var exp = ExpressionToken.TryParse(Scope, _createStringdefEndTokens);
					if (exp != null) AddToken(exp);
					else break;
				}
			}
		}

		private static readonly string[] _createTypedefEndTokens = new string[] { ";", "description" };

		private void ParseCreateTypedef()
		{
			var code = Code;

			var word = code.PeekWordR();
			if (!string.IsNullOrEmpty(word))
			{
				var td = DkDict.Dict.GetTypedef(word);
				if (td != null)
				{
					AddToken(new IdentifierToken(Scope, code.Span, word, td.Definition));
				}
				else
				{
					var exp = ExpressionToken.TryParse(Scope, _createTypedefEndTokens);
					if (exp != null) AddToken(exp);
				}

				var dt = DataType.TryParse(new DataType.ParseArgs
				{
					Code = code,
					Scope = Scope,
					TokenCreateCallback = token =>
					{
						AddToken(token);
					},
					VisibleModel = true,
					AllowTags = true
				});
				if (dt != null)
				{
					if (code.PeekWordR() == "description")
					{
						AddToken(new KeywordToken(Scope, code.Span, "description"));
						while (code.ReadStringLiteral()) AddToken(new StringLiteralToken(Scope, code.Span, code.Text));
					}

					if (code.ReadExact(';')) AddToken(new StatementEndToken(Scope, code.Span));
				}
			}
		}

		private static readonly string[] _createTimeRelationshipEndTokens = new string[] { "prompt", "comment", "description", "order", "tag", "to" };

		private void ParseCreateTimeRelationship()
		{
			var code = Code;
			var word = code.PeekWordR();
			var relind = DkDict.Dict.GetRelInd(word);
			if (relind != null)
			{
				AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, relind.Definition));
			}
			else return;

			if (code.ReadNumber()) AddToken(new NumberToken(Scope, code.Span, code.Text));
			else return;

			DkDict.Table table = null;

			while (!code.EndOfFile)
			{
				if (code.PeekExact('(') || code.PeekExact('{')) break;

				if (!string.IsNullOrEmpty(word = code.PeekWordR()))
				{
					if (word == "prompt" || word == "comment" || word == "description")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), word));
						var exp = ExpressionToken.TryParse(Scope, _createTimeRelationshipEndTokens);
						if (exp != null) AddToken(exp);
					}
					else if (word == "tag")
					{
						ParseTag(Scope, this, new KeywordToken(Scope, code.MovePeekedSpan(), word), _createTimeRelationshipEndTokens);
					}
					else if (word == "order")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), "order"));
						if (code.ReadExactWholeWord("by"))
						{
							AddToken(new KeywordToken(Scope, code.Span, "by"));

							while (!code.EndOfFile)
							{
								if (code.PeekExact('(') || code.PeekExact('{')) break;

								if (!string.IsNullOrEmpty(word = code.PeekWordR()))
								{
									var field = table != null ? table.GetColumn(word) : null;
									if (field != null) AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, field.Definition));
									else
									{
										var exp = ExpressionToken.TryParse(Scope, _createTimeRelationshipEndTokens);
										if (exp != null) AddToken(exp);
										else break;
									}
								}
							}
						}
					}
					else if (word == "to")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), "to"));

						if ((table = DkDict.Dict.GetTable(word)) != null)
						{
							AddToken(new IdentifierToken(Scope, code.Span, word, table.Definition));
						}
						else
						{
							var exp = ExpressionToken.TryParse(Scope, _createTimeRelationshipEndTokens);
							if (exp != null) AddToken(exp);
						}
					}
					else if ((table = DkDict.Dict.GetTable(word)) != null)
					{
						AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), word, table.Definition));

						if (code.ReadExactWholeWord("to"))
						{
							AddToken(new KeywordToken(Scope, code.Span, "to"));

							if ((table = DkDict.Dict.GetTable(word)) != null)
							{
								AddToken(new IdentifierToken(Scope, code.Span, word, table.Definition));
							}
							else
							{
								var exp = ExpressionToken.TryParse(Scope, _createTimeRelationshipEndTokens);
								if (exp != null) AddToken(exp);
							}
						}
					}
					else
					{
						var exp = ExpressionToken.TryParse(Scope, _createTimeRelationshipEndTokens);
						if (exp != null) AddToken(exp);
						else break;
					}
				}
				else
				{
					var exp = ExpressionToken.TryParse(Scope, _createTimeRelationshipEndTokens);
					if (exp != null) AddToken(exp);
					else break;
				}
			}

			if (code.PeekExact('('))
			{
				AddToken(BracketsToken.Parse(Scope, expectedDataType: null));
			}
		}

		private static readonly string[] _createWorkspaceEndTokens = new string[] { "(", "tag", "prompt", "comment", "image", "description" };
		private static readonly string[] _createWorkspaceColumnEndTokens = new string[] { ",", ")", "prompt", "comment", "tag", "preload" };

		private void ParseCreateWorkspace()
		{
			var code = Code;
			var word = code.PeekWordR();
			if (!string.IsNullOrEmpty(word)) AddToken(new UnknownToken(Scope, code.MovePeekedSpan(), word));
			else return;

			while (true)
			{
				if (code.PeekExact('(')) break;

				if (!string.IsNullOrEmpty(word = code.PeekWordR()))
				{
					if (word == "prompt" || word == "comment" || word == "description" || word == "image")
					{
						AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), word));
						var exp = ExpressionToken.TryParse(Scope, _createWorkspaceEndTokens);
						if (exp != null) AddToken(exp);
					}
					else if (word == "tag")
					{
						ParseTag(Scope, this, new KeywordToken(Scope, code.MovePeekedSpan(), "tag"), _createWorkspaceEndTokens);
					}
					else
					{
						var exp = ExpressionToken.TryParse(Scope, _createWorkspaceEndTokens);
						if (exp != null) AddToken(exp);
						else break;
					}
				}
				else
				{
					var exp = ExpressionToken.TryParse(Scope, _createWorkspaceEndTokens);
					if (exp != null) AddToken(exp);
					else break;
				}
			}

			if (code.ReadExact('('))
			{
				var brackets = new BracketsToken(Scope);
				brackets.AddOpen(code.Span);
				AddToken(brackets);

				while (true)
				{
					if (code.ReadExact(')'))
					{
						brackets.AddClose(code.Span);
						break;
					}

					if (code.ReadExact(','))
					{
						brackets.AddToken(new DelimiterToken(Scope, code.Span));
					}

					if (code.ReadWord())
					{
						brackets.AddToken(new UnknownToken(Scope, code.Span, code.Text));

						DkDict.Table table = null;
						do
						{
							table = DkDict.Dict.GetTable(code.PeekWordR());
							if (table != null)
							{
								brackets.AddToken(new IdentifierToken(Scope, code.MovePeekedSpan(), code.Text, table.Definition));
								if (code.ReadExact('\\')) brackets.AddToken(new OperatorToken(Scope, code.Span, code.Text));
							}
						}
						while (table != null);

						while (true)
						{
							if (code.ReadExact(','))
							{
								brackets.AddToken(new DelimiterToken(Scope, code.Span));
								break;
							}

							if (!string.IsNullOrEmpty(word = code.PeekWordR()))
							{
								if (word == "prompt" || word == "comment")
								{
									brackets.AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), word));
									var exp = ExpressionToken.TryParse(Scope, _createWorkspaceColumnEndTokens);
									if (exp != null) brackets.AddToken(exp);
								}
								else if (word == "preload")
								{
									brackets.AddToken(new KeywordToken(Scope, code.MovePeekedSpan(), word));
								}
								else if (word == "tag")
								{
									ParseTag(Scope, brackets, new KeywordToken(Scope, code.MovePeekedSpan(), "tag"), _createWorkspaceColumnEndTokens);
								}
								else
								{
									var exp = ExpressionToken.TryParse(Scope, _createWorkspaceColumnEndTokens);
									if (exp != null) brackets.AddToken(exp);
									else break;
								}
							}
							else
							{
								var exp = ExpressionToken.TryParse(Scope, _createWorkspaceColumnEndTokens);
								if (exp != null) brackets.AddToken(exp);
								else break;
							}
						}
					}
				}
			}
		}

	}
}
