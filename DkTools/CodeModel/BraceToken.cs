using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class BraceToken : Token, IBraceMatchingToken
	{
		private BracesToken _braces;
		private bool _open;

		public BraceToken(GroupToken parent, Scope scope, Span span, BracesToken braces, bool open)
			: base(parent, scope, span)
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

		public static BraceToken TryParseClose(GroupToken parent, Scope scope, BracesToken bracesToken)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;
			if (file.PeekChar() != '}') return null;

			return new BraceToken(parent, scope, file.MoveNextSpan(1), bracesToken, false);
		}
	}
}
