namespace DK.Modeling.Tokens.Statements
{
	public class HeaderStatement : GroupToken
	{
		private HeaderStatement(Scope scope)
			: base(scope)
		{
		}

		internal static HeaderStatement Parse(Scope scope, KeywordToken headerToken)
		{
			var ret = new HeaderStatement(scope);
			ret.AddToken(headerToken);

			if (!scope.Code.PeekExact('{')) return ret;
			ret.AddToken(BracesToken.Parse(scope, null));

			return ret;
		}
	}
}
