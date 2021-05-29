using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class KeywordToken : WordToken
	{
		internal KeywordToken(Scope scope, CodeSpan span, string text)
			: base(scope, span, text)
		{
			ClassifierType = ProbeClassifierType.Keyword;
		}
	}
}
