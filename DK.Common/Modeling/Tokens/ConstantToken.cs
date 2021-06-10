using DK.Code;
using DK.Definitions;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class ConstantToken : WordToken
	{
		internal ConstantToken(Scope scope, CodeSpan span, string text, Definition def)
			: base(scope, span, text)
		{
			ClassifierType = ProbeClassifierType.Constant;
			SourceDefinition = def;
		}
	}
}
