using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class ArgsToken : GroupToken
	{
		private ArgsToken(Scope scope, OperatorToken openBracketToken)
			: base(scope)
		{
			AddToken(openBracketToken);
		}

		private static string[] _endTokens = new string[] { ",", ")" };

		public static ArgsToken Parse(Scope scope, OperatorToken openBracketToken)
		{
			var code = scope.Code;
			var ret = new ArgsToken(scope, openBracketToken);

			while (code.SkipWhiteSpace())
			{
				code.Peek();
				if (code.Text == ")")
				{
					ret.AddToken(new OperatorToken(scope, code.MovePeekedSpan(), ")"));
					return ret;
				}

				if (code.Text == ",")
				{
					code.MovePeeked();
					ret.AddToken(new OperatorToken(scope, code.MovePeekedSpan(), ","));
					continue;
				}

				var exp = ExpressionToken.TryParse(scope, _endTokens);
				if (exp != null) ret.AddToken(exp);
				else break;
			}

			return ret;
		}
	}
}
