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
		public ClassToken(Scope scope, Span span, string text, ClassDefinition classDef)
			: base(scope, span, text)
		{
			SourceDefinition = classDef;
		}

		public ClassToken(Scope scope, Span span, string text, GlobalData.RepoClass ffClass)
			: base(scope, span, text)
		{
			SourceDefinition = new ClassDefinition(ffClass.Name, ffClass.FileName);
		}
	}
}
