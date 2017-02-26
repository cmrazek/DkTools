using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;
using DkTools.ErrorTagging;

/*
 * Precedence:
 * 100	[]
 * 26   - (unary)
 * 24	* / %
 * 22	+ -
 * 20	< > <= >=
 * 18	== !=
 * 16	and
 * 14	or
 * 12	? :
 * 10	conditional result
 * 7	= *= /= %= += -=
 * 2	unresolved nodes
 * */

namespace DkTools.CodeAnalysis.Nodes
{
	class OperatorNode : TextNode
	{
		private int _prec;
		private SpecialOperator? _special;

		public OperatorNode(Statement stmt, Span span, string text, SpecialOperator? special)
			: base(stmt, span, text)
		{
			_special = special;

			// Determine the operator precedence
			// Even numbers are left-to-right, odd are right-to-left
			switch (text)
			{
				case "*":
				case "/":
				case "%":
					_prec = 24;
					break;
				case "+":
					_prec = 22;
					break;
				case "-":
					_prec = _special == SpecialOperator.UnaryMinus ? 26 : 22;
					break;
				case "<":
				case ">":
				case "<=":
				case ">=":
					_prec = 20;
					break;
				case "==":
				case "!=":
					_prec = 18;
					break;
				case "and":
				case "&&":
					_prec = 16;
					break;
				case "or":
				case "||":
					_prec = 14;
					break;
				//case "?":
				//case ":":
				//	_prec = 12;
				//	break;
				case "=":
				case "*=":
				case "/=":
				case "%=":
				case "+=":
				case "-=":
					_prec = 7;
					break;
				default:
					Statement.CodeAnalyzer.ReportError(Span, CAError.CA0006, text);	// Unknown operator '{0}'.
					_prec = 0;
					break;
			}
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
			if (Parent == null) throw new InvalidOperationException("Operator node must have a parent.");

			switch (Text)
			{
				case "*":
				case "/":
				case "%":
				case "+":
					ExecuteMath(scope);
					break;

				case "-":
					if (_special == SpecialOperator.UnaryMinus) ExecuteMinus(scope);
					else ExecuteMath(scope);
					break;

				case "<":
				case ">":
				case "<=":
				case ">=":
				case "==":
				case "!=":
				case "and":
				case "&&":
				case "or":
				case "||":
					ExecuteComparison(scope);
					break;

				//case "?":
				//	ExecuteConditional1(scope);
				//	break;
				//case ":":
				//	ExecuteConditional2(scope);
				//	break;

				case "=":
				case "*=":
				case "/=":
				case "%=":
				case "+=":
				case "-=":
					ExecuteAssignment(scope);
					break;
			}
		}

		private void ExecuteMath(RunScope scope)	// * / % + -
		{
			var leftNode = Parent.GetLeftSibling(scope, this);
			var rightNode = Parent.GetRightSibling(scope, this);
			if (leftNode == null) ReportError(Span, CAError.CA0007, Text);			// Operator '{0}' expects value on left.
			else if (rightNode == null) ReportError(Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.
			if (leftNode != null && rightNode != null)
			{
				var leftValue = leftNode.ReadValue(scope);
				var rightScope = scope.Clone(dataTypeContext: leftValue.DataType);
				var rightValue = rightNode.ReadValue(rightScope);
				scope.Merge(rightScope);
				if (leftValue.IsVoid) leftNode.ReportError(leftNode.Span, CAError.CA0007, Text);		// Operator '{0}' expects value on left.
				else if (rightValue.IsVoid) rightNode.ReportError(rightNode.Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.

				Parent.ReplaceWithResult(new Value(leftValue), leftNode, this, rightNode);
			}
			else
			{
				Value resultValue = Value.Empty;
				if (leftNode != null && rightNode == null) resultValue = leftNode.ReadValue(scope);
				else if (leftNode == null && rightNode != null) resultValue = rightNode.ReadValue(scope);
				Parent.ReplaceWithResult(resultValue, leftNode, this, rightNode);
			}
		}

		private void ExecuteMinus(RunScope scope)
		{
			var rightNode = Parent.GetRightSibling(scope, this);
			if (rightNode == null) ReportError(Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.
			else
			{
				var rightValue = rightNode.ReadValue(scope);
				if (rightValue.IsVoid) rightNode.ReportError(rightNode.Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.

				Parent.ReplaceWithResult(rightValue, this, rightNode);
			}
		}

		private void ExecuteComparison(RunScope scope)	// < > <= >= == !=
		{
			var leftNode = Parent.GetLeftSibling(scope, this);
			var rightNode = Parent.GetRightSibling(scope, this);
			if (leftNode == null) ReportError(Span, CAError.CA0007, Text);	// Operator '{0}' expects value on left.
			else if (rightNode == null) ReportError(Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.
			if (leftNode != null && rightNode != null)
			{
				var leftValue = leftNode.ReadValue(scope);
				var rightScope = scope.Clone(dataTypeContext: leftValue.DataType);
				var rightValue = rightNode.ReadValue(rightScope);
				scope.Merge(rightScope);
				if (leftValue.IsVoid) leftNode.ReportError(leftNode.Span, CAError.CA0007, Text);		// Operator '{0}' expects value on left.
				else if (rightValue.IsVoid) rightNode.ReportError(rightNode.Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.

				Parent.ReplaceWithResult(new Value(leftValue), leftNode, this, rightNode);
			}
			else
			{
				Value resultValue = Value.Empty;
				if (leftNode != null && rightNode == null) resultValue = leftNode.ReadValue(scope);
				else if (leftNode == null && rightNode != null) resultValue = rightNode.ReadValue(scope);
				Parent.ReplaceWithResult(resultValue, leftNode, this, rightNode);
			}
		}

		private void ExecuteAssignment(RunScope scope)	// = *= /= %= += -=
		{
			var leftNode = Parent.GetLeftSibling(scope, this);
			var rightNode = Parent.GetRightSibling(scope, this);
			if (leftNode == null) ReportError(Span, CAError.CA0010, Text);			// Operator '{0}' expects assignable value on left.
			else if (rightNode == null) ReportError(Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.
			if (leftNode != null && rightNode != null)
			{
				var leftDataType = leftNode.GetDataType(scope);
				var rightScope = scope.Clone(dataTypeContext: leftDataType);
				var rightValue = rightNode.ReadValue(rightScope);
				scope.Merge(rightScope);
				if (!leftNode.CanAssignValue(scope)) leftNode.ReportError(leftNode.Span, CAError.CA0010, Text);				// Operator '{0}' expects assignable value on left.
				else if (rightValue.IsVoid) rightNode.ReportError(rightNode.Span, CAError.CA0008, Text);				// Operator '{0}' expects value on right.

				leftNode.WriteValue(scope, rightValue);

				Parent.ReplaceWithResult(rightValue, leftNode, this, rightNode);
			}
			else
			{
				Value resultValue = Value.Empty;
				if (leftNode != null && rightNode == null) resultValue = leftNode.ReadValue(scope);
				else if (leftNode == null && rightNode != null) resultValue = rightNode.ReadValue(scope);
				Parent.ReplaceWithResult(resultValue, leftNode, this, rightNode);
			}
		}

		//private void ExecuteConditional1(RunScope scope)	// ?
		//{
		//	var leftScope = scope.Clone(dataTypeContext: DataType.Int);
		//	var leftNode = Parent.GetLeftSibling(leftScope, this);
		//	scope.Merge(leftScope);
		//	if (leftNode == null)
		//	{
		//		ReportError(Span, CAError.CA0007, Text);	// Operator '{0}' expects value on left.
		//		Parent.ReplaceWithResult(new Value(DataType.Int), ResultSource.Conditional1, this);
		//	}
		//	else
		//	{
		//		var leftValue = leftNode.ReadValue(leftScope);
		//		if (leftValue.IsVoid) leftNode.ReportError(Span, CAError.CA0007, Text);	// Operator '{0}' expects value on left.

		//		Parent.ReplaceWithResult(new Value(DataType.Int), ResultSource.Conditional1, leftNode, this);
		//	}
		//}

		//private void ExecuteConditional2(RunScope scope)	// :
		//{
		//	var leftNode = Parent.GetLeftSibling(scope, this);
		//	var rightNode = Parent.GetRightSibling(scope, this);
		//	if (leftNode == null) ReportError(Span, CAError.CA0007, Text);	// Operator '{0}' expects value on left.
		//	else if (rightNode == null) ReportError(Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.
		//	if (leftNode != null && rightNode != null)
		//	{
		//		// TODO: The data type should be based on the value that is calling the entire conditional
		//		var leftValue = leftNode.ReadValue(scope);
		//		var rightValue = rightNode.ReadValue(scope);
		//		if (leftValue.IsVoid) leftNode.ReportError(leftNode.Span, CAError.CA0007, Text);		// Operator '{0}' expects value on left.
		//		else if (rightValue.IsVoid) rightNode.ReportError(rightNode.Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.

		//		Parent.ReplaceWithResult(new Value(leftValue), ResultSource.Conditional2, leftNode, this, rightNode);
		//	}
		//	else
		//	{
		//		Value resultValue = Value.Empty;
		//		if (leftNode != null && rightNode == null) resultValue = leftNode.ReadValue(scope);
		//		else if (leftNode == null && rightNode != null) resultValue = rightNode.ReadValue(scope);
		//		Parent.ReplaceWithResult(resultValue, ResultSource.Conditional2, leftNode, this, rightNode);
		//	}
		//}
	}

	enum SpecialOperator
	{
		None,
		UnaryMinus
	}
}
