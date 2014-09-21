using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class ExtractTableToken : WordToken
	{
		public ExtractTableToken(GroupToken parent, Scope scope, Span span, string name, ExtractTableDefinition def)
			: base(parent, scope, span, name)
		{
			SourceDefinition = def;
		}
	}
}
