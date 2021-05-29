using DK.Code;
using DK.Definitions;

namespace DK.Modeling.Tokens
{
	public class ExtractFieldToken : WordToken
	{
		internal ExtractFieldToken(Scope scope, CodeSpan span, string name, ExtractFieldDefinition def)
			: base(scope, span, name)
		{
			SourceDefinition = def;
		}
	}
}
