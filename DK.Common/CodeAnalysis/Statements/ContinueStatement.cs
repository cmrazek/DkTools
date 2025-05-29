using DK.Code;

namespace DK.CodeAnalysis.Statements
{
	class ContinueStatement : Statement
	{
		public ContinueStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			if (!p.Code.ReadExact(';')) ReportError(p.Code.Span, CAError.CA10015);	// Expected ';'.
		}

		public override string ToString() => "continue";

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			if (!scope.CanContinue)
			{
				ReportError(Span, CAError.CA10024);	// 'continue' is not valid here.
				return;
			}

			scope.Continued = TriState.True;
		}
	}
}
