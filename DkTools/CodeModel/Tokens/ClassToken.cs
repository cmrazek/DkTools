using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class ClassToken : WordToken
	{
		public ClassToken(GroupToken parent, Scope scope, Span span, string text, ClassDefinition classDef)
			: base(parent, scope, span, text)
		{
			SourceDefinition = classDef;
		}

		public ClassToken(GroupToken parent, Scope scope, Span span, string text, FunctionFileScanning.FFClass ffClass)
			: base(parent, scope, span, text)
		{
			SourceDefinition = ffClass.ClassDefinition;
		}
	}
}
