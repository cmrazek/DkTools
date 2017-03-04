using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
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
			var castScope = scope.Clone(dataTypeContext: _dataType);
			base.Execute(castScope);
			scope.Merge(castScope);
		}

		public override Value ReadValue(RunScope scope)
		{
			var castScope = scope.Clone(dataTypeContext: _dataType);
			var value = base.ReadValue(castScope);
			scope.Merge(castScope);
			return value;
		}
	}
}
