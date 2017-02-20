using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	class GroupNode : Node
	{
		private List<Node> _nodes = new List<Node>();

		public GroupNode(Span? span = null)
			: base(span.HasValue ? span.Value : Span.Empty)
		{
		}

		public void AddNode(Node node)
		{
			_nodes.Add(node);
			if (Span.IsEmpty) Span = node.Span;
			else Span = Span.Envelope(node.Span);
		}
	}
}
