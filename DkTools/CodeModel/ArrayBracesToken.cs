using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class ArrayBracesToken : GroupToken
	{
		private ArrayBraceToken _openToken;
		private ArrayBraceToken _closeToken;
		private List<Token> _innerTokens = new List<Token>();

		private ArrayBracesToken(GroupToken parent, Scope scope, Position startPos)
			: base(parent, scope, startPos)
		{
		}

		public static ArrayBracesToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;
			if (file.PeekChar() != '[') return null;

			return Parse(parent, scope);
		}

		public static ArrayBracesToken Parse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);
#if DEBUG
			if (file.PeekChar() != '[') throw new InvalidOperationException("ArrayBracesToken.Parse expected next char to be '['.");
#endif
			var startPos = scope.File.Position;
			scope.File.MoveNext();

			var indentScope = scope.CloneIndentNonRoot();
			indentScope.Hint |= ScopeHint.SuppressFunctionDefinition;

			var ret = new ArrayBracesToken(parent, scope, startPos);
			ret._openToken = new ArrayBraceToken(ret, scope, new Span(startPos, scope.File.Position), ret, true);
			ret.AddToken(ret._openToken);

			ret.ParseScope(indentScope, t =>
				{
					if (t is ArrayBraceToken && !(t as ArrayBraceToken).Open)
					{
						ret._closeToken = t as ArrayBraceToken;
						return ParseScopeResult.StopAndKeep;
					}

					ret._innerTokens.Add(t);
					return ParseScopeResult.Continue;
				});

			return ret;
		}

		public ArrayBraceToken OpenToken
		{
			get { return _openToken; }
		}

		public ArrayBraceToken CloseToken
		{
			get { return _closeToken; }
		}

		public IEnumerable<Token> InnerTokens
		{
			get { return _innerTokens; }
		}
	}
}
