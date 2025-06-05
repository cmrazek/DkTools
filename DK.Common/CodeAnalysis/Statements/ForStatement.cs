using DK.Code;
using DK.CodeAnalysis.Nodes;
using System.Collections.Generic;

namespace DK.CodeAnalysis.Statements
{
	class ForStatement : Statement
	{
		private Node _initExp;
		private Node _condExp;
		private Node _incExp;
		private List<Statement> _body = new List<Statement>();

		public override string ToString() => new string[] { "for (", _condExp?.ToString(), ")..." }.Combine();

		public ForStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;
			if (!code.ReadExact('('))
			{
				ReportError(keywordSpan, CAError.CA10025);	// Expected '('.
				return;
			}
			var errSpan = code.Span;

			_initExp = ExpressionNode.Read(p, null);
			if (_initExp != null) errSpan = _initExp.Span;

			if (!code.ReadExact(';'))
			{
				ReportError(errSpan, CAError.CA10026);	// Expected ';'.
				return;
			}
			errSpan = code.Span;

			_condExp = ExpressionNode.Read(p, null);
			if (_condExp != null) errSpan = _condExp.Span;

			if (!code.ReadExact(';'))
			{
				ReportError(errSpan, CAError.CA10026);	// Expected ';'.
				return;
			}
			errSpan = code.Span;

			_incExp = ExpressionNode.Read(p, null);
			if (_incExp != null) errSpan = _incExp.Span;

			if (!code.ReadExact(')'))
			{
				ReportError(errSpan, CAError.CA10027);	// Expected ')'.
				return;
			}
			errSpan = code.Span;

			if (!code.ReadExact('{'))
			{
				ReportError(errSpan, CAError.CA10019);	// Expected '{'.
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

		public override void Execute(CAScope scope)
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

            var notEnteredScope = scope.Clone();    // In the event the loop is never entered

            scope.Merge(new CAScope[] { bodyScope, notEnteredScope }, promoteBreak: false, promoteContinue: false);
		}
	}
}
