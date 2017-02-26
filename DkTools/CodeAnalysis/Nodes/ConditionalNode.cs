﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;
using DkTools.ErrorTagging;

namespace DkTools.CodeAnalysis.Nodes
{
	class ConditionalNode : GroupNode
	{
		private ExpressionNode _trueExp;
		private GroupNode _falseExp;
		private Span _opSpan;

		private ConditionalNode(Statement stmt, Span opSpan)
			: base(stmt, opSpan)
		{
			_opSpan = opSpan;
		}

		private static string[] s_stopStrings = new string[] { "?", ":" };

		public static ConditionalNode Read(ReadParams p, Span opSpan, string[] stopStrings)
		{
			var code = p.Code;
			var ret = new ConditionalNode(p.Statement, opSpan);

			var condStopStrings = stopStrings == null || stopStrings.Length == 0 ? s_stopStrings : stopStrings.Concat(s_stopStrings).ToArray();
			var trueExp = ExpressionNode.Read(p, condStopStrings);
			if (trueExp == null)
			{
				p.Statement.ReportError(new Span(code.Position, code.Position + 1), CAError.CA0042);	// Expected value to follow conditional '?'.
				return ret;
			}
			ret._trueExp = trueExp;

			if (!code.ReadExact(':'))
			{
				p.Statement.ReportError(new Span(code.Position, code.Position + 1), CAError.CA0041);	// Expected ':' to follow conditional result.
				return ret;
			}

			var falseExp = ExpressionNode.Read(p, condStopStrings);
			if (falseExp == null)
			{
				p.Statement.ReportError(new Span(code.Position, code.Position + 1), CAError.CA0043);	// Expected value to follow conditional ':'.
				return ret;
			}

			if (code.ReadExact('?'))
			{
				// Stacked conditional
				var group = new GroupNode(p.Statement);
				group.AddChild(falseExp);
				group.AddChild(ConditionalNode.Read(p, code.Span, stopStrings));
				ret._falseExp = group;
			}
			else
			{
				ret._falseExp = falseExp;
			}

			return ret;
		}

		public override int Precedence
		{
			get
			{
				return 12;
			}
		}

		public override void Simplify(RunScope scope)
		{
			if (Parent == null) throw new InvalidOperationException("Conditional operator must have a parent.");

			var leftScope = scope.Clone(dataTypeContext: DataType.Int);
			var leftNode = Parent.GetLeftSibling(leftScope, this);
			scope.Merge(leftScope);
			if (leftNode == null)
			{
				ReportError(_opSpan, CAError.CA0007, "?");	// Operator '{0}' expects value on left.
				Parent.ReplaceWithResult(new Value(DataType.Int), ResultSource.Conditional1, this);
			}
			else
			{
				var leftValue = leftNode.ReadValue(leftScope);
				if (leftValue.IsVoid) leftNode.ReportError(_opSpan, CAError.CA0007, "?");	// Operator '{0}' expects value on left.

				var value = Value.Empty;
				if (_trueExp != null)
				{
					value = _trueExp.ReadValue(scope);
				}

				if (_falseExp != null)
				{
					var falseValue = _falseExp.ReadValue(scope);
					if (value.IsVoid) value = falseValue;
				}

				Parent.ReplaceWithResult(value, leftNode, this);
			}
		}
	}
}
