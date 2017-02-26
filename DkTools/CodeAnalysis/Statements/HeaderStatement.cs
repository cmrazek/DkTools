using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class HeaderStatement : Statement
	{
		private List<Statement> _body = new List<Statement>();

		public HeaderStatement(ReadParams p, Span keywordSpan)
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

		public override void Execute(RunScope scope)
		{
			var headerScope = scope.Clone();
			foreach (var stmt in _body)
			{
				stmt.Execute(headerScope);
			}
			// Don't merge the headerScope back into the parent scope because there's no guarantees
			// the header is called asynchronously, or might not be called at all.
		}
	}
}
