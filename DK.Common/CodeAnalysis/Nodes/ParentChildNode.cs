using DK.CodeAnalysis.Values;
using System;

namespace DK.CodeAnalysis.Nodes
{
    internal class ParentChildNode : Node
    {
        private Node _parentNode;
        private Node _childNode;

        public ParentChildNode(Node parentNode, Node childNode)
            : base(parentNode.Statement, childNode.DataType, parentNode.Span.Envelope(childNode.Span))
        {
            _parentNode = parentNode ?? throw new ArgumentNullException(nameof(parentNode));
            _childNode = childNode ?? throw new ArgumentNullException(nameof(childNode));
        }

        public override bool IsReportable { get => _childNode.IsReportable; set => _childNode.IsReportable = value; }

        public override void Execute(CAScope scope)
        {
            _parentNode.OnUsed(scope);
            _childNode.Execute(scope);
        }

        public override Value ReadValue(CAScope scope)
        {
            _parentNode.OnUsed(scope);
            return _childNode.ReadValue(scope);
        }

        public override void WriteValue(CAScope scope, Value value)
        {
            _parentNode.OnUsed(scope);
            _childNode.WriteValue(scope, value);
        }

        public override bool CanAssignValue(CAScope scope) => _childNode.CanAssignValue(scope);

        public override void OnUsed(CAScope scope)
        {
            _parentNode.OnUsed(scope);
            _childNode.OnUsed(scope);
        }

        public override string ToString() => _childNode?.ToString() ?? nameof(ParentChildNode);
    }
}
