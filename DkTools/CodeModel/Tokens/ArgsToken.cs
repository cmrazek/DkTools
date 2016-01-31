using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class ArgsToken : GroupToken
	{
		private ArgsToken(GroupToken parent, Scope scope, OperatorToken openBracketToken)
			: base(parent, scope, new Token[] { openBracketToken })
		{
		}

		private static string[] _endTokens = new string[] { ",", ")" };

		public static ArgsToken Parse(GroupToken parent, Scope scope, OperatorToken openBracketToken)
		{
			var file = scope.File;
			var ret = new ArgsToken(parent, scope, openBracketToken);

			while (file.SkipWhiteSpaceAndComments(scope))
			{
				if (file.IsMatch(')'))
				{
					ret.AddToken(new OperatorToken(ret, scope, file.MoveNextSpan(), ")"));
					return ret;
				}

				if (file.IsMatch(','))
				{
					ret.AddToken(new OperatorToken(ret, scope, file.MoveNextSpan(), ","));
					continue;
				}

				var exp = ExpressionToken.TryParse(ret, scope, _endTokens);
				if (exp != null) ret.AddToken(exp);
				else break;
			}

			return ret;
		}
	}
}
