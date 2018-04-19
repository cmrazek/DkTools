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
	class ResultNode : Node
	{
		private Value _value;
		private ResultSource _source;
		private int _prec;

		public ResultNode(Statement stmt, Span span, Value value, ResultSource source, ErrorTagging.ErrorType? errorReported)
			: base(stmt, value.DataType, span)
		{
			_value = value;
			_source = source;
			ErrorReported = errorReported;

			if (_source == ResultSource.Conditional1) _prec = 10;
		}

		public override Value ReadValue(RunScope scope)
		{
			return _value;
		}

		public ResultSource Source
		{
			get { return _source; }
		}

		public override int Precedence
		{
			get
			{
				return _prec;
			}
		}

		public override void Simplify(RunScope scope)
		{
			if (_source == ResultSource.Conditional1)
			{
				var rightNode = Parent.GetRightSibling(scope, this) as ResultNode;
				if (rightNode == null || rightNode.Source != ResultSource.Conditional2)
				{
					ReportError(Span, CAError.CA0021);	// Operator '?' expects ':' on right.
					Parent.ReplaceWithResult(Value.Void, this);
				}

				Parent.ReplaceWithResult(rightNode.ReadValue(scope), this, rightNode);
			}
			else
			{
				base.Simplify(scope);
			}
		}
	}

	enum ResultSource
	{
		Normal,
		Conditional1,
		Conditional2
	}
}
