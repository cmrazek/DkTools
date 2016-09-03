using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class StringLiteralToken : Token
	{
		private string _text;
		private DataType _dataType;

		public StringLiteralToken(Scope scope, Span span, string text)
			: base(scope, span)
		{
			_text = text;

			ClassifierType = Classifier.ProbeClassifierType.StringLiteral;

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
