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

		public ArrayBraceToken(GroupToken parent, Scope scope, Span span, ArrayBracesToken braces, bool open)
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

		public static ArrayBraceToken TryParseClose(GroupToken parent, Scope scope, ArrayBracesToken bracesToken)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;
			if (file.PeekChar() != ']') return null;

			return new ArrayBraceToken(parent, scope, file.MoveNextSpan(1), bracesToken, false);
		}
	}
}
