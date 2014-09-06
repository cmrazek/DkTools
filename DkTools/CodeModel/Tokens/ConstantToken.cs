using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class ConstantToken : WordToken
	{
		public ConstantToken(GroupToken parent, Scope scope, Span span, string text, Definition def)
			: base(parent, scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.Constant;
			SourceDefinition = def;
		}
	}
}
