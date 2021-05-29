using DK.Code;
using DK.Syntax;
using System.Collections.Generic;

namespace DK.Modeling.Tokens
{
	public class ArrayBraceToken : Token, IBraceMatchingToken
	{
		private ArrayBracesToken _braces;
		private bool _open;

		internal ArrayBraceToken(Scope scope, CodeSpan span, ArrayBracesToken braces, bool open)
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
				var ret = new List<Token>();
				ret.Add(_braces.OpenToken);
				if (_braces.CloseToken != null) ret.Add(_braces.CloseToken);
				return ret;
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
