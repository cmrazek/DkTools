using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class ArrayNode : GroupNode
	{
		public ArrayNode(Statement stmt, Span openBracketSpan)
			: base(stmt, openBracketSpan)
		{
		}

		public override int Precedence
		{
			get
			{
				return 100;
			}
		}

		public override void Simplify(RunScope scope)
		{
			var leftNode = Parent.GetLeftSibling(this) as IdentifierNode;
			if (leftNode == null)
			{
				ReportError(Span, CAError.CA0020);	// Array indexer requires variable on left.
				Parent.ReplaceWithResult(new Value(DataType.Void), this);
			}

			// Read the array indexer value
			ReadValue(scope);

			// Combine with the variable on left to produce the result
			var result = new ArrayAccessorNode(Statement, Span.Envelope(leftNode.Span), leftNode.Definition, leftNode.Text);
			Parent.ReplaceNodes(result, leftNode, this);
		}
	}
}
