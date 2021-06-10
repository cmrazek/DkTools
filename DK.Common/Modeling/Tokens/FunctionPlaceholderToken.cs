using DK.Code;
using DK.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DK.Modeling.Tokens
{
	public class FunctionPlaceholderToken : GroupToken
	{
		private int _bodyStart;
		private int _argsEnd;
		private CodeSpan? _entireFunctionSpan;
		private bool _bodyTokenSearched;
		private BracesToken _bodyToken;

		internal FunctionPlaceholderToken(Scope scope, CodeSpan span, string text, IdentifierToken nameToken, BracketsToken argsToken, FunctionDefinition sourceDef)
			: base(scope)
		{
#if DEBUG
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (argsToken == null) throw new ArgumentNullException("argsToken");
			if (sourceDef == null) throw new ArgumentNullException("sourceDef");
#endif
			AddToken(nameToken);
			AddToken(argsToken);

			nameToken.SourceDefinition = sourceDef;
			_bodyStart = sourceDef.BodyStartPosition;
			_argsEnd = sourceDef.ArgsEndPosition;
		}

		private BracesToken FindBodyToken()
		{
			if (!_bodyTokenSearched)
			{
				_bodyToken = Scope.File.FindDownward(_bodyStart, t => t is BracesToken && t.Span.Start == _bodyStart).LastOrDefault() as BracesToken;
				_bodyTokenSearched = true;
			}
			return _bodyToken;
		}

		public CodeSpan EntireFunctionSpan
		{
			get
			{
				if (!_entireFunctionSpan.HasValue)
				{
					var bodyToken = FindBodyToken();
					if (bodyToken == null) _entireFunctionSpan = Span;
					else _entireFunctionSpan = new CodeSpan(Span.Start, bodyToken.Span.End);
				}
				return _entireFunctionSpan.Value;
			}
		}

		public override IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				foreach (var region in base.OutliningRegions) yield return region;

				var bodyToken = FindBodyToken();
				if (bodyToken != null)
				{
					var startPos = _argsEnd;
					if (startPos < Span.End && startPos > bodyToken.Span.Start) startPos = bodyToken.Span.Start;

					var regionText = Code.GetText(bodyToken.Span);
					if (regionText.Length > Constants.OutliningMaxContextChars)
					{
						regionText = regionText.Substring(0, Constants.OutliningMaxContextChars) + "...";
					}

					yield return new OutliningRegion
					{
						Span = new CodeSpan(startPos, bodyToken.Span.End),
						CollapseToDefinition = true,
						Text = Constants.DefaultOutliningText,
						TooltipText = regionText
					};
				}
			}
		}
	}
}
