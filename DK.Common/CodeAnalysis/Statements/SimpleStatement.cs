using DK.Code;
using DK.CodeAnalysis.Nodes;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Statements
{
    class SimpleStatement : Statement
    {
        private List<Node> _expressions = new List<Node>();
        private CodeSpan _headingSpan;

        public SimpleStatement(CodeAnalyzer ca)
            : base(ca)
        {
        }

        public bool IsEmpty => _expressions.Count == 0;
        public int NumChildren => _expressions.Count;
        public override string ToString() => _expressions.Select(x => x.ToString()).Combine(" ");

        public void AddNode(Node node)
        {
            _expressions.Add(node);

            if (Span.IsEmpty) Span = node.Span;
            else Span = Span.Envelope(node.Span);
        }

        public override void Execute(CAScope scope)
        {
            base.Execute(scope);

            if (_expressions.Count > 0)
            {
                var exeScope = scope.Clone();
                exeScope.RemoveHeaderString = true;
                foreach (var exp in _expressions) exp.Execute(exeScope);
                scope.Merge(exeScope);

                foreach (var exp in _expressions)
                {
                    if (!exp.IsReportable) continue;

                    var readScope = scope.Clone();
                    readScope.RemoveHeaderString = true;
                    var rootValue = exp.ReadValue(readScope);
                    if (!rootValue.IsVoid)
                    {
                        if (scope.Options.HighlightReportOutput)
                        {
                            ReportError(_headingSpan.IsEmpty ? exp.Span : exp.Span.Envelope(_headingSpan), CAError.CA10070);    // This expression writes to the report stream.
                        }
                    }
                    scope.Merge(readScope);
                }
            }
        }

        public void AddColumnHeading(CodeSpan headingSpan)
        {
            _headingSpan = headingSpan;
            Span = Span.Envelope(headingSpan);
        }
    }
}
