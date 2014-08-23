using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class KeywordToken : WordToken
	{
		public KeywordToken(GroupToken parent, Scope scope, Span span, string text)
			: base(parent, scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.Keyword;
		}

		public static KeywordToken TryParseMatching(GroupToken parent, Scope scope, string word)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;

			var startPos = file.Position;
			if (!file.SkipMatch(word)) return null;

			return new KeywordToken(parent, scope, new Span(startPos, file.Position), word);
		}
	}
}
