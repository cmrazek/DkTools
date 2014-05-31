using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class StatementEndToken : Token
	{
		public StatementEndToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Operator;
		}

		public override bool BreaksStatement
		{
			get { return true; }
		}

		/// <summary>
		/// Attempts to parse a ';' from the file.
		/// </summary>
		/// <param name="parent">Parent token</param>
		/// <param name="scope">Current scope</param>
		/// <returns>If the next token is a ';', then a new StatementEndToken; otherwise null.</returns>
		public static StatementEndToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);

			if (file.PeekChar() != ';') return null;

			var startPos = file.Position;
			file.MoveNext();

			return new StatementEndToken(parent, scope, new Span(startPos, file.Position));
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
