using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;

namespace DK.CodeAnalysis.Nodes
{
	class CharLiteralNode : TextNode
	{
		private char? _char;

		public CharLiteralNode(Statement stmt, CodeSpan span, string text)
			: base(stmt, DataType.Char, span, text)
		{
			if (text != null && text.Length >= 1) _char = text[0];
		}

		public override void Execute(CAScope scope) { }
		public override bool IsReportable => true;
		public override Value ReadValue(CAScope scope) => new CharValue(DataType.Char, _char);
		public override string ToString() => string.Concat("'", _char.ToString(), "'");
	}
}
