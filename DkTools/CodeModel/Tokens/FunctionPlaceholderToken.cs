using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class FunctionPlaceholderToken : WordToken
	{
		private Position _bodyStart;
		private Position _argsEnd;
		private Span? _entireFunctionSpan;
		private bool _bodyTokenSearched;
		private BracesToken _bodyToken;

		public FunctionPlaceholderToken(GroupToken parent, Scope scope, Span span, string text, FunctionDefinition sourceDef)
			: base(parent, scope, span, text)
		{
			SourceDefinition = sourceDef;
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

		public Span EntireFunctionSpan
		{
			get
			{
				if (!_entireFunctionSpan.HasValue)
				{
					var bodyToken = FindBodyToken();
					if (bodyToken == null) _entireFunctionSpan = Span;
					else _entireFunctionSpan = new Span(Span.Start, bodyToken.Span.End);
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

					yield return new OutliningRegion
					{
						Span = new Span(startPos, bodyToken.Span.End),
						CollapseToDefinition = true,
						Text = Constants.DefaultOutliningText,
						TooltipText = File.GetRegionText(bodyToken.Span)
					};
				}
			}
		}
	}
}
