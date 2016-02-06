using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class BreakStatement : GroupToken
	{
		private BreakStatement(Scope scope)
			: base(scope)
		{
		}

		public static BreakStatement Parse(Scope scope, KeywordToken breakToken)
		{
			var ret = new BreakStatement(scope);
			ret.AddToken(breakToken);

			if (scope.Code.ReadExact(';')) ret.AddToken(new StatementEndToken(scope, scope.Code.Span));

			return ret;
		}
	}
}
