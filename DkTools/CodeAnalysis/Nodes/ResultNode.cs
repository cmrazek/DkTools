using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class ResultNode : Node
	{
		private Value _value;

		public ResultNode(Statement stmt, Span span, Value value, ErrorTagging.ErrorType? errorReported)
			: base(stmt, span)
		{
			_value = value;
			ErrorReported = errorReported;
		}

		public override Value ReadValue(RunScope scope)
		{
			return _value;
		}
	}
}
