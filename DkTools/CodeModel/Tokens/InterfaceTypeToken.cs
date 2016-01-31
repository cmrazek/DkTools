using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal sealed class InterfaceTypeToken : WordToken
	{
		public InterfaceTypeToken(Scope scope, Span span, Definitions.InterfaceTypeDefinition def)
			: base(scope, span, def.Name)
		{
			SourceDefinition = def;
		}
	}
}
