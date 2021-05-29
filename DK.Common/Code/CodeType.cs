using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DK.Code
{
	public enum CodeType
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
