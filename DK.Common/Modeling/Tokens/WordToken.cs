using DK.Code;
using System;

namespace DK.Modeling.Tokens
{
	/// <summary>
	/// This class will be the base for all tokens that consist of a single word.
	/// </summary>
	public abstract class WordToken : Token
	{
		private string _text;

		internal WordToken(Scope scope, CodeSpan span, string text)
			: base(scope, span)
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
