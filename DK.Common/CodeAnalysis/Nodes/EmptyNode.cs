using DK.Code;
using DK.CodeAnalysis.Statements;

namespace DK.CodeAnalysis.Nodes
{
	class EmptyNode : Node
	{
		public EmptyNode(Statement stmt)
			: base(stmt, null, CodeSpan.Empty)
		{
		}

		public override bool IsReportable => false;
		public override string ToString() => "(empty)";
	}
}
