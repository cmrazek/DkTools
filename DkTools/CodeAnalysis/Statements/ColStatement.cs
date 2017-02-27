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
		//private ExpressionNode _exp;

		public ColStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);
			var code = p.Code;
			var errorSpan = keywordSpan;

			if (!code.ReadExact('+')) code.ReadExact('-');
			code.ReadNumber();

			//_exp = ExpressionNode.Read(p);
			//if (_exp != null)
			//{
			//	if (_exp.NumChildren > 1 && _exp.LastChild is StringLiteralNode)
			//	{
			//		var colNameNode = _exp.LastChild as StringLiteralNode;
			//		_exp.RemoveChild(colNameNode);
			//	}
			//}

			code.ReadExact(';');	// Optional
		}

		//public override void Execute(RunScope scope)
		//{
		//	base.Execute(scope);

		//	if (_exp != null) _exp.ReadValue(scope);
		//}
	}
}
