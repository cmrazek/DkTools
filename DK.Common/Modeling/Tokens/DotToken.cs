using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class DotToken : Token
	{
		internal DotToken(Scope scope, CodeSpan span)
			: base(scope, span)
		{
			ClassifierType = ProbeClassifierType.Delimiter;
		}

		public override string Text
		{
			get
			{
				return ".";
			}
		}
	}
}
