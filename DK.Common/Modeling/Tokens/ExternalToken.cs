using DK.Code;
using System;

namespace DK.Modeling.Tokens
{
	/// <summary>
	/// A token that exists outside the model, and is only referenced by filename/span.
	/// </summary>
	public class ExternalToken : Token
	{
		private string _fileName;

		public ExternalToken(string fileName, CodeSpan span)
			: base(new Scope(), span)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");
#endif
			_fileName = fileName;
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("fileName", _fileName);
			xml.WriteAttributeString("span", Span.ToString());
			base.DumpTreeInner(xml);
		}
	}
}
