using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class PreprocessorToken : Token
	{
		private string _text;
		private string _instructions;

		internal PreprocessorToken(Scope scope, CodeSpan span, string text)
			: base(scope, span)
		{
			_text = text;

			ClassifierType = ProbeClassifierType.Preprocessor;
		}

		public override string Text
		{
			get { return _text; }
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("text", _text);
			if (!string.IsNullOrWhiteSpace(_instructions)) xml.WriteAttributeString("instructions", _instructions);
			base.DumpTreeInner(xml);
		}

		public string Instructions
		{
			get { return _instructions; }
			set { _instructions = value; }
		}
	}
}
