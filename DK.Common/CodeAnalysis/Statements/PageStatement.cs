using DK.Code;

namespace DK.CodeAnalysis.Statements
{
	class PageStatement : Statement
	{
		public PageStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p.Code.ReadExact(';');
		}

		public override string ToString() => "page";
	}
}
