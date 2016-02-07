using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens.Statements
{
	class HeaderStatement : GroupToken
	{
		private HeaderStatement(Scope scope)
			: base(scope)
		{
		}

		public static HeaderStatement Parse(Scope scope, KeywordToken headerToken)
		{
			var ret = new HeaderStatement(scope);
			ret.AddToken(headerToken);

			if (!scope.Code.PeekExact('{')) return ret;
			ret.AddToken(BracesToken.Parse(scope, null));

			return ret;
		}
	}
}
