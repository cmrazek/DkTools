using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class PreprocessorToken : Token
	{
		private string _text;
		private string _instructions;

		public PreprocessorToken(Scope scope, Span span, string text)
			: base(scope, span)
		{
			_text = text;

			ClassifierType = Classifier.ProbeClassifierType.Preprocessor;
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
