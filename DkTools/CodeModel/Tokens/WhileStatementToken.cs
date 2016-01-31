using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class WhileStatementToken : GroupToken
	{
		private ExpressionToken _expressionToken;
		private BracesToken _bodyToken;	// Could be null for unfinished code.

		private WhileStatementToken(Scope scope, KeywordToken whileToken)
			: base(scope)
		{
			AddToken(whileToken);
		}

		public static WhileStatementToken Parse(Scope scope, KeywordToken whileToken)
		{
			var code = scope.Code;
			var ret = new WhileStatementToken(scope, whileToken);

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

					ret.AddToken(ret._bodyToken = BracesToken.Parse(bodyScope));
				}
			}

			return ret;
		}
	}
}
