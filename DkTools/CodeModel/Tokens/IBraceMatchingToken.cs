using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	interface IBraceMatchingToken
	{
		IEnumerable<Token> BraceMatchingTokens { get; }
	}
}
