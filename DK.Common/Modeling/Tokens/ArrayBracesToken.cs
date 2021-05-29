using DK.Code;
using System;
using System.Collections.Generic;

namespace DK.Modeling.Tokens
{
	public class ArrayBracesToken : GroupToken
	{
		private ArrayBraceToken _openToken;
		private ArrayBraceToken _closeToken;
		private List<Token> _innerTokens = new List<Token>();

		internal ArrayBracesToken(Scope scope)
			: base(scope)
		{
		}

		internal static ArrayBracesToken TryParse(Scope scope)
		{
			if (!scope.Code.PeekExact('[')) return null;
			return Parse(scope);
		}

		private static readonly string[] _endTokens = new string[] { "]", "," };

		internal static ArrayBracesToken Parse(Scope scope)
		{
			var code = scope.Code;
			if (!code.ReadExact('[')) throw new InvalidOperationException("ArrayBracesToken.Parse expected next char to be '['.");
			var openBracketSpan = code.Span;

			var indentScope = scope.CloneIndentNonRoot();
			indentScope.Hint |= ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressStatementStarts;

			var ret = new ArrayBracesToken(scope);
			ret.AddToken(ret._openToken = new ArrayBraceToken(scope, openBracketSpan, ret, true));

			while (!code.EndOfFile)
			{
				if (code.ReadExact(']'))
				{
					ret.AddToken(ret._closeToken = new ArrayBraceToken(scope, code.Span, ret, false));
					break;
				}

				if (code.ReadExact(','))
				{
					ret.AddToken(new DelimiterToken(indentScope, code.Span));
					continue;
				}

				var exp = ExpressionToken.TryParse(indentScope, _endTokens);
				if (exp != null) ret.AddToken(exp);
				else break;
			}

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

		public void AddOpen(CodeSpan span)
		{
			AddToken(_openToken = new ArrayBraceToken(Scope, span, this, true));
		}

		public void AddClose(CodeSpan span)
		{
			AddToken(_openToken = new ArrayBraceToken(Scope, span, this, false));
		}
	}
}
