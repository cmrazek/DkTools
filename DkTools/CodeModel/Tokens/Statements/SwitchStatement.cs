using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class SwitchStatement : GroupToken, IBreakOwner
	{
		private ExpressionToken _expressionToken;
		private BracesToken _bodyToken;	// Could be null for unfinished code.

		private SwitchStatement(Scope scope, KeywordToken switchToken)
			: base(scope)
		{
#if DEBUG
			if (switchToken == null) throw new ArgumentNullException("switchToken");
#endif
			AddToken(switchToken);
		}

		private static readonly string[] _caseEndTokens = new string[] { ":" };

		public static SwitchStatement Parse(Scope scope, KeywordToken switchToken)
		{
			var ret = new SwitchStatement(scope, switchToken);

			scope = scope.Clone();
			scope.BreakOwner = ret;

			var code = scope.Code;

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

				var switchDataType = ret.ExpressionDataType;

				while (true)
				{
					if (code.ReadExact('}'))
					{
						ret._bodyToken.AddClose(code.Span);
						return ret;
					}

					if (code.ReadExactWholeWord("case"))
					{
						ret.AddToken(new KeywordToken(scope, code.Span, "case"));

						var caseExp = ExpressionToken.TryParse(scope, _caseEndTokens, expectedDataType: switchDataType);
						if (caseExp != null) ret.AddToken(caseExp);

						if (code.ReadExact(':')) ret.AddToken(new OperatorToken(scope, code.Span, ":"));
						continue;
					}

					if (code.ReadExactWholeWord("default"))
					{
						ret.AddToken(new KeywordToken(scope, code.Span, "default"));
						if (code.ReadExact(':')) ret.AddToken(new OperatorToken(scope, code.Span, ":"));
						continue;
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
				var lastChild = _expressionToken.Children.LastOrDefault();
				if (lastChild != null)
				{
					if (lastChild is CompletionOperator) return (lastChild as CompletionOperator).CompletionDataType;
					return lastChild.ValueDataType;
				}
				return null;
			}
		}

		public void OnBreakAttached(BreakStatement breakToken)
		{
		}
	}
}
