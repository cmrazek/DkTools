using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class WhileStatement : GroupToken, IBreakOwner, IContinueOwner
	{
		private ExpressionToken _expressionToken;
		private BracesToken _bodyToken;	// Could be null for unfinished code.

		private WhileStatement(Scope scope, KeywordToken whileToken)
			: base(scope)
		{
			AddToken(whileToken);
		}

		public static WhileStatement Parse(Scope scope, KeywordToken whileToken)
		{
			var code = scope.Code;
			var ret = new WhileStatement(scope, whileToken);

			scope = scope.Clone();
			scope.BreakOwner = ret;
			scope.ContinueOwner = ret;

			// Expression
			var expressionScope = scope.Clone();
			expressionScope.Hint |= ScopeHint.SuppressDataType | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			ret._expressionToken = ExpressionToken.TryParse(scope, null);
			if (ret._expressionToken != null)
			{
				ret.AddToken(ret._expressionToken);

				if (code.PeekExact('{'))
				{
					var bodyScope = scope.Clone();
					bodyScope.Hint |= ScopeHint.SuppressFunctionDefinition;

					ret.AddToken(ret._bodyToken = BracesToken.Parse(bodyScope, null));
				}
			}

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
