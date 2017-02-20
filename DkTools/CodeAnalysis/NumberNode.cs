using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	class NumberNode : TextNode
	{
		public NumberNode(Statement stmt, Span span, string text)
			: base(stmt, span, text)
		{
		}

		public override Value Value
		{
			get
			{
				return new Value(DataType.Numeric, true);
			}
			set
			{
				base.Value = value;
			}
		}
	}
}
