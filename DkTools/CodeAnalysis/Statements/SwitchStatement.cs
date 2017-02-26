using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class SwitchStatement : Statement
	{
		private ExpressionNode _condExp;
		private OperatorNode _compareOp;
		private List<Case> _cases = new List<Case>();
		private List<Statement> _default;

		private class Case
		{
			public ExpressionNode value;
			public List<Statement> body = new List<Statement>();
			public Span caseSpan;
			public bool safeFallThrough;
		}

		public SwitchStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;

			_condExp = ExpressionNode.Read(p, "{", ";");
			if (_condExp == null)
			{
				ReportError(keywordSpan, CAError.CA0018, "switch");	// Expected condition after '{0}'.
				return;
			}
			var errorSpan = keywordSpan;

			if (_condExp.NumChildren > 1 && _condExp.LastChild is OperatorNode)
			{
				_condExp.RemoveChild(_compareOp = _condExp.LastChild as OperatorNode);
				errorSpan = _compareOp.Span;
			}

			if (!code.ReadExact('{'))
			{
				ReportError(errorSpan, CAError.CA0019);	// Expected '{'.
				return;
			}
			errorSpan = code.Span;

			var insideDefault = false;
			while (!code.EndOfFile)
			{
				if (code.ReadExact('}')) break;
				if (code.ReadExactWholeWord("case"))
				{
					errorSpan = code.Span;

					if (_cases.Count > 0)
					{
						var lastCase = _cases[_cases.Count - 1];
						if (lastCase != null && lastCase.body.Count == 0)
						{
							lastCase.safeFallThrough = true;
						}
					}

					_cases.Add(new Case
					{
						caseSpan = code.Span
					});
					insideDefault = false;

					var exp = ExpressionNode.Read(p, ":", "}", ";");
					if (exp == null) ReportError(errorSpan, CAError.CA0028);	// Expected case value.
					else
					{
						_cases.Last().value = exp;
						errorSpan = exp.Span;
					}

					if (!code.ReadExact(':')) ReportError(errorSpan, CAError.CA0029);	// Expected ':'.
				}
				else if (code.ReadExactWholeWord("default"))
				{
					if (_default != null)
					{
						ReportError(code.Span, CAError.CA0032);	// Duplicate default case.
					}

					insideDefault = true;
					_default = new List<Statement>();

					if (!code.ReadExact(':')) ReportError(errorSpan, CAError.CA0029);	// Expected ':'.
				}
				else if (code.ReadExactWholeWord("break"))
				{
					if (insideDefault) _default.Add(new BreakStatement(p, code.Span));
					else if (_cases.Any()) _cases.Last().body.Add(new BreakStatement(p, code.Span));
					else ReportError(code.Span, CAError.CA0023);	// 'break' is not valid here.
				}
				else
				{
					var stmt = Statement.Read(p);
					if (stmt == null) break;
					if (insideDefault) _default.Add(stmt);
					else if (_cases.Any()) _cases.Last().body.Add(stmt);
					else ReportError(stmt.Span, CAError.CA0030);	// Statement is not valid here.
				}
			}
		}

		public override void Execute(RunScope scope)
		{
			base.Execute(scope);

			DataType dataType = null;
			if (_condExp != null) dataType = _condExp.ReadValue(scope).DataType;

			var bodyScopes = new List<RunScope>();

			foreach (var cas in _cases)
			{
				if (cas.value != null)
				{
					var valueScope = scope.Clone(dataTypeContext: dataType);
					cas.value.ReadValue(valueScope);
					scope.Merge(valueScope, true, true);
				}

				if (!cas.safeFallThrough)
				{
					var bodyScope = scope.Clone(canBreak: true);
					foreach (var stmt in cas.body)
					{
						stmt.Execute(bodyScope);
					}
					bodyScopes.Add(bodyScope);

					if (bodyScope.Breaked == TriState.False && bodyScope.Returned == TriState.False && cas.body.Any())
					{
						ReportError(cas.caseSpan, CAError.CA0031);	// Switch fall-throughs are inadvisable.
					}
				}
			}

			if (_default != null)
			{
				var bodyScope = scope.Clone(canBreak: true);
				foreach (var stmt in _default) stmt.Execute(bodyScope);
				bodyScopes.Add(bodyScope);
			}
			else
			{
				// Because there's no default, this switch doesn't cover all code branches.
				// Add a dummy scope to indicate that.
				bodyScopes.Add(scope.Clone());
			}

			scope.Merge(bodyScopes, false, true);
		}
	}
}
