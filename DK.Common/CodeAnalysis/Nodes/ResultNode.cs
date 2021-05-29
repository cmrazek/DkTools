using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;

namespace DK.CodeAnalysis.Nodes
{
	class ResultNode : Node
	{
		private Value _value;
		private ResultSource _source;
		private int _prec;
		private bool _reportable;

		public ResultNode(Statement stmt, CodeSpan span, Value value, ResultSource source, CAErrorType? errorReported, bool reportable)
			: base(stmt, value.DataType, span)
		{
			_value = value;
			_source = source;
			ErrorReported = errorReported;
			_reportable = reportable;

			if (_source == ResultSource.Conditional1) _prec = 10;
		}

		public override void Execute(CAScope scope) { }
		public override bool IsReportable => _reportable;
		public override string ToString() => _value.ToString();

		public override Value ReadValue(CAScope scope)
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

		public override void Simplify(CAScope scope)
		{
			if (_source == ResultSource.Conditional1)
			{
				var rightNode = Parent.GetRightSibling(scope, this) as ResultNode;
				if (rightNode == null || rightNode.Source != ResultSource.Conditional2)
				{
					ReportError(Span, CAError.CA0021);  // Operator '?' expects ':' on right.
					Parent.ReplaceWithResult(Value.Void, false, this);
				}
				else
				{
					var rightValue = rightNode.ReadValue(scope);
					Parent.ReplaceWithResult(rightValue, !rightValue.IsVoid, this, rightNode);
				}
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
