using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class StringLiteralToken : Token
	{
		private string _text;

		public StringLiteralToken(Scope scope, Span span, string text)
			: base(scope, span)
		{
			_text = text;

			ClassifierType = Classifier.ProbeClassifierType.StringLiteral;
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("text", _text);
			base.DumpTreeInner(xml);
		}
	}
}
