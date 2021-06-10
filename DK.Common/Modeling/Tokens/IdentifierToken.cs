using DK.Code;
using DK.Definitions;
using System;

namespace DK.Modeling.Tokens
{
	public class IdentifierToken : WordToken
	{
		internal IdentifierToken(Scope scope, CodeSpan span, string text, Definition def)
			: base(scope, span, text)
		{
#if DEBUG
			if (def == null) throw new ArgumentNullException("def");
#endif
			SourceDefinition = def;
		}

		public override DataType ValueDataType
		{
			get
			{
				var def = SourceDefinition;
				if (def != null) return def.DataType;
				return null;
			}
		}
	}
}
