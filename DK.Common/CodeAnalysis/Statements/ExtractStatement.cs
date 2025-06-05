using DK.Code;
using DK.CodeAnalysis.Nodes;
using DK.Definitions;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Statements
{
	class ExtractStatement : Statement
	{
		private List<ExtractColumnNode> _columns = new List<ExtractColumnNode>();

		public ExtractStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;

			code.ReadExactWholeWord("permanent");	// Optional

			if (!code.ReadWord())
			{
				ReportError(keywordSpan, CAError.CA10044);	// Expected temp table name to follow 'extract'.
				return;
			}
			var tableName = code.Text;

			var def = p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>(tableName).FirstOrDefault();
			if (def == null)
			{
				ReportError(code.Span, CAError.CA10045, tableName);	// Extract table '{0}' does not exist.
				return;
			}

			while (!code.EndOfFile)
			{
				if (!code.ReadWord())
				{
					ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA10046);	// Expected extract column name.
					return;
				}
				var colSpan = code.Span;
				var colDef = def.GetChildDefinitions(code.Text, p.AppSettings).FirstOrDefault();
				if (colDef == null)
				{
					ReportError(code.Span, CAError.CA10046);	// Expected extract column name.
					return;
				}

				if (!code.ReadExact('='))
				{
					ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA10047);	// Expected '=' to follow extract column name.
					return;
				}
				var assignSpan = code.Span;

				var exp = ExpressionNode.Read(p, null);
				if (exp == null)
				{
					ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA10048);	// Expected extract column expression.
					return;
				}

				var colNode = new IdentifierNode(p.Statement, colSpan, colDef.Name, colDef, reportable: false);

				_columns.Add(new ExtractColumnNode(p.Statement, colSpan.Envelope(exp.Span), colNode, exp));

				if (code.ReadExact(';')) return;
			}
		}

		public override string ToString() => new string[] { "extract... " }.Concat(_columns.Select(x => x.ToString()).Delim(" ")).Combine();

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			foreach (var col in _columns)
			{
				col.Execute(scope);
			}
		}
	}
}
