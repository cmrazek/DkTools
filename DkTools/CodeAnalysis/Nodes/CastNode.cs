using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class CastNode : GroupNode
	{
		private DataType _dataType;

		public CastNode(Statement stmt, Span span, DataType dataType, ExpressionNode exp)
			: base(stmt, span)
		{
			_dataType = dataType;

			if (exp != null) AddChild(exp);
		}

		protected override void Execute(RunScope scope)
		{
			base.Execute(scope.Clone(dataTypeContext: _dataType));
		}

		public override Value ReadValue(RunScope scope)
		{
			return base.ReadValue(scope.Clone(dataTypeContext: _dataType));
		}
	}
}
