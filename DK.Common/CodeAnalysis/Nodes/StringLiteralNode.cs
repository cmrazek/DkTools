using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;

namespace DK.CodeAnalysis.Nodes
{
	class StringLiteralNode : TextNode
	{
		public StringLiteralNode(Statement stmt, CodeSpan span, string text)
			: base(stmt, DataType.String, span, text)
		{
		}

		public override void Execute(CAScope scope) { }
		public override bool IsReportable => true;
		public override Value ReadValue(CAScope scope) => new StringValue(DataType.String, CodeParser.StringLiteralToString(Text));
		public override string ToString() => $"\"{Text}\"";
	}
}
