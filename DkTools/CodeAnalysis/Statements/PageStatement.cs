using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class PageStatement : Statement
	{
		public PageStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p.Code.ReadExact(';');
		}

		public override string ToString() => "page";
	}
}
