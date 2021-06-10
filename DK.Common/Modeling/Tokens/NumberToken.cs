using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class NumberToken : Token
	{
		private string _text;

		internal NumberToken(Scope scope, CodeSpan span, string text)
			: base(scope, span)
		{
			_text = text;

			ClassifierType = ProbeClassifierType.Number;
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("text", _text);
			base.DumpTreeInner(xml);
		}

		public override DataType ValueDataType
		{
			get
			{
				return DataType.Numeric;
			}
		}
	}
}
