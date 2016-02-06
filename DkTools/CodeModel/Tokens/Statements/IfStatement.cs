using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal sealed class IfStatement : GroupToken
	{
		private ExpressionToken _condition;
		private BracesToken _trueBody = null;
		private KeywordToken _elseToken = null;
		private Token _falseBody = null;

		private IfStatement(Scope scope)
			: base(scope)
		{
		}

		public static IfStatement Parse(Scope scope, KeywordToken ifToken)
		{
			var code = scope.Code;

			var ret = new IfStatement(scope);
			ret.AddToken(ifToken);

			var scopeIndent = scope.Clone();
			scopeIndent.Hint |= ScopeHint.NotOnRoot | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			ret._condition = ExpressionToken.TryParse(scopeIndent, null);
			if (ret._condition != null) ret.AddToken(ret._condition);

			if (code.PeekExact('{'))
			{
				ret.AddToken(ret._trueBody = BracesToken.Parse(scopeIndent, null));

				if (code.ReadExactWholeWord("else"))
				{
					ret.AddToken(ret._elseToken = new KeywordToken(scopeIndent, code.Span, "else"));

					if (code.PeekExact('{'))
					{
						ret.AddToken(ret._falseBody = BracesToken.Parse(scopeIndent, null));
					}
					else if (code.ReadExactWholeWord("if"))
					{
						var ifToken2 = new KeywordToken(scopeIndent, code.Span, "if");
						ret.AddToken(ret._falseBody = IfStatement.Parse(scopeIndent, ifToken2));
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
