using System.Collections.Generic;

namespace DK.Modeling.Tokens
{
	public interface IBraceMatchingToken
	{
		IEnumerable<Token> BraceMatchingTokens { get; }
	}
}
