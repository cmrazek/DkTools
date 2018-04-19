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
		public CastNode(Statement stmt, Span span, DataType dataType, ExpressionNode exp)
			: base(stmt, dataType, span)
		{
			if (exp != null) AddChild(exp);
		}

		protected override void Execute(RunScope scope)
		{
			var castScope = scope.Clone();
			base.Execute(castScope);
			scope.Merge(castScope);
		}

		public override Value ReadValue(RunScope scope)
		{
			var castScope = scope.Clone();
			var value = base.ReadValue(castScope);
			var dataTypeValue = Value.CreateUnknownFromDataType(DataType);
			value = dataTypeValue.Convert(scope, Span, value);
			scope.Merge(castScope);
			return value;
		}
	}
}
