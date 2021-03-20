using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis.Statements
{
	class ExtractStatement : Statement
	{
		private List<ExpressionNode> _colExps = new List<ExpressionNode>();

		public ExtractStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;

			code.ReadExactWholeWord("permanent");	// Optional

			if (!code.ReadWord())
			{
				ReportError(keywordSpan, CAError.CA0044);	// Expected temp table name to follow 'extract'.
				return;
			}
			var tableName = code.Text;

			var def = p.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>(tableName).FirstOrDefault();
			if (def == null)
			{
				ReportError(code.Span, CAError.CA0045, tableName);	// Extract table '{0}' does not exist.
				return;
			}

			while (!code.EndOfFile)
			{
				if (!code.ReadWord())
				{
					ReportError(new Span(code.Position, code.Position + 1), CAError.CA0046);	// Expected extract column name.
					return;
				}
				var colDef = def.GetChildDefinitions(code.Text, p.AppSettings).FirstOrDefault();
				if (colDef == null)
				{
					ReportError(code.Span, CAError.CA0046);	// Expected extract column name.
					return;
				}

				if (!code.ReadExact('='))
				{
					ReportError(new Span(code.Position, code.Position + 1), CAError.CA0047);	// Expected '=' to follow extract column name.
					return;
				}

				var exp = ExpressionNode.Read(p, null, true);
				if (exp == null)
				{
					ReportError(new Span(code.Position, code.Position + 1), CAError.CA0048);	// Expected extract column expression.
					return;
				}

				_colExps.Add(exp);

				if (code.ReadExact(';')) return;
			}
		}

		public override string ToString() => new string[] { "extract... " }.Concat(_colExps.Select(x => x.ToString()).Delim(" ")).Combine();

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			foreach (var exp in _colExps)
			{
				exp.ReadValue(scope);
			}
		}
	}
}
