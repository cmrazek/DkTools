using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens.Statements
{
	class RowColStatement : GroupToken
	{
		private RowColStatement(Scope scope)
			: base(scope)
		{
		}

		public static RowColStatement Parse(Scope scope, KeywordToken rowColToken)
		{
			var ret = new RowColStatement(scope);
			ret.AddToken(rowColToken);

			var code = scope.Code;
			if (code.PeekExact('+') || code.PeekExact('-')) ret.AddToken(new OperatorToken(scope, code.MovePeekedSpan(), code.Text));
			if (code.ReadNumber()) ret.AddToken(new NumberToken(scope, code.Span, code.Text));

			var exp = ExpressionToken.TryParse(scope, null);
			if (exp != null) ret.AddToken(exp);

			if (code.PeekExact(';')) ret.AddToken(new StatementEndToken(scope, code.Span));

			return ret;
		}
	}
}
