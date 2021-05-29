namespace DK.Modeling.Tokens
{
	public class BreakStatement : GroupToken
	{
		private BreakStatement(Scope scope)
			: base(scope)
		{
		}

		internal static BreakStatement Parse(Scope scope, KeywordToken breakToken)
		{
			var ret = new BreakStatement(scope);
			ret.AddToken(breakToken);

			if (scope.Code.ReadExact(';')) ret.AddToken(new StatementEndToken(scope, scope.Code.Span));

			return ret;
		}
	}
}
