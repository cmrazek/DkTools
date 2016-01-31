using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal sealed class IfStatementToken : GroupToken
	{
		private ExpressionToken _condition;
		private BracesToken _trueBody = null;
		private KeywordToken _elseToken = null;
		private Token _falseBody = null;

		private IfStatementToken(Scope scope)
			: base(scope)
		{
		}

		public static IfStatementToken Parse(Scope scope, KeywordToken ifToken)
		{
			var code = scope.Code;

			var ret = new IfStatementToken(scope);
			ret.AddToken(ifToken);

			var scopeIndent = scope.Clone();
			scopeIndent.Hint |= ScopeHint.NotOnRoot | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			ret._condition = ExpressionToken.TryParse(scopeIndent, null);
			if (ret._condition != null) ret.AddToken(ret._condition);

			if (code.PeekExact('{'))
			{
				ret.AddToken(ret._trueBody = BracesToken.Parse(scopeIndent));

				if (code.ReadExactWholeWord("else"))
				{
					ret.AddToken(ret._elseToken = new KeywordToken(scopeIndent, code.Span, "else"));

					if (code.PeekExact('{'))
					{
						ret.AddToken(ret._falseBody = BracesToken.Parse(scopeIndent));
					}
					else if (code.ReadExactWholeWord("if"))
					{
						var ifToken2 = new KeywordToken(scopeIndent, code.Span, "if");
						ret.AddToken(ret._falseBody = IfStatementToken.Parse(scopeIndent, ifToken2));
					}
				}
			}

			return ret;
		}

		public override bool BreaksStatement
		{
			get
			{
				return true;
			}
		}
	}
}
