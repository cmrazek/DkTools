using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class DotToken : Token
	{
		public DotToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Delimiter;
		}

		public override string Text
		{
			get
			{
				return ".";
			}
		}

		public static DotToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;
			if (file.PeekChar() != '.') return null;
			return new DotToken(parent, scope, file.MoveNextSpan());
		}
	}
}
