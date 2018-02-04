using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens.Statements
{
	class FooterStatement : GroupToken
	{
		private FooterStatement(Scope scope)
			: base(scope)
		{
		}

		public static FooterStatement Parse(Scope scope, KeywordToken footerToken)
		{
			var ret = new FooterStatement(scope);
			ret.AddToken(footerToken);

			if (!scope.Code.PeekExact('{')) return ret;
			ret.AddToken(BracesToken.Parse(scope, null));

			return ret;
		}
	}
}
