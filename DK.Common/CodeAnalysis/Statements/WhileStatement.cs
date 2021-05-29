using DK.Code;
using DK.CodeAnalysis.Nodes;
using System.Collections.Generic;

namespace DK.CodeAnalysis.Statements
{
	class WhileStatement : Statement
	{
		private ExpressionNode _cond;
		private List<Statement> _body = new List<Statement>();

		public WhileStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);

			_cond = ExpressionNode.Read(p, null);
			if (_cond == null)
			{
				ReportError(keywordSpan, CAError.CA0018, "if");	// Expected condition after '{0}'.
				return;
			}

			if (!p.Code.ReadExact("{"))
			{
				ReportError(keywordSpan, CAError.CA0019);	// Expected '{'.
				return;
			}

			while (!p.Code.EndOfFile && !p.Code.ReadExact("}"))
			{
				var stmt = Statement.Read(p);
				if (stmt == null) break;
				_body.Add(stmt);
			}
		}

		public override string ToString() => new string[] { "while (", _cond?.ToString(), ")..." }.Combine();

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			if (_cond != null)
			{
				var condScope = scope.Clone();
				_cond.ReadValue(condScope);
				scope.Merge(condScope, true, true);
			}

			var bodyScope = scope.Clone(canBreak: true, canContinue: true);
			foreach (var stmt in _body)
			{
				stmt.Execute(bodyScope);
			}
			scope.Merge(bodyScope, false, false);
		}
	}
}
