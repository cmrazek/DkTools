using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class FunctionNameToken : WordToken
	{
		public FunctionNameToken(GroupToken parent, Scope scope, Span span, string text, FunctionDefinition sourceDef)
			: base(parent, scope, span, text)
		{
			SourceDefinition = sourceDef;
		}
	}
}
