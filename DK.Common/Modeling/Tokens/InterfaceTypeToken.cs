using DK.Code;

namespace DK.Modeling.Tokens
{
	public class InterfaceTypeToken : WordToken
	{
		internal InterfaceTypeToken(Scope scope, CodeSpan span, Definitions.InterfaceTypeDefinition def)
			: base(scope, span, def.Name)
		{
			SourceDefinition = def;
		}
	}
}
