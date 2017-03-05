using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class NumberNode : TextNode
	{
		private decimal _value;

		public NumberNode(Statement stmt, Span span, string text)
			: base(stmt, span, text)
		{
			if (!decimal.TryParse(text, out _value))
			{
#if DEBUG
				throw new InvalidOperationException(string.Format("Unable to parse number '{0}'.", text));
#endif
			}
		}

		public override Value ReadValue(RunScope scope)
		{
			return new NumberValue(DataType.Numeric, _value);
		}

		public override DataType GetDataType(RunScope scope)
		{
			return DataType.Numeric;
		}
	}
}
