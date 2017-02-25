using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class ContinueStatement : Statement
	{
		public ContinueStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			if (!p.Code.ReadExact(';')) ReportError(p.Code.Span, CAError.CA0015);	// Expected ';'.
		}

		public override void Execute(RunScope scope)
		{
			base.Execute(scope);

			if (!scope.CanContinue)
			{
				ReportError(Span, CAError.CA0024);	// 'continue' is not valid here.
				return;
			}

			scope.Continued = TriState.True;
		}
	}
}
