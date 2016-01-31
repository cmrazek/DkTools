using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class BracesToken : GroupToken
	{
		private Token _openToken;
		private Token _closeToken;
		private List<Token> _innerTokens = new List<Token>();

		private BracesToken(GroupToken parent, Scope scope, int startPos)
			: base(parent, scope, startPos)
		{
			IsLocalScope = true;
		}

		public BracesToken(GroupToken parent, Scope scope, Span openBraceSpan)
			: base(parent, scope, openBraceSpan.Start)
		{
			_openToken = new BraceToken(this, scope, openBraceSpan, this, true);
			AddToken(_openToken);
		}

		public static BracesToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope) || file.PeekChar() != '{') return null;
			return Parse(parent, scope);
		}

		public static BracesToken Parse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);
#if DEBUG
			if (file.PeekChar() != '{') throw new InvalidOperationException("BracesToken.Parse expected next char to be '{'.");
#endif
			var startPos = scope.File.Position;
			scope.File.MoveNext();

			var indentScope = scope.CloneIndentNonRoot();
			indentScope.Hint |= ScopeHint.SuppressFunctionDefinition;

			var ret = new BracesToken(parent, scope, startPos);
			ret._openToken = new BraceToken(ret, scope, new Span(startPos, scope.File.Position), ret, true);
			ret.AddToken(ret._openToken);

			while (file.SkipWhiteSpaceAndComments(scope))
			{
				if (file.IsMatch('}'))
				{
					ret._closeToken = new BraceToken(ret, scope, file.MoveNextSpan(), ret, false);
					ret.AddToken(ret._closeToken);
					return ret;
				}

				var stmt = StatementToken.TryParse(ret, scope);
				if (stmt != null) ret.AddToken(stmt);
			}

			return ret;

			// TODO: remove
			//ret.ParseScope(indentScope, t =>
			//	{
			//		if (t is BraceToken && !(t as BraceToken).Open)
			//		{
			//			ret._closeToken = t as BraceToken;
			//			return ParseScopeResult.StopAndKeep;
			//		}

			//		ret._innerTokens.Add(t);
			//		return ParseScopeResult.Continue;
			//	});

			//return ret;
		}

		public Token OpenToken
		{
			get { return _openToken; }
		}

		public Token CloseToken
		{
			get { return _closeToken; }
		}

		public override IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				if (_openToken != null && _closeToken != null)
				{
					var startLineEnd = File.FindEndOfLine(_openToken.Span.End);
					if (_closeToken.Span.Start > startLineEnd)
					{
						foreach (var region in base.OutliningRegions) yield return region;

						//if (!(Parent is FunctionToken) && !(Parent is DefineToken))
						//{
							yield return new OutliningRegion
							{
								Span = Span,
								CollapseToDefinition = false,
								Text = Constants.DefaultOutliningText,
								TooltipText = File.GetRegionText(Span)
							};
						//}

						yield break;
					}
				}

				foreach (var region in base.OutliningRegions) yield return region;
			}
		}

		public IEnumerable<Token> InnerTokens
		{
			get { return _innerTokens; }
		}

		public void AddCloseBrace(Scope scope, Span span)
		{
#if DEBUG
			if (_closeToken != null) throw new InvalidOperationException("Close brace has already been added.");
#endif
			_closeToken = new BraceToken(this, scope, span, this, false);
			AddToken(_closeToken);
		}
	}
}
