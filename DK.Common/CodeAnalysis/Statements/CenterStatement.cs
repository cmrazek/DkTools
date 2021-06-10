using DK.Code;

namespace DK.CodeAnalysis.Statements
{
	class CenterStatement : Statement
	{
		public CenterStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p.Code.ReadExact(';');	// Optional
		}

		public override string ToString() => "center";
	}
}
