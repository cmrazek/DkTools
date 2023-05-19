using DK.Code;
using DK.CodeAnalysis.Nodes;
using DK.Definitions;
using DK.Modeling;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Statements
{
	class SwitchStatement : Statement
	{
		private ExpressionNode _condExp;
		private OperatorNode _compareOp;
		private List<Case> _cases = new List<Case>();
		private List<Statement> _default;

		private class Case
		{
			public ExpressionNode exp;
			public List<Statement> body = new List<Statement>();
			public CodeSpan caseSpan;
			public bool safeFallThrough;
		}

		public override string ToString() => new string[] { "switch (", _condExp?.ToString(), _compareOp?.ToString(), ")..." }.Combine();

		public SwitchStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;

			_condExp = ExpressionNode.Read(p, null);
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

					var exp = ExpressionNode.Read(p, _condExp.DataType, ":");
					if (exp == null) ReportError(errorSpan, CAError.CA0028);	// Expected case value.
					else
					{
						_cases.Last().exp = exp;
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

					if (_cases.Count > 0)
					{
						var lastCase = _cases[_cases.Count - 1];
						if (lastCase != null && lastCase.body.Count == 0)
						{
							lastCase.safeFallThrough = true;
						}
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

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			_condExp?.ReadValue(scope);

			var bodyScopes = new List<CAScope>();
			var currentBodyScope = (CAScope)null;

			for (int c = 0; c < _cases.Count; c++)
			{
				var cas = _cases[c];

				if (cas.exp != null)
				{
					var valueScope = scope.Clone();
					cas.exp.ReadValue(valueScope);
					scope.Merge(valueScope, true, true);
				}

				if (!cas.safeFallThrough)
				{
					if (currentBodyScope == null) currentBodyScope = scope.Clone(canBreak: true);
					foreach (var stmt in cas.body)
					{
						stmt.Execute(currentBodyScope);
					}

					if (currentBodyScope.Terminated == TriState.True)
					{
						bodyScopes.Add(currentBodyScope);
						currentBodyScope = null;
					}
					else if (currentBodyScope.Breaked == TriState.False && currentBodyScope.Returned == TriState.False && cas.body.Count > 0)
					{
						if (c == _cases.Count - 1 && _default == null)
						{
							// Fall-throughs are allowed on the last 'case', as long as there is no 'default'.
						}
						else
						{
							ReportError(cas.caseSpan, CAError.CA0031);  // Switch fall-throughs are inadvisable.
						}
					}
				}
			}

			if (_default != null)
			{
				if (currentBodyScope == null) currentBodyScope = scope.Clone(canBreak: true);
				foreach (var stmt in _default) stmt.Execute(currentBodyScope);
				bodyScopes.Add(currentBodyScope);
			}
			else
			{
				// Because there's no default, this switch doesn't cover all code branches.
				// Add a dummy scope to indicate that.
				if (currentBodyScope != null) bodyScopes.Add(currentBodyScope);
				bodyScopes.Add(scope.Clone());
			}

			scope.Merge(bodyScopes, false, true);
		}
	}
}
