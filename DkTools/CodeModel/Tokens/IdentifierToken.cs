using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class IdentifierToken : WordToken
	{
		public IdentifierToken(Scope scope, Span span, string text, Definition def)
			: base(scope, span, text)
		{
#if DEBUG
			if (def == null) throw new ArgumentNullException("def");
#endif
			SourceDefinition = def;
		}
	}
}
