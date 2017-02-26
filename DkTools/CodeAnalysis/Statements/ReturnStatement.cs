using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Statements
{
	class ReturnStatement : Statement
	{
		public ReturnStatement(ReadParams p, Span keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);

			var retDataType = p.FuncDef.DataType;
			if (retDataType != null && !retDataType.IsVoid)
			{
				var exp = ExpressionNode.Read(p);
				if (exp == null)
				{
					ReportError(keywordSpan, CAError.CA0014);	// Expected value after 'return'.
				}
				else
				{
					AddNode(exp);
				}
			}

			if (!p.Code.ReadExact(';')) ReportError(p.Code.Span, CAError.CA0015);	// Expected ';'.
		}

		public override void Execute(RunScope scope)
		{
			var returnScope = scope.Clone(dataTypeContext: scope.FunctionDefinition.DataType);
			base.Execute(returnScope);
			scope.Merge(returnScope);

			scope.Returned = TriState.True;
		}
	}
}
