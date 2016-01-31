using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class ArrayBraceToken : Token, IBraceMatchingToken
	{
		private ArrayBracesToken _braces;
		private bool _open;

		public ArrayBraceToken(Scope scope, Span span, ArrayBracesToken braces, bool open)
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
