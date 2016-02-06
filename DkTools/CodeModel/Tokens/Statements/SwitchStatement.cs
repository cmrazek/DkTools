using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class SwitchStatement : GroupToken
	{
		private ExpressionToken _expressionToken;
		private OperatorToken _compareOpToken;	// Could be null if not used
		private BracesToken _bodyToken;	// Could be null for unfinished code.

		private SwitchStatement(Scope scope, KeywordToken switchToken)
			: base(scope)
		{
#if DEBUG
			if (switchToken == null) throw new ArgumentNullException("switchToken");
#endif
			AddToken(switchToken);
		}

		public static SwitchStatement Parse(Scope scope, KeywordToken switchToken)
		{
			var code = scope.Code;

			var ret = new SwitchStatement(scope, switchToken);

			var expressionScope = scope.Clone();
			expressionScope.Hint |= ScopeHint.SuppressDataType | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			ret._expressionToken = ExpressionToken.TryParse(expressionScope, null);
			if (ret._expressionToken != null) ret.AddToken(ret._expressionToken);

			if (code.ReadExact('{'))
			{
				var bodyScope = scope.Clone();
				bodyScope.Hint |= ScopeHint.SuppressFunctionDefinition;

				ret._bodyToken = new BracesToken(bodyScope, code.Span);
				ret.AddToken(ret._bodyToken);

				while (!code.EndOfFile)
				{
					if (code.ReadExact('}'))
					{
						ret._bodyToken.AddCloseBrace(code.Span);
						return ret;
					}

					var stmt = StatementToken.TryParse(bodyScope);
					if (stmt != null) ret._bodyToken.AddToken(stmt);
					else break;
				}
			}

			return ret;
		}

		public DataType ExpressionDataType
		{
			get
			{
				if (_expressionToken != null) return _expressionToken.ValueDataType;
				return null;
			}
		}
	}
}
