﻿using System;
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

		public BracesToken(Scope scope)
			: base(scope)
		{
		}

		public BracesToken(Scope scope, Span openBraceSpan)
			: base(scope)
		{
			_openToken = new BraceToken(scope, openBraceSpan, this, true);
			AddToken(_openToken);
		}

		public static BracesToken Parse(Scope scope)
		{
			var code = scope.Code;
			if (!code.ReadExact('{')) throw new InvalidOperationException("BracesToken.Parse expected next char to be '{'.");
			var openBraceSpan = code.Span;

			var indentScope = scope.CloneIndentNonRoot();
			indentScope.Hint |= ScopeHint.SuppressFunctionDefinition;

			var ret = new BracesToken(scope);
			ret._openToken = new BraceToken(scope, openBraceSpan, ret, true);
			ret.AddToken(ret._openToken);

			while (!code.EndOfFile)
			{
				if (code.ReadExact('}'))
				{
					ret._closeToken = new BraceToken(scope, code.Span, ret, false);
					ret.AddToken(ret._closeToken);
					return ret;
				}

				var stmt = StatementToken.TryParse(scope);
				if (stmt != null) ret.AddToken(stmt);
			}

			return ret;
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
					var startLineEnd = Code.FindEndOfLine(_openToken.Span.End);
					if (_closeToken.Span.Start > startLineEnd)
					{
						foreach (var region in base.OutliningRegions) yield return region;

						var regionText = Code.GetText(Span);
						if (regionText.Length > Constants.OutliningMaxContextChars)
						{
							regionText = regionText.Substring(0, Constants.OutliningMaxContextChars) + "...";
						}

						yield return new OutliningRegion
						{
							Span = Span,
							CollapseToDefinition = false,
							Text = Constants.DefaultOutliningText,
							TooltipText = regionText
						};

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

		public void AddOpenBrace(Span span)
		{
#if DEBUG
			if (_openToken != null) throw new InvalidOperationException("Opening brace has already been added.");
#endif
			AddToken(_openToken = new BraceToken(Scope, span, this, true));
		}

		public void AddCloseBrace(Span span)
		{
#if DEBUG
			if (_closeToken != null) throw new InvalidOperationException("Close brace has already been added.");
#endif
			AddToken(_closeToken = new BraceToken(Scope, span, this, false));
		}
	}
}
