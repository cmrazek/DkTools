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

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			if (_root.NumChildren > 0)
			{
				var exeScope = scope.Clone();
				exeScope.RemoveHeaderString = true;
				_root.Execute(exeScope);
				scope.Merge(exeScope);

				if (_root.IsReportable)
				{
					var readScope = scope.Clone();
					readScope.RemoveHeaderString = true;
					var rootValue = _root.ReadValue(readScope);
					if (!rootValue.IsVoid)
					{
						if (ProbeToolsPackage.Instance.EditorOptions.HighlightReportOutput)
						{
							ReportError(_root.Span, CAError.CA0070);    // This expression writes to the report stream.
						}
					}
					scope.Merge(readScope);
				}
			}
		}
	}
}
