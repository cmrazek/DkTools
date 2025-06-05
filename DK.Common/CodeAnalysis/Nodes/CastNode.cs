using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;
using System;

namespace DK.CodeAnalysis.Nodes
{
    class CastNode : Node
    {
        private DataType _castDataType;
        private Node _expression;

        public CastNode(Statement stmt, CodeSpan span, DataType dataType, Node expression)
            : base(stmt, dataType, span)
        {
            _castDataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            _expression = expression;
        }

        public override string ToString() => $"(cast to {DataType.ToCodeString()})";

        public override void Execute(CAScope scope) { }

        public override Value ReadValue(CAScope scope)
        {
            var castScope = scope.Clone();
            var value = _expression.ReadValue(castScope);
            var dataTypeValue = Value.CreateUnknownFromDataType(_castDataType);
            value = dataTypeValue.Convert(scope, Span, value);
            scope.Merge(castScope);
            return value;
        }

        public override DataType DataType => _castDataType;
    }
}
