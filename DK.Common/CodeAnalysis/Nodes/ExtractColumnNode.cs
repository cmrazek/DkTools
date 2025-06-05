using DK.Code;
using DK.CodeAnalysis.Statements;
using System;

namespace DK.CodeAnalysis.Nodes
{
    internal class ExtractColumnNode : Node
    {
        private IdentifierNode _columnNode;
        private Node _expression;

        public ExtractColumnNode(Statement stmt, CodeSpan span, IdentifierNode columnNode, Node expression)
            : base(stmt, expression?.DataType ?? columnNode?.DataType, span)
        {
            _columnNode = columnNode ?? throw new ArgumentNullException(nameof(columnNode));
            _expression = expression;

            _columnNode.Parent = this;
            if (_expression != null) _expression.Parent = this;
        }

        public override void Execute(CAScope scope)
        {
            _expression?.Execute(scope);
        }

        public override void OnUsed(CAScope scope)
        {
            _columnNode.OnUsed(scope);
            _expression?.OnUsed(scope);
        }
    }
}
