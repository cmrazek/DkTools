using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class ReferenceToken : Token
	{
		public ReferenceToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Operator;
		}

		public override string Text
		{
			get { return "&"; }
		}

		public static ReferenceToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;
			if (file.PeekChar() != '&') return null;
			return new ReferenceToken(parent, scope, file.MoveNextSpan());
		}
	}
}
