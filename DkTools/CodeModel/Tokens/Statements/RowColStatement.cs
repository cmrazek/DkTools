using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens.Operators;

namespace DkTools.CodeModel.Tokens.Statements
{
	class RowColStatement : GroupToken
	{
		private RowColStatement(Scope scope)
			: base(scope)
		{
		}

		public static RowColStatement Parse(Scope scope, KeywordToken keywordToken)
		{
			var ret = new RowColStatement(scope);
			var code = scope.Code;

			while (keywordToken != null)
			{
				ret.AddToken(keywordToken);

				switch (keywordToken.Text)
				{
					case "col":
					case "colff":
					case "page":
					case "row":
						if (code.PeekExact('+') || code.PeekExact('-')) ret.AddToken(new OperatorToken(scope, code.MovePeekedSpan(), code.Text));
						if (code.ReadNumber()) ret.AddToken(new NumberToken(scope, code.Span, code.Text));
						break;
				}

				if (Constants.ReportOutputKeywords.Contains(code.PeekWordR()))
				{
					keywordToken = new KeywordToken(scope, code.MovePeekedSpan(), code.Text);
				}
				else
				{
					keywordToken = null;
				}
			}

			var exp = ExpressionToken.TryParse(scope, null);
			if (exp != null) ret.AddToken(exp);

			if (code.PeekExact(';')) ret.AddToken(new StatementEndToken(scope, code.Span));

			return ret;
		}
	}
}
