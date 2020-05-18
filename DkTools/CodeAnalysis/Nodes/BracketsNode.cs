using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class BracketsNode : GroupNode
	{
		public BracketsNode(Statement stmt, Span openBracketSpan)
			: base(stmt, null, openBracketSpan)
		{
		}
	}
}
