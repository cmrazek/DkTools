﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class UnknownToken : Token
	{
		string _text;

		public UnknownToken(GroupToken parent, Scope scope, Span span, string text)
			: base(parent, scope, span)
		{
			_text = text;
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("text", _text);
			base.DumpTreeInner(xml);
		}
	}
}