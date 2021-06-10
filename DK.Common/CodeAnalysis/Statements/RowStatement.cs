using DK.Code;

namespace DK.CodeAnalysis.Statements
{
	class RowStatement : Statement
	{
		public RowStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;

			if (!code.ReadExact('+')) code.ReadExact('-');
			code.ReadNumber();

			code.ReadExact(';');	// Optional
		}

		public override string ToString() => "row...";
	}
}
