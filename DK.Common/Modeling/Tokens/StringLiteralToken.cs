using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class StringLiteralToken : Token
	{
		private string _text;
		private DataType _dataType;

		internal StringLiteralToken(Scope scope, CodeSpan span, string text)
			: base(scope, span)
		{
			_text = text;

			ClassifierType = ProbeClassifierType.StringLiteral;

			if (text.StartsWith("'")) _dataType = DataType.Char;
			else _dataType = DataType.String;
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
				return _dataType;
			}
		}
	}
}
