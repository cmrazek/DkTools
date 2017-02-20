using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	class BracketsNode : GroupNode
	{
		public BracketsNode(Statement stmt, Span openBracketSpan)
			: base(stmt, openBracketSpan)
		{
		}

		public override int Precedence
		{
			get
			{
				return 0;
			}
		}
	}
}
