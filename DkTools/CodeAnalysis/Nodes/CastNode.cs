using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class CastNode : GroupNode
	{
		private DataType _castDataType;

		public CastNode(Statement stmt, Span span, DataType dataType, ExpressionNode exp)
			: base(stmt, dataType, span)
		{
			_castDataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
			if (exp != null) AddChild(exp);
		}

		public override string ToString() => $"(cast to {DataType.ToCodeString()})";

		public override void Execute(CAScope scope)
		{
			base.Execute(scope);
		}

		public override Value ReadValue(CAScope scope)
		{
			var castScope = scope.Clone();
			var value = base.ReadValue(castScope);
			var dataTypeValue = Value.CreateUnknownFromDataType(_castDataType);
			value = dataTypeValue.Convert(scope, Span, value);
			scope.Merge(castScope);
			return value;
		}

		public override DataType DataType => _castDataType;
	}
}
