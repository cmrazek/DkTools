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

        public override string ToString() => $"(cast to {DataType.ToCodeString()}){_expression}";

        public override void Execute(CAScope scope)
        {
            // A cast would only get executed (not read) when casting a function
            // to (void) to avoid writing to the report stream.
            ReadValue(scope);   // Don't do anything with the return value
        }

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
