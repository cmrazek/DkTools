using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class BracketsToken : GroupToken
	{
		private List<Token> _innerTokens = new List<Token>();
		private BracketToken _openToken;
		private BracketToken _closeToken;

		private BracketsToken(GroupToken parent, Scope scope, int startPos)
			: base(parent, scope, startPos)
		{
		}

		/// <summary>
		/// Attempts to parse a set of brackets from the file.
		/// </summary>
		/// <param name="parent">Parent token</param>
		/// <param name="scope">Current scope</param>
		/// <returns>If the next token is a set of brackets, the new token; otherwise null.</returns>
		public static BracketsToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);

			if (file.PeekChar() != '(') return null;

			return Parse(parent, scope);
		}

		private static readonly string[] _endTokens = new string[] { ")" };

		public static BracketsToken Parse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);
#if DEBUG
			if (file.PeekChar() != '(') throw new InvalidOperationException("BracketsToken.Parse expected next char to be '('.");
#endif
			var startPos = scope.File.Position;
			scope.File.MoveNext();

			var indentScope = scope.CloneIndentNonRoot();
			indentScope.Hint |= ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressControlStatements;

			var ret = new BracketsToken(parent, scope, startPos);
			ret.AddToken(ret._openToken = new OpenBracketToken(ret, scope, new Span(startPos, scope.File.Position), ret));

			while (file.SkipWhiteSpaceAndComments(indentScope))
			{
				if (file.IsMatch(')'))
				{
					ret.AddToken(ret._closeToken = new CloseBracketToken(ret, scope, file.MoveNextSpan(), ret));
					break;
				}

				var exp = ExpressionToken.TryParse(ret, indentScope, _endTokens);
				if (exp != null) ret.AddToken(exp);
			}

			return ret;

			// TODO: remove
			//ret.ParseScope(indentScope, t =>
			//	{
			//		if (t is CloseBracketToken)
			//		{
			//			ret._closeToken = t as CloseBracketToken;
			//			return ParseScopeResult.StopAndKeep;
			//		}

			//		ret._innerTokens.Add(t);
			//		return ParseScopeResult.Continue;
			//	});

			//return ret;
		}

		public BracketToken OpenToken
		{
			get { return _openToken; }
		}

		public BracketToken CloseToken
		{
			get { return _closeToken; }
		}

		public override DataType ValueDataType
		{
			get
			{
				if (SubTokens.Count() == 1) return SubTokens.First().ValueDataType;
				return base.ValueDataType;
			}
		}

		public override string NormalizedText
		{
			get
			{
				return string.Concat("(", Token.GetNormalizedText(_innerTokens), ")");
			}
		}

		public IEnumerable<Token> InnerTokens
		{
			get { return _innerTokens; }
		}
	}

	internal abstract class BracketToken : Token, IBraceMatchingToken
	{
		private BracketsToken _bracketsToken = null;

		public BracketToken(GroupToken parent, Scope scope, Span span, BracketsToken bracketsToken)
			: base(parent, scope, span)
		{
			_bracketsToken = bracketsToken;
			ClassifierType = Classifier.ProbeClassifierType.Operator;
		}

		IEnumerable<Token> IBraceMatchingToken.BraceMatchingTokens
		{
			get
			{
				yield return _bracketsToken.OpenToken;
				if (_bracketsToken.CloseToken != null) yield return _bracketsToken.CloseToken;
			}
		}
	}

	internal class OpenBracketToken : BracketToken
	{
		public OpenBracketToken(GroupToken parent, Scope scope, Span span, BracketsToken bracketsToken)
			: base(parent, scope, span, bracketsToken)
		{
		}
	}

	internal class CloseBracketToken : BracketToken
	{
		public CloseBracketToken(GroupToken parent, Scope scope, Span span, BracketsToken bracketsToken)
			: base(parent, scope, span, bracketsToken)
		{
		}

		public static CloseBracketToken TryParse(GroupToken parent, Scope scope, BracketsToken bracketsToken)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;
			if (file.PeekChar() != '}') return null;

			return new CloseBracketToken(parent, scope, file.MoveNextSpan(), bracketsToken);
		}
	}
}
