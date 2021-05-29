using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.Modeling;

namespace DK.CodeAnalysis.Nodes
{
	abstract class TextNode : Node
	{
		private string _text;

		public TextNode(Statement stmt, DataType dataType, CodeSpan span, string text)
			: base(stmt, dataType, span)
		{
			_text = text;
		}

		public string Text => _text;
		public override string ToString() => _text;
	}
}
