using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class StatementEndToken : Token
	{
		public StatementEndToken(Scope scope, Span span)
			: base(scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Operator;
		}

		public override bool BreaksStatement
		{
			get { return true; }
		}

		public override string Text
		{
			get
			{
				return ";";
			}
		}
	}
}
