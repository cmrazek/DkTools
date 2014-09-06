using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	/// <summary>
	/// A token that exists outside the model, and is only referenced by filename/span.
	/// </summary>
	internal class ExternalToken : Token
	{
		private string _fileName;

		public ExternalToken(string fileName, Span span)
			: base(null, new Scope(), span)
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
