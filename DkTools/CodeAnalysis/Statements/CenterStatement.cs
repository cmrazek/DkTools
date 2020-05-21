using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class CenterStatement : Statement
	{
		public CenterStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p.Code.ReadExact(';');	// Optional
		}

		public override string ToString() => "center";
	}
}
