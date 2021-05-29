using DK.Code;
using DK.Syntax;
using System.Collections.Generic;

namespace DK.Modeling.Tokens
{
	public class InsertToken : GroupToken
	{
		private InsertStartToken _startToken;
		private InsertEndToken _endToken;  // Optional

		private InsertToken(Scope scope)
			: base(scope)
		{
		}

		internal static Token Parse(Scope scope, InsertStartToken insertToken)
		{
			var code = scope.Code;

			var ret = new InsertToken(scope);

			var scopeIndent = scope.CloneIndent();
			ret.AddToken(ret._startToken = insertToken);

			var done = false;
			while (!done && !code.EndOfFile)
			{
				var token = StatementToken.TryParse(scopeIndent, t =>
				{
					if (t is InsertEndToken)
					{
						ret._endToken = t as InsertEndToken;
						done = true;
					}
				});
				if (token != null) ret.AddToken(token);
			}

			return ret;
		}

		public override IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				if (_startToken != null && _endToken != null)
				{
					var startLineEnd = Scope.Code.FindEndOfLine(_startToken.Span.End);
					var endPrevLine = Scope.Code.FindEndOfPreviousLine(_endToken.Span.Start);
					if (endPrevLine > startLineEnd)
					{
						var span = new CodeSpan(startLineEnd, endPrevLine);
						
						var regionText = Code.GetText(span);
						if (regionText.Length > Constants.OutliningMaxContextChars)
						{
							regionText = regionText.Substring(0, Constants.OutliningMaxContextChars) + "...";
						}

						return new OutliningRegion[]
						{
							new OutliningRegion
							{
								Span = span,
								TooltipText = regionText
							}
						};
					}
				}

				return new OutliningRegion[0];
			}
		}

		public InsertStartToken StartToken
		{
			get { return _startToken; }
		}

		public InsertEndToken EndToken
		{
			get { return _endToken; }
		}
	}

	public class InsertStartToken : Token, IBraceMatchingToken
	{
		internal InsertStartToken(Scope scope, CodeSpan span)
			: base(scope, span)
		{
			ClassifierType = ProbeClassifierType.Preprocessor;
		}

		IEnumerable<Token> IBraceMatchingToken.BraceMatchingTokens
		{
			get
			{
				var parent = Parent as InsertToken;
				yield return parent.StartToken;
				if (parent.EndToken != null) yield return parent.EndToken;
			}
		}
	}

	public class InsertEndToken : Token, IBraceMatchingToken
	{
		internal InsertEndToken(Scope scope, CodeSpan span)
			: base(scope, span)
		{
			ClassifierType = ProbeClassifierType.Preprocessor;
		}

		IEnumerable<Token> IBraceMatchingToken.BraceMatchingTokens
		{
			get
			{
				var parent = Parent as InsertToken;
				yield return parent.StartToken;
				if (parent.EndToken != null) yield return parent.EndToken;
			}
		}

		internal static InsertEndToken TryParse(Scope scope)
		{
			var code = scope.Code;

			var startPos = code.Position;
			if (!code.ReadExactWholeWord("#endinsert")) return null;

			return new InsertEndToken(scope, code.Span);
		}
	}
}
