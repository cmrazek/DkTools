using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.ErrorTagging;

namespace DkTools.CodeAnalysis
{
	class GroupNode : Node
	{
		private List<Node> _nodes = new List<Node>();

		public GroupNode(Statement stmt, Span? span = null)
			: base(stmt, span.HasValue ? span.Value : Span.Empty)
		{
		}

		public void AddNode(Node node)
		{
			_nodes.Add(node);
			node.Parent = this;

			if (!node.Span.IsEmpty)
			{
				if (Span.IsEmpty) Span = node.Span;
				else Span = Span.Envelope(node.Span);
			}
		}

		public int NumChildren
		{
			get { return _nodes.Count; }
		}

		public IEnumerable<Node> Children
		{
			get { return _nodes; }
		}

		public void ReplaceNodes(Node newNode, params Node[] oldNodes)
		{
			var insertIndex = -1;
			foreach (var node in oldNodes)
			{
				if (node == null) continue;

				var index = _nodes.FindIndex(n => n == node);
				if (insertIndex < 0 || index < insertIndex) insertIndex = index;

				_nodes.Remove(node);
			}

			if (newNode != null)
			{
				if (insertIndex < 0) throw new ArgumentException("Cannot find old nodes to replace.");
				_nodes.Insert(insertIndex, newNode);
			}
		}

		public void ReplaceWithResult(Value value, params Node[] nodes)
		{
			ErrorType? errRep = null;
			Span span = Span.Empty;

			foreach (var node in nodes)
			{
				if (node == null) continue;

				errRep = errRep.Combine(node.ErrorReported);

				if (!node.Span.IsEmpty)
				{
					if (span.IsEmpty) span = node.Span;
					else span = span.Envelope(node.Span);
				}
			}

			ReplaceNodes(new ResultNode(Statement, span, value, errRep), nodes);
		}

		public override void Execute()
		{
			while (true)
			{
				// Find the highest precedence
				var highestPrec = 0;
				foreach (var node in _nodes)
				{
					var prec = node.Precedence;
					if (prec != 0)
					{
						if (highestPrec < prec) highestPrec = prec;
					}
				}

				if (highestPrec == 0) return;
				var numNodesWithPrec = _nodes.Count(n => n.Precedence != 0);

				if (highestPrec % 2 == 0)
				{
					// Left-to-right associativity
					foreach (var node in _nodes)
					{
						if (node.Precedence == highestPrec)
						{
							node.Execute();
							break;
						}
					}
				}
				else
				{
					// Right-to-left associativity
					for (int i = _nodes.Count - 1; i >= 0; i--)
					{
						var node = _nodes[i];
						if (node.Precedence == highestPrec)
						{
							node.Execute();
							break;
						}
					}
				}

				// Sanity check to avoid infinite loop
				var afterNum = _nodes.Count(n => n.Precedence != 0);
				if (afterNum == numNodesWithPrec) throw new CAException("Number of nodes with precedence did not change during iteration.");
			}
		}

		public Node GetLeftSibling(Node node)
		{
			Node last = null;

			foreach (var n in _nodes)
			{
				if (n == node) return last;
				last = n;
			}

			return null;
		}

		public Node GetRightSibling(Node node)
		{
			Node last = null;

			foreach (var n in _nodes)
			{
				if (last == node) return n;
				last = n;
			}

			return null;
		}

		public override Value Value
		{
			get
			{
				Execute();
				if (_nodes.Count == 1) return _nodes[0].Value;

				ReportError(Span, CAError.CA0011);	// Syntax error.
				return Value.Empty;
			}
			set
			{
			}
		}

	}
}
