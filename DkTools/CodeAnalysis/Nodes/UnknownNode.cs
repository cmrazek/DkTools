using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class UnknownNode : TextNode
	{
		public UnknownNode(Statement stmt, Span span, string text)
			: base(stmt, span, text)
		{
		}
	}
}
