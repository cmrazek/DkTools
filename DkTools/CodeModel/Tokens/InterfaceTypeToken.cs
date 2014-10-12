using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal sealed class InterfaceTypeToken : WordToken
	{
		public InterfaceTypeToken(GroupToken parent, Scope scope, Span span, Definitions.InterfaceTypeDefinition def)
			: base(parent, scope, span, def.Name)
		{
			SourceDefinition = def;
		}
	}
}
