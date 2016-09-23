using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class RelIndToken : WordToken
	{
		public RelIndToken(Scope scope, Span span, string text, Definition def)
			: base(scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.TableName;
			SourceDefinition = def;
		}

		public override DataType ValueDataType
		{
			get
			{
				return DataType.IndRel;
			}
		}
	}
}
