using DK.Code;
using DK.Syntax;
using System.Collections.Generic;

namespace DK.Modeling.Tokens
{
	public class BraceToken : Token, IBraceMatchingToken
	{
		private BracesToken _braces;
		private bool _open;

		internal BraceToken(Scope scope, CodeSpan span, BracesToken braces, bool open)
			: base(scope, span)
		{
			_braces = braces;
			_open = open;
			ClassifierType = ProbeClassifierType.Operator;
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
