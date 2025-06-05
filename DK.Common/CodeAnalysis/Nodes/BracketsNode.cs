using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;

namespace DK.CodeAnalysis.Nodes
{
    class BracketsNode : Node
    {
        private Node _expression;

        private BracketsNode(Statement stmt, CodeSpan span, Node expression)
            : base(stmt, null, span)
        {
            _expression = expression;
            if (_expression != null) _expression.Parent = this;
        }

        public static BracketsNode Read(ReadParams p, CodeSpan openBracketSpan, DataType refDataType)
        {
            var totalSpan = openBracketSpan;

            var expression = ExpressionNode.Read(p, refDataType);
            if (expression != null)
            {
                totalSpan = totalSpan.Envelope(expression.Span);
            }
            else
            {
                p.CodeAnalyzer.ReportError(openBracketSpan, CAError.CA10009);  // Expected expression.
            }

            if (p.Code.ReadExact(')'))
            {
                totalSpan = totalSpan.Envelope(p.Code.Span);
            }
            else
            {
                p.CodeAnalyzer.ReportError(totalSpan, CAError.CA10027); // Expected ')'.
            }

            return new BracketsNode(p.Statement, totalSpan, expression);
        }

        public override string ToString() => $"({_expression?.ToString()})";

        public override void Execute(CAScope scope) => _expression?.Execute(scope);

        public override Value ReadValue(CAScope scope) => _expression?.ReadValue(scope);

        public override void WriteValue(CAScope scope, Value value) => _expression?.WriteValue(scope, value);

        public override bool CanAssignValue(CAScope scope) => _expression?.CanAssignValue(scope) ?? base.CanAssignValue(scope);

        public override DataType DataType => _expression?.DataType;
    }
}
