using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	class ResultNode : Node
	{
		private Value _value;

		public ResultNode(Statement stmt, Span span, Value value, ErrorTagging.ErrorType? errorReported)
			: base(stmt, span)
		{
			ErrorReported = errorReported;
		}

		public override Value Value
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
			}
		}
	}
}
