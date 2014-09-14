using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class InsertToken : GroupToken
	{
		private InsertStartToken _startToken;
		private InsertEndToken _endToken;  // Optional

		private InsertToken(GroupToken parent, Scope scope, int startPos)
			: base(parent, scope, startPos)
		{
		}

		public static Token Parse(GroupToken parent, Scope scope, InsertStartToken insertToken)
		{
			var file = scope.File;

			var ret = new InsertToken(parent, scope, file.Position);

			var scopeIndent = scope.CloneIndent();
			ret.AddToken(ret._startToken = insertToken);

			ret.ParseScope(scope, t =>
				{
					if (t is InsertEndToken)
					{
						ret._endToken = t as InsertEndToken;
						return ParseScopeResult.StopAndKeep;
					}
					return ParseScopeResult.Continue;
				});

			return ret;
		}

		public override IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				if (_startToken != null && _endToken != null)
				{
					var startLineEnd = File.FindEndOfLine(_startToken.Span.End);
					var endPrevLine = File.FindEndOfPreviousLine(_endToken.Span.Start);
					if (endPrevLine > startLineEnd)
					{
						var span = new Span(startLineEnd, endPrevLine);
						return new OutliningRegion[]
						{
							new OutliningRegion
							{
								Span = span,
								TooltipText = File.GetRegionText(span)
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

	internal class InsertStartToken : Token, IBraceMatchingToken
	{
		public InsertStartToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Preprocessor;
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

	internal class InsertEndToken : Token, IBraceMatchingToken
	{
		public InsertEndToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Preprocessor;
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

		public static InsertEndToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;

			var startPos = file.Position;
			if (!file.SkipMatch("#endinsert")) return null;

			return new InsertEndToken(parent, scope, new Span(startPos, file.Position));
		}
	}
}
