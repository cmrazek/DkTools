using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens.Operators
{
	internal class OperatorToken : Token
	{
		private string _text;

		public OperatorToken(Scope scope, Span span, string text)
			: base(scope, span)
		{
			_text = text;

			ClassifierType = Classifier.ProbeClassifierType.Operator;
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
