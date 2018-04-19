using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class EmptyNode : Node
	{
		public EmptyNode(Statement stmt)
			: base(stmt, null, Span.Empty)
		{
		}
	}
}
