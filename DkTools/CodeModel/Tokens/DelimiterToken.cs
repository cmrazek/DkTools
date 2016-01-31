using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class DelimiterToken : Token
	{
		public DelimiterToken(Scope scope, Span span)
			: base(scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Delimiter;
		}

		public override string Text
		{
			get
			{
				return ",";
			}
		}
	}
}
