using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.TokenParser
{
	internal enum TokenType
	{
		Unknown,
		WhiteSpace,
		Comment,
		Word,
		Number,
		StringLiteral,
		Operator,
		Preprocessor,
		Nested
	}
}
