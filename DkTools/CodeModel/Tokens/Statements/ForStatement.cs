using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens.Statements
{
	class ForStatement : GroupToken, IBreakOwner, IContinueOwner
	{
		private ForStatement(Scope scope)
			: base(scope)
		{
		}

		private static readonly string[] _conditionEndTokens = new string[] { ")" };

		public static ForStatement Parse(Scope scope, KeywordToken forToken)
		{
			var ret = new ForStatement(scope);

			var code = scope.Code;
			ret.AddToken(forToken);
			if (!code.ReadExact('(')) return ret;

			var brackets = new BracketsToken(scope);
			brackets.AddOpen(code.Span);
			ret.AddToken(brackets);

			// Initializer
			var exp = ExpressionToken.TryParse(scope, _conditionEndTokens);
			if (exp != null) brackets.AddToken(exp);
			if (!code.ReadExact(';')) return ret;

			// Condition
			exp = ExpressionToken.TryParse(scope, _conditionEndTokens);
			if (exp != null) brackets.AddToken(exp);
			if (!code.ReadExact(';')) return ret;

			// Increment
			exp = ExpressionToken.TryParse(scope, _conditionEndTokens);
			if (exp != null) brackets.AddToken(exp);
			if (!code.ReadExact(')')) return ret;
			brackets.AddClose(code.Span);

			// Body
			var bodyScope = scope.Clone();
			bodyScope.BreakOwner = ret;
			bodyScope.ContinueOwner = ret;

			if (!code.PeekExact('{')) return ret;
			var body = BracesToken.Parse(bodyScope, null);
			ret.AddToken(body);

			return ret;
		}

		public void OnBreakAttached(BreakStatement breakToken)
		{
		}

		public void OnContinueAttached(ContinueStatement continueToken)
		{
		}

	}
}
