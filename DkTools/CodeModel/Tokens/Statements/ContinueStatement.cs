using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class ContinueStatement : GroupToken
	{
		private ContinueStatement(Scope scope)
			: base(scope)
		{
		}

		public static ContinueStatement Parse(Scope scope, KeywordToken continueToken)
		{
			var ret = new ContinueStatement(scope);
			ret.AddToken(continueToken);

			if (scope.Code.ReadExact(';')) ret.AddToken(new StatementEndToken(scope, scope.Code.Span));

			return ret;
		}
	}
}
