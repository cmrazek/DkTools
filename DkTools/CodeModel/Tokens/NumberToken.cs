using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class NumberToken : Token
	{
		private string _text;

		public NumberToken(GroupToken parent, Scope scope, Span span, string text)
			: base(parent, scope, span)
		{
			_text = text;

			ClassifierType = Classifier.ProbeClassifierType.Number;
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("text", _text);
			base.DumpTreeInner(xml);
		}

		public static NumberToken Parse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			var startPos = file.Position;
			file.ParseNumber();

			var span = new Span(startPos, file.Position);
			return new NumberToken(parent, scope, span, file.GetText(span));
		}

		public static NumberToken TryParse(GroupToken parent, Scope scope, bool allowNegative)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;

			var ch = file.PeekChar();
			if (ch == '-')
			{
				if (!allowNegative) return null;
				var nextCh = file.PeekChar(1);
				if (!char.IsDigit(nextCh)) return null;
			}
			else if (!char.IsDigit(ch)) return null;

			var startPos = file.Position;
			file.ParseNumber();
			var span = new Span(startPos, file.Position);
			return new NumberToken(parent, scope, span, file.GetText(span));
		}
	}
}
