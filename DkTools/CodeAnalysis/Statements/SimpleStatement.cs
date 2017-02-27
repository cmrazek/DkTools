using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;

namespace DkTools.CodeAnalysis.Statements
{
	class SimpleStatement : Statement
	{
		private GroupNode _root;

		public SimpleStatement(CodeAnalyzer ca)
			: base(ca)
		{
			_root = new GroupNode(this);
		}

		public int NumChildren
		{
			get { return _root.NumChildren; }
		}

		public bool IsEmpty
		{
			get { return _root.NumChildren == 0; }
		}

		public void AddNode(Node node)
		{
			_root.AddChild(node);

			if (Span.IsEmpty) Span = _root.Span;
			else if (!_root.Span.IsEmpty) Span = Span.Envelope(_root.Span);
		}

		public override void Execute(RunScope scope)
		{
			base.Execute(scope);

			if (_root.NumChildren > 0) _root.ReadValue(scope);
		}
	}
}
