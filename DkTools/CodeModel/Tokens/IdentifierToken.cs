using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class IdentifierToken : WordToken
	{
		public IdentifierToken(GroupToken parent, Scope scope, Span span, string text)
			: base(parent, scope, span, text)
		{ }

		/// <summary>
		/// Attempts to parse an identifier from the file.
		/// </summary>
		/// <param name="parent">Parent token</param>
		/// <param name="scope">Current scope</param>
		/// <returns>If the next token is a 'word', an IdentifierToken for that word; otherwise null.</returns>
		public static IdentifierToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);

			var word = file.PeekWord();
			if (string.IsNullOrEmpty(word)) return null;

			var startPos = file.Position;
			file.MoveNext(word.Length);

			return new IdentifierToken(parent, scope, new Span(startPos, file.Position), word);
		}
	}
}
