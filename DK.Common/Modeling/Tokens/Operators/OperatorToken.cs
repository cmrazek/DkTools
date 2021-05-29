using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class OperatorToken : Token
	{
		private string _text;

		internal OperatorToken(Scope scope, CodeSpan span, string text)
			: base(scope, span)
		{
			_text = text;

			ClassifierType = ProbeClassifierType.Operator;
		}

		public override string Text
		{
			get { return _text; }
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("text", _text);
			base.DumpTreeInner(xml);
		}
	}
}
