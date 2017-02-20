using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.ErrorTagging;

namespace DkTools.CodeAnalysis
{
	class OperatorNode : TextNode
	{
		private int _prec;

		public OperatorNode(Statement stmt, Span span, string text)
			: base(stmt, span, text)
		{
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
				case "-":
					_prec = 22;
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
					_prec = 16;
					break;
				case "or":
					_prec = 14;
					break;
				case "?":
				case ":":
					_prec = 12;
					break;
				case "=":
				case "*=":
				case "/=":
				case "%=":
				case "+=":
				case "-=":
					_prec = 11;
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

		public override void Execute()
		{
			if (Parent == null) throw new InvalidOperationException("Operator node must have a parent.");

			switch (Text)
			{
				case "*":
				case "/":
				case "%":
				case "+":
				case "-":
					ExecuteMath();
					break;

				case "<":
				case ">":
				case "<=":
				case ">=":
				case "==":
				case "!=":
				case "and":
				case "or":
					ExecuteComparison();
					break;

				case "?":
					ExecuteConditional1();
					break;
				case ":":
					ExecuteConditional2();
					break;

				case "=":
				case "*=":
				case "/=":
				case "%=":
				case "+=":
				case "-=":
					ExecuteAssignment();
					break;
			}
		}

		private void ExecuteMath()	// * / % + -
		{
			var leftNode = Parent.GetLeftSibling(this);
			var rightNode = Parent.GetRightSibling(this);
			if (leftNode == null) ReportError(Span, CAError.CA0007, Text);			// Operator '{0}' expects value on left.
			else if (rightNode == null) ReportError(Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.
			if (leftNode != null && rightNode != null)
			{
				var leftValue = leftNode.Value;
				var rightValue = rightNode.Value;
				if (leftValue.IsVoid) leftNode.ReportError(leftNode.Span, CAError.CA0007, Text);		// Operator '{0}' expects value on left.
				else if (rightValue.IsVoid) rightNode.ReportError(rightNode.Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.

				Parent.ReplaceWithResult(new Value(leftValue, rightValue), leftNode, this, rightNode);
			}
			else
			{
				Value resultValue = Value.Empty;
				if (leftNode != null && rightNode == null) resultValue = leftNode.Value;
				else if (leftNode == null && rightNode != null) resultValue = rightNode.Value;
				Parent.ReplaceWithResult(resultValue, leftNode, this, rightNode);
			}
		}

		private void ExecuteComparison()	// < > <= >= == !=
		{
			var leftNode = Parent.GetLeftSibling(this);
			var rightNode = Parent.GetRightSibling(this);
			if (leftNode == null) ReportError(Span, CAError.CA0007, Text);	// Operator '{0}' expects value on left.
			else if (rightNode == null) ReportError(Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.
			if (leftNode != null && rightNode != null)
			{
				var leftValue = leftNode.Value;
				var rightValue = rightNode.Value;
				if (leftValue.IsVoid) leftNode.ReportError(leftNode.Span, CAError.CA0007, Text);		// Operator '{0}' expects value on left.
				else if (rightValue.IsVoid) rightNode.ReportError(rightNode.Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.

				Parent.ReplaceWithResult(new Value(leftValue, rightValue), leftNode, this, rightNode);
			}
			else
			{
				Value resultValue = Value.Empty;
				if (leftNode != null && rightNode == null) resultValue = leftNode.Value;
				else if (leftNode == null && rightNode != null) resultValue = rightNode.Value;
				Parent.ReplaceWithResult(resultValue, leftNode, this, rightNode);
			}
		}

		private void ExecuteAssignment()
		{
			var leftNode = Parent.GetLeftSibling(this);
			var rightNode = Parent.GetRightSibling(this);
			if (leftNode == null) ReportError(Span, CAError.CA0010, Text);			// Operator '{0}' expects assignable value on left.
			else if (rightNode == null) ReportError(Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.
			if (leftNode != null && rightNode != null)
			{
				var rightValue = rightNode.Value;
				if (!leftNode.CanAssignValue) leftNode.ReportError(leftNode.Span, CAError.CA0010, Text);				// Operator '{0}' expects assignable value on left.
				else if (rightValue.IsVoid) rightNode.ReportError(rightNode.Span, CAError.CA0008, Text);				// Operator '{0}' expects value on right.

				leftNode.Value = rightValue;	// To simulate a 'write'

				Parent.ReplaceWithResult(rightValue, leftNode, this, rightNode);
			}
			else
			{
				Value resultValue = Value.Empty;
				if (leftNode != null && rightNode == null) resultValue = leftNode.Value;
				else if (leftNode == null && rightNode != null) resultValue = rightNode.Value;
				Parent.ReplaceWithResult(resultValue, leftNode, this, rightNode);
			}
		}

		private void ExecuteConditional1()
		{
			var leftNode = Parent.GetLeftSibling(this);
			if (leftNode == null)
			{
				ReportError(Span, CAError.CA0007, Text);	// Operator '{0}' expects value on left.
				Parent.ReplaceWithResult(new Value(DataType.Int, true), this);
			}
			else
			{
				var leftValue = leftNode.Value;
				if (leftValue.IsVoid) leftNode.ReportError(Span, CAError.CA0007, Text);	// Operator '{0}' expects value on left.

				Parent.ReplaceWithResult(new Value(DataType.Int, leftValue.Initialized), leftNode, this);
			}
		}

		private void ExecuteConditional2()
		{
			var leftNode = Parent.GetLeftSibling(this);
			var rightNode = Parent.GetRightSibling(this);
			if (leftNode == null) ReportError(Span, CAError.CA0007, Text);	// Operator '{0}' expects value on left.
			else if (rightNode == null) ReportError(Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.
			if (leftNode != null && rightNode != null)
			{
				var leftValue = leftNode.Value;
				var rightValue = rightNode.Value;
				if (leftValue.IsVoid) leftNode.ReportError(leftNode.Span, CAError.CA0007, Text);		// Operator '{0}' expects value on left.
				else if (rightValue.IsVoid) rightNode.ReportError(rightNode.Span, CAError.CA0008, Text);	// Operator '{0}' expects value on right.

				Parent.ReplaceWithResult(new Value(leftValue, rightValue), leftNode, this, rightNode);
			}
			else
			{
				Value resultValue = Value.Empty;
				if (leftNode != null && rightNode == null) resultValue = leftNode.Value;
				else if (leftNode == null && rightNode != null) resultValue = rightNode.Value;
				Parent.ReplaceWithResult(resultValue, leftNode, this, rightNode);
			}
		}
	}
}
