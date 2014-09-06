using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class OperatorToken : Token
	{
		private string _text;

		public OperatorToken(GroupToken parent, Scope scope, Span span, string text)
			: base(parent, scope, span)
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

		public static OperatorToken TryParseMatching(GroupToken parent, Scope scope, string text)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;

			var startPos = file.Position;
			if (!file.SkipMatch(text)) return null;

			return new OperatorToken(parent, scope, new Span(startPos, file.Position), text);
		}
	}
}
