using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class DelimiterToken : Token
	{
		public DelimiterToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Delimiter;
		}

		/// <summary>
		/// Attempts to parse a delimiter from the file.
		/// </summary>
		/// <param name="parent">Parent token</param>
		/// <param name="scope">Current scope</param>
		/// <returns>If the next token is ',', then a new DelimiterToken; otherwise null.</returns>
		public static DelimiterToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);

			if (file.PeekChar() != ',') return null;

			var startPos = file.Position;
			file.MoveNext();

			return new DelimiterToken(parent, scope, new Span(startPos, file.Position));
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
