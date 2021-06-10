using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class ReferenceToken : Token
	{
		internal ReferenceToken(Scope scope, CodeSpan span)
			: base(scope, span)
		{
			ClassifierType = ProbeClassifierType.Operator;
		}

		public override string Text
		{
			get { return "&"; }
		}
	}
}
