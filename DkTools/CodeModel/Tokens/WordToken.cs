using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	/// <summary>
	/// This class will be the base for all tokens that consist of a single word.
	/// </summary>
	internal abstract class WordToken : Token
	{
		private string _text;

		public WordToken(GroupToken parent, Scope scope, Span span, string text)
			: base(parent, scope, span)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException("text");
#endif

			_text = text;
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
