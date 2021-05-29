using DK.Code;
using DK.CodeAnalysis.Statements;

namespace DK.CodeAnalysis.Nodes
{
	class BracketsNode : GroupNode
	{
		public BracketsNode(Statement stmt, CodeSpan openBracketSpan)
			: base(stmt, null, openBracketSpan)
		{
		}
	}
}
