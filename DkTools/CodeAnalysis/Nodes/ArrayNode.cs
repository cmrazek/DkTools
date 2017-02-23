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
		private List<GroupNode> _indexNodes = new List<GroupNode>();

		private ArrayNode(Statement stmt, Span openBracketSpan)
			: base(stmt, openBracketSpan)
		{
		}

		public static ArrayNode Read(ReadParams p, Span openSpan)
		{
			var ret = new ArrayNode(p.Statement, openSpan);
			var code = p.Code;

			while (!code.EndOfFile)
			{
				if (code.ReadExact(']'))
				{
					ret.Span = ret.Span.Envelope(code.Span);
					break;
				}
				if (code.ReadExact(',')) continue;
				if (code.ReadExact(';')) break;

				var exp = ExpressionNode.Read(p, "]", ",", ";");
				if (exp != null)
				{
					ret._indexNodes.Add(exp);
					ret.IncludeNodeInSpan(exp);
				}
			}

			if (ret._indexNodes.Count == 0 || ret._indexNodes.Count > 2)
			{
				ret._indexNodes[2].ReportError(CAError.CA0022);	// Only 1 or 2 index accessors allowed.
			}

			return ret;
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
			var leftNode = Parent.GetLeftSibling(scope, this) as IdentifierNode;
			if (leftNode == null)
			{
				ReportError(Span, CAError.CA0020);	// Array indexer requires variable on left.
				Parent.ReplaceWithResult(new Value(DataType.Void), this);
			}

			// Read the array indexer value
			foreach (var ix in _indexNodes)
			{
				ix.ReadValue(scope.Clone(dataTypeContext: DataType.Int));
			}

			// Combine with the variable on left to produce the result
			var result = new ArrayAccessorNode(Statement, Span.Envelope(leftNode.Span), leftNode.GetDefinition(scope), leftNode.Text);
			Parent.ReplaceNodes(result, leftNode, this);
		}
	}
}
