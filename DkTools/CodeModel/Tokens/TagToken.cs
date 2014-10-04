using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal sealed class TagToken : GroupToken
	{
		private TagToken(GroupToken parent, Scope scope, IEnumerable<Token> tokens)
			: base(parent, scope, tokens)
		{ }

		public static Token Parse(GroupToken parent, Scope scope, KeywordToken tagToken)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);

			var word = file.PeekWord();
			if (string.IsNullOrEmpty(word))
			{
				return tagToken;
			}

			if (!Constants.TagNames.Contains(word))
			{
				return tagToken;
			}

			var nameToken = new KeywordToken(parent, scope, file.MoveNextSpan(word.Length), word);
			return new TagToken(parent, scope, new Token[] { tagToken, nameToken });
		}
	}
}
