using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class IfStatement : Statement
	{
		private List<ExpressionNode> _conditions = new List<ExpressionNode>();
		private List<List<Statement>> _trueBodies = new List<List<Statement>>();
		private List<Statement> _falseBody;

		public override string ToString() => new string[] { "if ", _conditions.FirstOrDefault()?.ToString(), " {...}" }.Combine();

		public IfStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;
			var stmtSpan = keywordSpan;

			while (!code.EndOfFile)
			{
				var condition = ExpressionNode.Read(p, null);
				if (condition == null)
				{
					ReportError(stmtSpan, CAError.CA0018, "if");	// Expected condition after '{0}'.
					break;
				}

				_conditions.Add(condition);

				if (!code.ReadExact("{"))
				{
					ReportError(stmtSpan, CAError.CA0019);	// Expected '{'.
					break;
				}

				var trueBody = new List<Statement>();
				while (!code.EndOfFile && !code.ReadExact("}"))
				{
					var stmt = Statement.Read(p);
					if (stmt == null) break;
					trueBody.Add(stmt);
				}
				_trueBodies.Add(trueBody);

				if (code.ReadExactWholeWord("else"))
				{
					stmtSpan = code.Span;

					if (code.ReadExactWholeWord("if"))
					{
						stmtSpan = code.Span;
						continue;
					}

					if (!code.ReadExact("{"))
					{
						ReportError(stmtSpan, CAError.CA0019);	// Expected '{'.
						break;
					}

					_falseBody = new List<Statement>();
					while (!code.EndOfFile && !code.ReadExact("}"))
					{
						var stmt = Statement.Read(p);
						if (stmt == null) break;
						_falseBody.Add(stmt);
					}
				}

				break;
			}
		}

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			var scopes = new List<CAScope>();

			for (int i = 0, ii = _conditions.Count > _trueBodies.Count ? _conditions.Count : _trueBodies.Count; i < ii; i++)
			{
				Value condValue = null;
				if (i < _conditions.Count)
				{
					var condScope = scope.Clone();
					condValue = _conditions[i].ReadValue(condScope);
					scope.Merge(condScope, true, true);
				}

				if (i < _trueBodies.Count)
				{
					var trueScope = scope.Clone();
					foreach (var stmt in _trueBodies[i]) stmt.Execute(trueScope);
					scopes.Add(trueScope);
				}
			}

			var falseScope = scope.Clone();
			if (_falseBody != null)
			{
				foreach (var stmt in _falseBody) stmt.Execute(falseScope);
			}
			scopes.Add(falseScope);

			scope.Merge(scopes, true, true);
		}
	}
}
