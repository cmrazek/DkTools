using DK.Code;
using DK.CodeAnalysis.Nodes;
using DK.Modeling;

namespace DK.CodeAnalysis.Statements
{
	class ReturnStatement : Statement
	{
		private ExpressionNode _exp;
		private DataType _returnDataType;

		public ReturnStatement(ReadParams p, CodeSpan keywordSpan)
			: base(p.CodeAnalyzer, keywordSpan)
		{
			p = p.Clone(this);

			var retDataType = p.FuncDef.DataType;
			if (retDataType != null && !retDataType.IsVoid)
			{
				_returnDataType = retDataType;
				_exp = ExpressionNode.Read(p, retDataType);
				if (_exp == null)
				{
					ReportError(keywordSpan, CAError.CA0014);	// Expected value after 'return'.
				}
			}

			if (!p.Code.ReadExact(';')) ReportError(p.Code.Span, CAError.CA0015);	// Expected ';'.
		}

		public override string ToString() => new string[] { "return", _exp.ToString() }.Combine(" ");

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);

			if (_exp != null)
			{
				var returnScope = scope.Clone();
				var returnValue = _exp.ReadValue(returnScope);
				returnValue.CheckTypeConversion(scope, _exp.Span, _returnDataType);
				scope.Merge(returnScope);
			}

			scope.Returned = TriState.True;
		}
	}
}
