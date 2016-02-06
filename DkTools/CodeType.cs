using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools
{
	internal enum CodeType
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
