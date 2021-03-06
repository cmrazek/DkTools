﻿namespace DK.Modeling.Tokens.Statements
{
	public class FormatStatement : GroupToken
	{
		private FormatStatement(Scope scope)
			: base(scope)
		{
		}

		private static readonly string[] _endTokens = new string[] { "rows", "cols", "genpages", "outfile" };

		internal static FormatStatement Parse(Scope scope, KeywordToken formatToken)
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
