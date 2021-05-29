using DK.Code;
using DK.Definitions;

namespace DK.Modeling.Tokens
{
	public class ExtractTableToken : WordToken
	{
		internal ExtractTableToken(Scope scope, CodeSpan span, string name, ExtractTableDefinition def)
			: base(scope, span, name)
		{
			SourceDefinition = def;
		}
	}
}
