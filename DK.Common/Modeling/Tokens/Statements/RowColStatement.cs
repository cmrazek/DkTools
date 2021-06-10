namespace DK.Modeling.Tokens.Statements
{
	public class RowColStatement : GroupToken
	{
		private RowColStatement(Scope scope)
			: base(scope)
		{
		}

		internal static RowColStatement Parse(Scope scope, KeywordToken keywordToken)
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
