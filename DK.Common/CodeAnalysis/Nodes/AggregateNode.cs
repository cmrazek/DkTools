using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.Modeling;
using System.Linq;

namespace DK.CodeAnalysis.Nodes
{
    class AggregateNode : GroupNode
    {
        public AggregateNode(Statement stmt, DataType dataType, params Node[] childNodes)
            : base(stmt, dataType, childNodes.Length > 0 ? (CodeSpan?)CodeSpan.Envelope(childNodes.Select(x => x.Span)) : null)
        {
            foreach (var node in childNodes) AddChild(node);
        }
    }
}
