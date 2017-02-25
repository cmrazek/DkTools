using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class BreakStatement : Statement
	{
		public BreakStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			if (!p.Code.ReadExact(';')) ReportError(p.Code.Span, CAError.CA0015);	// Expected ';'.
		}

		public override void Execute(RunScope scope)
		{
			if (!scope.CanBreak)
			{
				ReportError(Span, CAError.CA0023);	// 'break' is not valid here.
				return;
			}

			scope.Breaked = true;
			base.Execute(scope);
		}
	}
}
