using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.Classifier
{
	internal enum ProbeClassifierType
	{
		Normal,
		Comment,
		Keyword,
		Number,
		StringLiteral,
		Preprocessor,
		Inactive,
		TableName,
		TableField,
		Constant,
		DataType,
		Function,
		Delimiter,
		Operator
	}
}
