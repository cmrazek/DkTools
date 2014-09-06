using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class StringLiteralToken : Token
	{
		private string _text;

		public StringLiteralToken(GroupToken parent, Scope scope, Span span, string text)
			: base(parent, scope, span)
		{
			_text = text;

			ClassifierType = Classifier.ProbeClassifierType.StringLiteral;
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("text", _text);
			base.DumpTreeInner(xml);
		}

		public static StringLiteralToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;
			switch (file.PeekChar())
			{
				case '\"':
				case '\'':
					break;
				default:
					return null;
			}

			var startPos = file.Position;
			file.ParseStringLiteral();
			var span = new Span(startPos, file.Position);
			return new StringLiteralToken(parent, scope, span, file.GetText(span));
		}
	}
}
