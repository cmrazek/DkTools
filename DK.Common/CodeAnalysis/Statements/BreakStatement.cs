using DK.Code;

namespace DK.CodeAnalysis.Statements
{
	class BreakStatement : Statement
	{
		public BreakStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			if (!p.Code.ReadExact(';')) ReportError(p.Code.Span, CAError.CA10015);	// Expected ';'.
		}

		public override string ToString() => "break";

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			if (!scope.CanBreak)
			{
				ReportError(Span, CAError.CA10023);	// 'break' is not valid here.
				return;
			}

			scope.Breaked = TriState.True;
		}
	}
}
