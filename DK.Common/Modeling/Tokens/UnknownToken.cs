using DK.Code;

namespace DK.Modeling.Tokens
{
	public class UnknownToken : Token
	{
		string _text;

		internal UnknownToken(Scope scope, CodeSpan span, string text)
			: base(scope, span)
		{
			_text = text;
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("text", _text);
			base.DumpTreeInner(xml);
		}
	}
}
