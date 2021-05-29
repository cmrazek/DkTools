using DK.Code;
using System.Collections.Generic;

namespace DK.CodeAnalysis.Statements
{
	class FooterStatement : Statement
	{
		private List<Statement> _body = new List<Statement>();

		public FooterStatement(ReadParams p, CodeSpan keywordSpan)
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

		public override void Execute(CAScope scope)
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
