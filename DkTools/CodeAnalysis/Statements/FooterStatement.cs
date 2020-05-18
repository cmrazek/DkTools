using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class FooterStatement : Statement
	{
		private List<Statement> _body = new List<Statement>();

		public FooterStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;

			if (!code.ReadExact('{'))
			{
				ReportError(keywordSpan, CAError.CA0019);	// Expected '{'.
				return;
			}

			while (!code.EndOfFile && !code.ReadExact("}"))
			{
				var stmt = Statement.Read(p);
				if (stmt == null) break;
				_body.Add(stmt);
			}
		}

		public override string ToString() => "footer...";

		public override void Execute(RunScope scope)
		{
			var footerScope = scope.Clone();
			foreach (var stmt in _body)
			{
				stmt.Execute(footerScope);
			}
			// Don't merge the footerScope back into the parent scope because there's no guarantees
			// the footer is called asynchronously, or might not be called at all.
		}
	}
}
