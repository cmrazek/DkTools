namespace DK.Modeling.Tokens.Statements
{
	public class ReturnStatement : GroupToken
	{
		private ReturnStatement(Scope scope)
			: base(scope)
		{
		}

		internal static ReturnStatement Parse(Scope scope, KeywordToken returnToken)
		{
			var ret = new ReturnStatement(scope);
			ret.AddToken(returnToken);

			var code = scope.Code;
			if (code.ReadExact(';'))
			{
				ret.AddToken(new StatementEndToken(scope, code.Span));
				return ret;
			}

			var exp = ExpressionToken.TryParse(scope, null, expectedDataType: scope.ReturnDataType);
			if (exp != null) ret.AddToken(exp);

			if (code.ReadExact(';')) ret.AddToken(new StatementEndToken(scope, code.Span));

			return ret;
		}
	}
}
