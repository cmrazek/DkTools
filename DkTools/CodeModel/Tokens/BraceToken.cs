using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class BraceToken : Token, IBraceMatchingToken
	{
		private BracesToken _braces;
		private bool _open;

		public BraceToken(Scope scope, Span span, BracesToken braces, bool open)
			: base(scope, span)
		{
			_braces = braces;
			_open = open;
			ClassifierType = Classifier.ProbeClassifierType.Operator;
		}

		IEnumerable<Token> IBraceMatchingToken.BraceMatchingTokens
		{
			get
			{
				yield return _braces.OpenToken;
				if (_braces.CloseToken != null) yield return _braces.CloseToken;
			}
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("open", _open.ToString());
			base.DumpTreeInner(xml);
		}

		public bool Open
		{
			get { return _open; }
		}
	}
}
