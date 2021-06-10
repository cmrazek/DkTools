using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DK.Modeling.Tokens
{
	public sealed class TagToken : GroupToken
	{
		private TagToken(Scope scope)
			: base(scope)
		{ }

		internal static Token Parse(Scope scope, KeywordToken tagToken)
		{
			var code = scope.Code;
			code.SkipWhiteSpace();

			var word = code.PeekWordR();
			if (string.IsNullOrEmpty(word))
			{
				return tagToken;
			}

			if (!Constants.TagNames.Contains(word))
			{
				return tagToken;
			}

			var nameToken = new KeywordToken(scope, code.MovePeekedSpan(), word);

			var ret = new TagToken(scope);
			ret.AddToken(tagToken);
			ret.AddToken(nameToken);
			return ret;
		}
	}
}
