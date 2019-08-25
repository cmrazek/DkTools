using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens.Operators;

namespace DkTools.CodeModel.Tokens.Statements
{
	class FormatStatement : GroupToken
	{
		private FormatStatement(Scope scope)
			: base(scope)
		{
		}

		private static readonly string[] _endTokens = new string[] { "rows", "cols", "genpages", "outfile" };

		public static FormatStatement Parse(Scope scope, KeywordToken formatToken)
		{
			var ret = new FormatStatement(scope);
			ret.AddToken(formatToken);

			var code = scope.Code;

			while (true)
			{
				if (code.ReadExact(';'))
				{
					ret.AddToken(new StatementEndToken(scope, code.Span));
					break;
				}

				var word = code.PeekWordR();
				if (string.IsNullOrEmpty(word)) break;

				if (word == "rows" || word == "cols" || word == "genpages" || word == "outfile")
				{
					ret.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), word));
					if (code.ReadExact('=')) ret.AddToken(new OperatorToken(scope, code.Span, "="));
					var exp = ExpressionToken.TryParse(scope, _endTokens);
					if (exp != null) ret.AddToken(exp);
					else break;
				}
				else break;
			}

			return ret;
		}
	}
}
