namespace DK.Modeling.Tokens
{
	public class ExtractTableAndFieldToken : GroupToken
	{
		internal ExtractTableAndFieldToken(Scope scope, ExtractTableToken exToken, DotToken dotToken, ExtractFieldToken fieldToken)
			: base(scope)
		{
			AddToken(exToken);
			AddToken(dotToken);
			AddToken(fieldToken);
		}
	}
}
