using DK.Code;
using DK.CodeAnalysis.Statements;

namespace DK.CodeAnalysis.Nodes
{
	class UnknownNode : TextNode
	{
		public UnknownNode(Statement stmt, CodeSpan span, string text)
			: base(stmt, null, span, text)
		{
		}

		public override bool IsReportable => false;
		public override string ToString() => "(unknown)";
	}
}
