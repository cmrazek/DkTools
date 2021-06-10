namespace DK.Modeling.Tokens.Statements
{
	public class FooterStatement : GroupToken
	{
		private FooterStatement(Scope scope)
			: base(scope)
		{
		}

		internal static FooterStatement Parse(Scope scope, KeywordToken footerToken)
		{
			var ret = new FooterStatement(scope);
			ret.AddToken(footerToken);

			if (!scope.Code.PeekExact('{')) return ret;
			ret.AddToken(BracesToken.Parse(scope, null));

			return ret;
		}
	}
}
