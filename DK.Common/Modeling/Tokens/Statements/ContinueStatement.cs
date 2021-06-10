namespace DK.Modeling.Tokens
{
	public class ContinueStatement : GroupToken
	{
		private ContinueStatement(Scope scope)
			: base(scope)
		{
		}

		internal static ContinueStatement Parse(Scope scope, KeywordToken continueToken)
		{
			var ret = new ContinueStatement(scope);
			ret.AddToken(continueToken);

			if (scope.Code.ReadExact(';')) ret.AddToken(new StatementEndToken(scope, scope.Code.Span));

			return ret;
		}
	}
}
