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
		public ReturnStatement(CodeAnalyzer ca, Span span)
			: base(ca, span)
		{
		}

		public static ReturnStatement Read(ReadParams p, Span keywordSpan)
		{
			var ret = new ReturnStatement(p.CodeAnalyzer, keywordSpan);
			p = p.Clone(ret);

			var exp = ExpressionNode.Read(p, ";");
			if (exp == null)
			{
				ret.ReportError(keywordSpan, CAError.CA0014);	// Expected value after 'return'.
			}
			else
			{
				ret.AddNode(exp);
			}

			if (!p.Code.ReadExact(';')) ret.ReportError(p.Code.Span, CAError.CA0015);	// Expected ';'.
			return ret;
		}

		public override void Execute(RunScope scope)
		{
			var returnScope = scope.Clone(dataTypeContext: scope.FunctionDefinition.DataType);
			base.Execute(returnScope);

			scope.Returned = true;
		}
	}
}
