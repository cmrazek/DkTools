using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class ColStatement : Statement
	{
		public ColStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;

			if (!code.ReadExact('+')) code.ReadExact('-');
			code.ReadNumber();

			code.ReadExact(';');	// Optional
		}

		public override string ToString() => "col...";
	}
}
