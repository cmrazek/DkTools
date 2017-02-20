using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeAnalysis
{
	class Statement
	{
		private CodeAnalyzer _ca;
		private GroupNode _nodes;
		private CodeModel.Span _endSpan;

		public Statement(CodeAnalyzer ca)
		{
			_ca = ca;
			_nodes = new GroupNode(this);
		}

		public bool IsEmpty
		{
			get { return _nodes.NumChildren == 0; }
		}

		public CodeModel.Span EndSpan
		{
			get { return _endSpan; }
			set { _endSpan = value; }
		}

		public void AddNode(Node node)
		{
			_nodes.AddNode(node);
		}

		public void Execute()
		{
			_nodes.Execute();
			var val = _nodes.Value;	// To simulate a 'read' of the expression
		}

		public CodeAnalyzer CodeAnalyzer
		{
			get { return _ca; }
		}

		public void ReplaceNodes(Node newNode, params Node[] oldNodes)
		{
			_nodes.ReplaceNodes(newNode, oldNodes);
		}
	}
}
