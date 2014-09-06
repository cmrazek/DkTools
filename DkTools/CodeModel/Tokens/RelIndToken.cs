using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class RelIndToken : WordToken
	{
		public RelIndToken(GroupToken parent, Scope scope, Span span, string text, Definition def)
			: base(parent, scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.TableName;
			SourceDefinition = def;
		}

		public static RelIndToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;

			var word = file.PeekWord();
			var relInd = ProbeEnvironment.GetRelInd(word);
			if (relInd == null) return null;

			return new RelIndToken(parent, scope, file.MoveNextSpan(word.Length), word, relInd.Definition);
		}
	}
}
