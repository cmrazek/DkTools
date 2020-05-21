﻿using System;
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
			_root = new GroupNode(this, null);
		}

		public bool IsEmpty => _root.NumChildren == 0;
		public int NumChildren => _root.NumChildren;
		public override string ToString() => _root.ToString();

		public void AddNode(Node node)
		{
			_root.AddChild(node);

			if (Span.IsEmpty) Span = _root.Span;
			else if (!_root.Span.IsEmpty) Span = Span.Envelope(_root.Span);
		}

		public override void Execute(RunScope scope)
		{
			base.Execute(scope);

			if (_root.NumChildren > 0)
			{
				_root.Execute(scope);

				if (_root.IsReportable)
				{
					var readScope = scope.Clone();
					readScope.RemoveHeaderString = true;
					_root.ReadValue(readScope);
					scope.Merge(readScope);
				}
			}
		}
	}
}
