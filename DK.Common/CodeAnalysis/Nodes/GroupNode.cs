using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Nodes
{
	class GroupNode : Node
	{
		private List<Node> _nodes = new List<Node>();

		public GroupNode(Statement stmt, DataType dataType, CodeSpan? span = null)
			: base(stmt, dataType, span.HasValue ? span.Value : CodeSpan.Empty)
		{
		}

		public override string ToString() => string.Concat("(", _nodes.Select(n => n.ToString()).Combine(" "), ")");

		public void AddChild(Node node)
		{
			_nodes.Add(node);
			OnNodeAdded(node);
		}

		private void OnNodeAdded(Node node)
		{
			node.Parent = this;
			IncludeNodeInSpan(node);
		}

		public void RemoveChild(Node node)
		{
			if (_nodes.Remove(node))
			{
				if (node.Parent == this) node.Parent = null;
			}
		}

		protected void IncludeNodeInSpan(Node node)
		{
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
				OnNodeAdded(newNode);
			}
		}

		public void ReplaceWithResult(Value value, bool resultIsReportable, params Node[] nodes)
		{
			ReplaceWithResult(value, resultIsReportable, ResultSource.Normal, nodes);
		}

		public void ReplaceWithResult(Value value, bool resultIsReportable, ResultSource source, params Node[] nodes)
		{
			CAErrorType? errRep = null;
			CodeSpan span = CodeSpan.Empty;

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

			ReplaceNodes(new ResultNode(Statement, span, value, source, errRep, resultIsReportable), nodes);
		}

		private void SimplifyGroup(CAScope scope)
		{
			var reduceRetries = 3;

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
							node.Simplify(scope);
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
							node.Simplify(scope);
							break;
						}
					}
				}

				// Sanity check to avoid infinite loop
				var afterNum = _nodes.Count(n => n.Precedence != 0);
				if (afterNum == numNodesWithPrec)
				{
					if (--reduceRetries == 0) throw new CAException("Number of nodes with precedence did not change during iteration.");
				}
				else
				{
					reduceRetries = 3;
				}
			}
		}

		public Node GetLeftSibling(CAScope scope, Node node)
		{
			Node last = null;

			foreach (var n in _nodes)
			{
				if (n == node) return last;
				last = n;
			}

			return null;
		}

		public Node GetRightSibling(CAScope scope, Node node)
		{
			Node last = null;

			foreach (var n in _nodes)
			{
				if (last == node) return n;
				last = n;
			}

			return null;
		}

		public override void Execute(CAScope scope)
		{
			SimplifyGroup(scope);

			if (_nodes.Count == 2 && scope.RemoveHeaderString)
			{
				var lastNode = _nodes.Last() as StringLiteralNode;
				if (lastNode != null)
				{
					RemoveChild(lastNode);
					scope.RemoveHeaderString = false;
				}
			}

			if (_nodes.Count == 1)
			{
				_nodes[0].Execute(scope);
				return;
			}

			ReportError(Span, CAError.CA0101);  // Syntax error.
		}

		public override Value ReadValue(CAScope scope)
		{
			SimplifyGroup(scope);

			if (_nodes.Count == 1)
			{
				return _nodes[0].ReadValue(scope);
			}

			ReportError(Span, CAError.CA0101);  // Syntax error.
			return Value.Void;
		}

		public override void WriteValue(CAScope scope, Value value)
		{
			SimplifyGroup(scope);
			if (_nodes.Count == 1)
			{
				_nodes[0].WriteValue(scope, value);
			}
			else
			{
				base.WriteValue(scope, value);
			}
		}

		public Node FirstChild
		{
			get { return _nodes.Count > 0 ? _nodes[0] : null; }
		}

		public Node LastChild
		{
			get { return _nodes.Count > 0 ? _nodes[_nodes.Count - 1] : null; }
		}

		public override DataType DataType
		{
			get
			{
				foreach (var node in _nodes)
				{
					var dt = node.DataType;
					if (dt != null) return dt;
				}
				return null;
			}
		}

		public override bool IsReportable
		{
			get => _nodes.Count > 0 ? _nodes[0].IsReportable : false;
			set { if (_nodes.Count > 0) _nodes[0].IsReportable = value; }
		}
	}
}
