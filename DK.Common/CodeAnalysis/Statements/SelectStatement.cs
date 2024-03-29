﻿using DK.Code;
using DK.CodeAnalysis.Nodes;
using DK.Definitions;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Statements
{
	class SelectStatement : Statement
	{
		private List<Definition> _tables = new List<Definition>();
		private ExpressionNode _whereExp;
		private ExpressionNode _filterByExp;
		private List<GroupBody> _groups = new List<GroupBody>();

		public override string ToString() => new string[] { "select * from ", _tables.Select(t => t.Name).Combine(", "), "..." }.Combine();

		public SelectStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;

			code.ReadStringLiteral();

			if (!code.ReadExact('*'))
			{
				ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0034, "*");	// Expected '{0}'.
				return;
			}

			var bodyStarted = false;
			while (!code.EndOfFile && !bodyStarted)
			{
				if (code.ReadExact('{'))
				{
					bodyStarted = true;
					break;
				}

				#region from
				if (code.ReadExact("from"))
				{
					while (!code.EndOfFile)
					{
						if (code.ReadExact('{'))
						{
							bodyStarted = true;
							break;
						}

						if (code.ReadWord())
						{
							if (code.Text == "where" || code.Text == "order" || code.Text == "filterby")
							{
								code.Position = code.TokenStartPostion;
								break;
							}

							if (code.Text == "of")
							{
								if (!code.ReadWord())
								{
									ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0036);    // Expected table name after 'of'.
									return;
								}
							}

							var def = (from d in CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetGlobalFromAnywhere(code.Text)
									   where d is TableDefinition || d is RelIndDefinition || d is ExtractTableDefinition
									   select d).FirstOrDefault();
							if (def == null)
							{
								ReportError(code.Span, CAError.CA0035, code.Text);  // Table or relationship '{0}' does not exist.
								return;
							}
							else
							{
								_tables.Add(def);
							}
						}
						else if (code.ReadExact(',')) { }
						else
						{
							ReportError(code.Span, CAError.CA0034, "{");    // Expected '{0}'.
							return;
						}
					}
				}
				#endregion
				#region where
				else if (code.ReadExact("where"))
				{
					_whereExp = ExpressionNode.Read(p, null, "from", "where", "order", "filterby");

					if (code.ReadExact(';'))
					{
						bodyStarted = true;
						break;
					}
				}
				#endregion
				#region filterby
				else if (code.ReadExact("filterby"))
                {
					_filterByExp = ExpressionNode.Read(p, null, "from", "where", "order", "filterby");

					if (code.ReadExact(';'))
					{
						bodyStarted = true;
						break;
					}
				}
				#endregion
				#region order by
				else if (code.ReadExact("order"))
				{
					if (!code.ReadExactWholeWord("by"))
					{
						ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0034, "by");  // Expected '{0}'.
						return;
					}

					while (!code.EndOfFile)
					{
						if (code.ReadExact('{'))
						{
							bodyStarted = true;
							break;
						}

						if (code.ReadExact(',')) continue;

						if (code.ReadExactWholeWord("from") || code.ReadExactWholeWord("where"))
						{
							code.Position = code.TokenStartPostion;
							break;
						}

						if (!ReadTableColOrRelInd(p, true)) return;
						if (!code.ReadExactWholeWord("asc")) code.ReadExactWholeWord("desc");
					}
				}
				#endregion
				else break;
			}

			if (bodyStarted)
			{
				while (!code.EndOfFile && !code.ReadExact('}'))
				{
					if (code.ReadExactWholeWord("before") || code.ReadExactWholeWord("after"))
					{
						if (!code.ReadExactWholeWord("group"))
						{
							ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0034, "group");   // Expected '{0}'.
							return;
						}

						if (code.ReadExactWholeWord("all")) { }
						else if (!ReadTableColOrRelInd(p, false)) return;

						if (!code.ReadExact(':'))
						{
							ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0034, ":");   // Expected '{0}'.
							return;
						}

						if (!ReadGroupStatements(p, false)) return;
					}
					else if (code.ReadExactWholeWord("for"))
					{
						if (!code.ReadExactWholeWord("each"))
						{
							ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0034, "each");    // Expected '{0}'.
							return;
						}

						if (!code.ReadExact(':'))
						{
							ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0034, ":");   // Expected '{0}'.
							return;
						}

						ReadGroupStatements(p, false);
					}
					else if (code.ReadExactWholeWord("default"))
					{
						if (!code.ReadExact(':'))
						{
							ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0034, ":");   // Expected '{0}'.
							return;
						}

						ReadGroupStatements(p, true);
					}
					else break;
				}
			}
		}

		private bool ReadTableColOrRelInd(ReadParams p, bool allowIndexes)
		{
			var code = p.Code;

			if (!code.ReadWord())
			{
				ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0038);	// Expected table or relationship name.
				return false;
			}

			var def = _tables.FirstOrDefault(x => x.Name == code.Text);
			if (def == null)
			{
				def = CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetGlobalFromAnywhere<RelIndDefinition>(code.Text).FirstOrDefault();
				if (def == null && allowIndexes)
				{
					ReportError(code.Span, CAError.CA0037, code.Text);	// Table or relationship '{0}' is not referenced in the 'from' clause.
					return false;
				}
			}

			Definition tableDef = def as TableDefinition;
			if (tableDef == null) tableDef = def as ExtractTableDefinition;
			if (tableDef != null)
			{
				if (!code.ReadExact('.'))
				{
					ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0034, ".");	// Expected '{0}'.
					return false;
				}

				if (!code.ReadWord())
				{
					ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA0039);	// Expected column name to follow table name.
					return false;
				}

				var fieldDef = tableDef.GetChildDefinitions(code.Text, p.AppSettings).FirstOrDefault();
				if (fieldDef == null)
				{
					ReportError(code.Span, CAError.CA0040, tableDef.Name, code.Text);	// Table '{0}' has no column '{1}'.
					return false;
				}
			}

			return true;
		}

		private bool ReadGroupStatements(ReadParams p, bool isDefault)
		{
			var body = new GroupBody { isDefault = isDefault };
			var code = p.Code;

			_groups.Add(body);

			while (!code.EndOfFile && !p.Code.PeekExact("}"))
			{
				var resetPos = code.Position;
				if (code.ReadExactWholeWord("before") || code.ReadExactWholeWord("after"))
				{
					if (code.ReadExactWholeWord("group"))
					{
						code.Position = resetPos;
						return true;
					}
					code.Position = resetPos;
				}
				else if (code.ReadExactWholeWord("for"))
				{
					if (code.ReadExactWholeWord("each"))
					{
						code.Position = resetPos;
						return true;
					}
					code.Position = resetPos;
				}
				else if (code.ReadExactWholeWord("default"))
				{
					code.Position = resetPos;
					return true;
				}

				var stmt = Statement.Read(p);
				if (stmt == null) break;
				body.stmts.Add(stmt);
			}

			return true;
		}

		private class GroupBody
		{
			public List<Statement> stmts = new List<Statement>();
			public bool isDefault;
		}

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			if (_whereExp != null)
			{
				_whereExp.ReadValue(scope);
			}

			if (_filterByExp != null)
            {
				_filterByExp.ReadValue(scope);
            }

			var foundScope = scope.Clone(canBreak: true, canContinue: true);
			foreach (var group in _groups)
			{
				if (group.isDefault) continue;

				foreach (var stmt in group.stmts)
				{
					stmt.Execute(foundScope);
				}
			}

			var defaultScope = scope.Clone(canBreak: true, canContinue: true);
			foreach (var group in _groups)
			{
				if (!group.isDefault) continue;

				foreach (var stmt in group.stmts)
				{
					stmt.Execute(defaultScope);
				}
			}

			scope.Merge(new CAScope[] { foundScope, defaultScope }, false, false);
		}
	}
}
