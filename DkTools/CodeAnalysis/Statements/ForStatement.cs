using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class ForStatement : Statement
	{
		private ExpressionNode _initExp;
		private ExpressionNode _condExp;
		private ExpressionNode _incExp;
		private List<Statement> _body = new List<Statement>();

		public override string ToString() => new string[] { "for (", _condExp?.ToString(), ")..." }.Combine();

		public ForStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;
			if (!code.ReadExact('('))
			{
				ReportError(keywordSpan, CAError.CA0025);	// Expected '('.
				return;
			}
			var errSpan = code.Span;

			_initExp = ExpressionNode.Read(p, null, ")");
			if (_initExp != null) errSpan = _initExp.Span;

			if (!code.ReadExact(';'))
			{
				ReportError(errSpan, CAError.CA0026);	// Expected ';'.
				return;
			}
			errSpan = code.Span;

			_condExp = ExpressionNode.Read(p, null, ")");
			if (_condExp != null) errSpan = _condExp.Span;

			if (!code.ReadExact(';'))
			{
				ReportError(errSpan, CAError.CA0026);	// Expected ';'.
				return;
			}
			errSpan = code.Span;

			_incExp = ExpressionNode.Read(p, null, ")");
			if (_incExp != null) errSpan = _incExp.Span;

			if (!code.ReadExact(')'))
			{
				ReportError(errSpan, CAError.CA0027);	// Expected ')'.
				return;
			}
			errSpan = code.Span;

			if (!code.ReadExact('{'))
			{
				ReportError(errSpan, CAError.CA0019);	// Expected '{'.
				return;
			}
			errSpan = code.Span;

			while (!code.EndOfFile && !code.ReadExact("}"))
			{
				var stmt = Statement.Read(p);
				if (stmt == null) break;
				_body.Add(stmt);
				errSpan = stmt.Span;
			}
		}

		public override void Execute(RunScope scope)
		{
			base.Execute(scope);

			if (_initExp != null)
			{
				var initScope = scope.Clone();
				_initExp.ReadValue(initScope);
				scope.Merge(initScope);
			}

			if (_condExp != null)
			{
				var condScope = scope.Clone();
				_condExp.ReadValue(condScope);
				scope.Merge(condScope);
			}

			if (_incExp != null)
			{
				var incScope = scope.Clone();
				_incExp.ReadValue(incScope);
				scope.Merge(incScope);
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
