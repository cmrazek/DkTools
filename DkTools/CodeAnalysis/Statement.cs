using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeAnalysis
{
	class Statement
	{
		private List<Node> _nodes = new List<Node>();
		private CodeModel.Span _endSpan;

		public bool IsEmpty
		{
			get { return _nodes.Count == 0; }
		}

		public CodeModel.Span EndSpan
		{
			get { return _endSpan; }
			set { _endSpan = value; }
		}

		public void AddNode(Node node)
		{
			_nodes.Add(node);
		}
	}
}
